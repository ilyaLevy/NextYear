using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Humanizer;

namespace _1stYear
{
    // how this structs work:
    // http://www.cclgroupltd.com/geek-post-nskeyedarchiver-files-what-are-they-and-how-can-i-use-them/
    class Processor
    {
        // substitutes bring[i] instead of ref(i)
        static XElement translate(XElement root, Dictionary<int, XElement> bricks)
        {
            root.Descendants("dict").ToList()
                .Where(_ => _.Descendants().Count() == 2 && _.Elements().First().Value == "CF$UID")
                .Select(_ =>
                {
                    var key = Int16.Parse(_.Elements().Last().Value);
                    if (bricks.ContainsKey(key))
                    {
                        _.RemoveAll();
                        _.Add(translate(bricks[key], bricks));
                    }
                    else
                    {
                        _.Add(new XElement(key.ToString(), "???"));
                    }
                    return true;
                }).Count();

            return root;
        }

        public static void extractThumbnails(string xml)
        {
            var doc = XDocument.Load(xml);

            doc
                .Descendants("data")
                .Select((d, i) =>
                {

                    var thumbnail = Path.GetTempFileName() + ".png";

                    //var thumbnail = Path.Combine(Path.GetTempPath(),
                    //                                d.Parent.ElementsBeforeSelf().Last()
                    //                                        .ElementsBeforeSelf().Last()
                    //                                        .Descendants("string").First().Value + ".png");

                    File.WriteAllText(thumbnail + ".base64", d.Value);

                    File.WriteAllBytes(thumbnail, System.Convert.FromBase64String(d.Value));

                    d.Value = thumbnail;

                    return thumbnail;
                })
                // not sure whats wrong with this line - but it just doesnt work!!!
                //.Dump("Extracted thumbnails from " + xml)
                .Count();

            doc.Save(xml);
        }


        public static IEnumerable<TransactionObject> extractXos(string logFilename)
        {
            extractThumbnails(logFilename);

            var root = XDocument.Load(logFilename)
                        .Elements().First().Element("dict");

            var bigArray = root.Element("array");

            // enumerate the elements, save them in an array
            //var numberedEls = bigArray.Elements().Select((_,i)=>{_.Add( new XAttribute("_id_", i)); return _; });
            //new XElement("root",numberedEls).Save(logFilename + ".xml");

            // collect all the building-blocks, aka bricks:
            Dictionary<int, XElement> bricks = new Dictionary<int, XElement>();
            bigArray.Elements().Select((_, i) => { bricks.Add(i, _); return true; }).Count();
            //bricks.Count().Dump();
            //bricks[36].Dump("b36");

            //	bricks
            //		.Values
            //		.Select(_=>{ _.Descendants("data").Remove(); return true; } )
            //		.Count();

            // replace the pointers with the data
            bigArray
                .Descendants("dict")
                .Select(_ => translate(_, bricks))
                .Count();

            var xos = bigArray.Descendants("key").Where(_ => _.Value == "TransactionObject")
                        .Select(_ => _.ElementsAfterSelf().First())
                        ;

            var doc = new XDocument();
            doc.Add(new XElement("root", xos));
            doc.Save(logFilename + "_bigArray.xml");

            xos = simplifyDataStructure(logFilename + "_bigArray.xml");

            var objs = xos.Select(_ => TransactionObject.loadXoNew(_));
            return objs.ToList();
        }

        private static IEnumerable<XElement> simplifyDataStructure(string filename)
        {
            var xdoc = XDocument.Load(filename);

            var keys = xdoc.Descendants("key");

            // remove all togather each               <key>$classes</key><array>...
            keys.ToList()
                .Where(_ => _.Value == "$classes")
                .Select(_ =>
                {
                    var arr = _.ElementsAfterSelf().First();

                    if (arr.Name != "array")
                    {
                        throw new ApplicationException("expected to find a dict, found " + arr.Name + " with value=" + _.Value);
                    }

                    arr.Remove();
                    _.Remove();

                    return true;
                }
                )
                .Count()
                ;


            // 1. <key> followed by a <dict> wiht a single element
            keys.Select(k =>
            {
                var dict = k.ElementsAfterSelf().First();

                if (dict.Name == "true" || dict.Name == "false")
                {
                    // the trivial one
                    k.Add(new XAttribute("val", dict.Name));
                    dict.Remove();
                    return true;
                }

                if (dict.Name == "string"
                    || dict.Name == "real"
                    || dict.Name == "integer"
                    || dict.Name == "data")
                {
                    // the trivial one
                    k.Add(new XAttribute("val", dict.Value));
                    dict.Remove();
                    return true;
                }

                if (k.Value == "NS.objects")
                {
                    if (null == dict.Elements())
                    {
                        dict.Remove();
                        k.Remove();

                        return true;
                    }

                    return false;
                }

                if (dict.Name != "dict")
                {
                    throw new ApplicationException("expected to find a dict, found " + dict.Name + " for key value=" + k.Value);
                }

                if (1 == dict.Elements().Count()
                    && dict.Elements().First().Name == "string")
                {
                    k.Add(new XAttribute("val", dict.Elements().First().Value));
                    dict.Remove();
                    return true;
                }

                return true;
            })
            .Count()
            ;


            // 2. <key>Timestamp</key> has too much data
            flatten(keys, "Timestamp", "NS.time");

            // 3 <key>$class</key> is also too fat
            flatten(keys, "$class", "$classname");

            // 4 <key>Time</key>
            flatten(keys, "Time", "NS.time");

            flatten(keys, "DOB", "NS.time");

            flatten(keys, "ObjectID", "NS.string");

            flatten(keys, "FileName", "NS.string");

            flatten(keys, "ActivityID", "NS.string");

            // 5. <dict> inside of <dict>
            xdoc.Descendants("dict")
                .Where(_ => _.Elements().Count() == 1 && _.Elements().First().Name == "dict")
                .ToList()
                .Select(_ =>
                {
                    var inner_dict = _.Element("dict");
                    inner_dict.Remove();
                    _.ReplaceWith(inner_dict);
                    return true;
                }
                )
                .Count()
                ;

            //    <dict>
            //      <key val="NSArray">$class</key>
            //      <key>NS.objects</key>
            //      <array></array>
            //    </dict>
            xdoc.Descendants("array")
                .ToList()
                .Where(_ => _.Elements() == null || !_.Elements().Any())
                .Select(_ =>
                {
                    if (_.Parent.ElementsBeforeSelf().Last().Name == "key")
                    {
                        _.Parent.ElementsBeforeSelf().Last().Remove();
                        _.Parent.Remove();
                    }
                    else
                    {
                        _.ElementsBeforeSelf().Last().Remove();
                        _.Remove();
                    }

                    return true;
                }
                )
                .Count()
                ;


            // move the attributes
            // there is a special case when - keeping the first dicts clean
            var root = xdoc.Element("root");
            xdoc.Descendants("dict")
                .Where(_ => _.Parent != root
                            && _.Elements()
                                .Where(k => k.Name == "key"
                                            && null != k.Attribute("val")).Count() == _.Elements().Count())
                .ToList()
                .Select(_ =>
            {
                var dst = _.ElementsBeforeSelf().LastOrDefault();
                if (null != dst)
                {
                    var lst = new List<XAttribute>();

                    _.Elements()
                        .ToList()
                        .Select(k =>
                        {
                            lst.Add(new XAttribute(k.Value.Replace('$', '_'), k.Attribute("val").Value));
                            return 0;
                        })
                        .Count();

                    lst.Select(att => { dst.Add(att); return true; }).Count();

                    _.RemoveNodes();
                }
                else
                {
                    //_.Dump("no ebs");
                }
                return 0;
            }
            )
            .Count()
            ;

            // time
            xdoc.Descendants("key")
                .Where(_ => _.Value == "Timestamp" || _.Value == "Time")
                .Select(_ =>
            {
                var t = new DateTime(2001, 1, 1)
                    + new TimeSpan(0, 0, (int)(Single.Parse(_.Attributes().Single().Value)))
                    + TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);

                _.Attributes().Single().Value = t.ToString();

                return 0;
            })
            .Count()
            ;

            xdoc.Descendants("key")
                .Where(_ => _.Value == "Thumbnail")
                .Select(_ =>
            {
                var f = _.ElementsAfterSelf("key")
                            .Where(fn => fn.Value == "FileName")
                            .Single()
                            .Attributes()
                            .Single()
                            .Value;

                var newFilename = Path.Combine(Path.GetTempPath(), f + "_.png");
                if (!File.Exists(newFilename))
                {
                    File.Move(_.Attribute("NS.data").Value, newFilename);
                    File.Move(_.Attribute("NS.data").Value + ".base64", newFilename + ".base64");

                    _.Attribute("NS.data").Value = newFilename;
                }


                return 0;

            })
            .Count();


            xdoc.Descendants("dict")
                .Where(_ => null == _.Elements() || !_.Elements().Any())
                .ToList()
                .Select(_ => { _.Remove(); return 0; })
                .Count()
                ;

            // enumerate XOs
            xdoc.Element("root").Elements("dict")
                .Select((d, i) => { d.Add(new XAttribute("num", i)); return true; })
                .Count()
                ;


            xdoc.Save(filename + ".xml");

            return  xdoc.Element("root").Elements("dict")
                    //.GroupBy(_ => _.keyAttr("$class") + "_" + _.keyAttr("ObjectID"))
                    //.Where(gr=>gr.Any(g=>g.Descendants("key").Where(d=>d.Value == "Thumbnail").Any()))
                    //.Select(gr=>gr.OrderBy(g=>g.keyAttr("Timestamp")).Last())
                    ;
        }

        static void flatten(IEnumerable<XElement> keys, string v1, string v2)
        {
            keys.Where(_ => _.Value == v1 && null == _.Attribute("val"))
                .Select(_ =>
                {
                    var nxt = _.ElementsAfterSelf().First();

                    if (nxt.Name != "dict")
                    {
                        throw new ApplicationException("wrong element next: " + nxt.Name + " with value " + nxt.Value);
                    }

                    try
                    {
                        _.Add(new XAttribute("val", nxt.Descendants("key")
                                                            .Where(d => d.Value == v2)
                                                            .First()
                                                            .Attributes()
                                                            .First()
                                                            .Value));
                        nxt.Remove();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                        //return false;
                    }
                }
                )
                .Count()
                ;
        }

        public static void buildHtml(string dataFilename, string templateFilename, string outputFilename)
        {
            var ldb = XDocument.Load(dataFilename);

            var l = ldb.Descendants("photo").Select(_ => new
            {
                title = _.Attribute("title").Value,
                date = DateTime.Parse(_.Attribute("date").Value),
                id = _.Attribute("id").Value,
                url = _.Attribute("url").Value,
                thumbUrl = _.Attribute("thumbUrl").Value,
            })
                .GroupBy(_ => _.date.ToString("MMMM d"))
                .OrderByDescending(_ => _.First().date)
                ;



            // style we eventually use
            {
                var formatTitle = "<li><h1>{0}</h1></li>";
                var format = @"
	<li>
		<div class='image'>
			<a href='{0}' target='_blank'>
				<img src='{1}?dl=1' />
				<span class='imgTitle'>{2}</span>
			</a>
		</div>
	</li>";
                var formatEmb = @"
	<li>
		<div class='image'>
			<a href='{0}' target='_blank'>
				<img src='data:image/png;base64,
{1}' />
				<span class='imgTitle'>{2}</span>
			</a>
		</div>
	</li>";

                var items = l.Select(_ =>
                    _.Aggregate(new StringBuilder(String.Format(formatTitle, _.Key)),
                                    (a, n) =>
                                    {
                                        if (n.thumbUrl.ToLower().StartsWith("http"))
                                        {
                                            return a.Append(String.Format(format, n.url, n.thumbUrl, n.title, n.date));
                                        }
                                        else
                                        {
                                            var base64 = File.ReadAllText(n.thumbUrl + ".base64");
                                            return a.Append(String.Format(formatEmb, n.url, base64, n.title, n.date));
                                        }
                                    }
                                    ).ToString())
                    ;


                var add_items_here = "<!--##ADD ITEMS HERE##-->";
                File.WriteAllText(outputFilename, File.ReadAllText(templateFilename).Replace(add_items_here, String.Join("\n", items)));
            }
        }


        internal static void buildHtmlsOld(string dataFilename, string templateFilename, string outputFilename)
        {

            var headerFormat = @"
<table style='width:100%'>
    <tr>
        <td style='text-align:left; padding-left:20px; width:20%;'><h2><a href='{beforeUrl}'>{before}</a></td>
        <td>
            <header class='codrops-header'>
                <h1>Ilya's {Nth} Month</h1>
            </header>
        </td>
        <td style='text-align:right; padding-right:20px; width:20%;'><h2><a href='{afterUrl}'>{after}</a></h2></td>
    </tr>
</table>
";

            var formatEx = @"
<li class='title-box'><h2>{date}</h2></li>
<li><a target='_blank' href='{url}?dl=0'>
        <img src='{thumbUrl}?dl=1' alt='{title}'>
        <h3>{title}</h3></a></li>
";

            var ldb = XDocument.Load(dataFilename);

            var l = ldb.Descendants("photo").Select(_ => new
            {
                title = _.Attribute("title").Value,
                date = DateTime.Parse(_.Attribute("date").Value),
                id = _.Attribute("id").Value,
                url = _.Attribute("url").Value,
                thumbUrl = _.Attribute("thumbUrl").Value,
            })
                .GroupBy(_ => _.date.ToString("MMMM d"))
                .OrderByDescending(_ => _.First().date)
                ;

            var curKey = "";
            var items = l.Select((_, i) =>

                    new
                    {
                        date = _.First().date,

                        text = _.Aggregate(new StringBuilder(),
                                            (a, n) =>
                                            {
                                                var ttl = "";

                                                if (_.Key != curKey)
                                                {
                                                    curKey = _.Key;
                                                    ttl = _.Key;
                                                }

                                                return a.Append(formatEx.FormatEx( new { url = n.url, thumbUrl = n.thumbUrl, title = n.title, date = ttl} )
                                                                        .Replace("<h2></h2>","")
                                                                        .Replace("<h3></h3>", ""));
                                            }
                                ).ToString()
                    }
                                ).ToList();

            var add_header_here = "<!--##ADD HEADER HERE##-->";
            var add_items_here = "<!--##ADD ITEMS HERE##-->";

            var BD = new DateTime(2014, 8, 15);

            items.GroupBy(_ => 1 + (_.date - BD).Days / 31)
                .Select(m =>
                {
                    var before = m.Key == 1 ? "" : "earlier...";
                    var after = m.Key == 1 + (DateTime.Now - BD).Days / 31 ? "" : "later...";

                    var headerText = headerFormat.FormatEx(new
                    {
                        Nth = new String(m.Key.ToOrdinalWords().Select((c, i) => 0 == i ? Char.ToUpper(c) : c).ToArray()),

                        before = before,
                        after = after,

                        beforeUrl = Path.GetFileName(String.Format(outputFilename, m.Key - 1)),
                        afterUrl = Path.GetFileName(String.Format(outputFilename, m.Key + 1))
                    }
                                                                );

                    var itemsText = String.Join("\n", m.Select(_ => _.text));

                    File.WriteAllText(  String.Format(outputFilename, m.Key),
                                        File.ReadAllText(templateFilename)
                                            .Replace(add_header_here, headerText)
                                            .Replace(add_items_here, itemsText)
                                            );

                    return true;
                })
                .Count()
                ;
        }


        internal static void buildHtmls(string dataFilename, string templateFilename, string outputFilename)
        {
            var ldb = XDocument.Load(dataFilename);

            var l = ldb.Descendants("photo").Select(_ => new
            {
                title = _.Attribute("title").Value,
                date = DateTime.Parse(_.Attribute("date").Value),
                id = _.Attribute("id").Value,
                url = _.Attribute("url").Value,
                thumbUrl = _.Attribute("thumbUrl").Value,
            })
                .GroupBy(_ => _.date.ToString("MMMM d"))
                .OrderByDescending(_ => _.First().date)
                ;



            // style we eventually use
            {
                var format = @"
<div class='item'>
    <h1>{4}</h1>
    <a href='{0}' target='_blank'>
        <img class='thumbnail {3}' src='{1}?dl=1'/>
        <span class='imgTitle'>{2}</span></a></div>
";
                								

                var add_items_here = "<!--##ADD ITEMS HERE##-->";

                var rnd = new Random();
                var clss = new string[] { "", "small", "medium", "large" };

                var curKey = "";

                var items = l.Select((_,i) =>

                        new { date = _.First().date,

                                    text = _.Aggregate( new StringBuilder(),
                                                        (a, n) => 
                                                            {
                                                                var ttl = "";

                                                                if (_.Key != curKey)
                                                                {
                                                                    curKey = _.Key;
                                                                    ttl = _.Key;
                                                                }

                                                                return a.Append(String.Format(format, n.url, n.thumbUrl, n.title, clss[rnd.Next(clss.Length)], ttl));
                                                            }
                                            ).ToString()
                                    }
                                    ).ToList();

                items.GroupBy(_ => 1 + (_.date - new DateTime(2014, 8, 15)).Days / 31)
                    .Select(m =>
                    {
                        var itms = String.Join("\n", m.Select(_=>_.text));

                        File.WriteAllText(String.Format(outputFilename, m.Key), File.ReadAllText(templateFilename).Replace(add_items_here, String.Join("\n", itms)));

                        return true;
                    })
                    .Count()
                    ;

            }
        }
    }
}
