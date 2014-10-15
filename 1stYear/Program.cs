using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace _1stYear
{
    class Program
    {
        static string localDbFilename = @"C:\Dev\Quick\FirstYear\localDb.xml";

        static void Main(string[] args)
        {

            var allXOs = new List<TransactionObject>();

            if( !File.Exists(localDbFilename))
            {
                // load the old stuff only for when the localDB is not created yet
                allXOs.AddRange(loadDbseedData());
            }

            allXOs.AddRange(loadDynamicData());

            var flt = allXOs
                //.Where(_ => null != _ as XoJoy)
                            .GroupBy(_ => _.ObjectID)
                            .Select(_ => _.OrderByDescending(__ => __.Timestamp).First())
                            .OrderBy(_ => _.Time)
                //.Select(_ => _ as XoJoy)
                            .ToList()
                            ;

            var asdf1 = flt.Where(_ => _.ObjectID == new Guid("099C05BF-88A0-40FB-A19C-F58D08C0F652"))
                    //.OrderBy(_ => _.Timestamp)
                    .ToList();
            
            //var freshXOs = loadDynamicData();

            Console.WriteLine("Total XOs loaded: {0}, distinct: {1}", allXOs.Count(), flt.Count());


            var asdf = allXOs.Where(_ => _.PictureNote.Thumbnail == null || _.PictureNote.Thumbnail == "").ToList();

            
            // now update the local db - there is a chance the titles were changed
            updateLocalDb(flt.Select(_ => new FYPhoto()
            {
                title = _.Note,
                id = _.PictureNote.FileName.ToString(),
                //thumbUrl = _.PictureNote.Thumbnail,
                date = _.Time
            }), localDbFilename);

            //Processor.buildHtml(dataFilename, @"C:\Dev\Quick\FirstYear\Ilya Daily, template.html", @"C:\Dev\Quick\FirstYear\IlyaDaily_A{0}.html");
            //Processor.buildHtmls(dataFilename, @"C:\Dev\Quick\FirstYear\Ilya Daily, new template.html", @"C:\Dev\Quick\FirstYear\IlyaDaily_B{0}.html");

            Processor.buildHtmlsOld(localDbFilename, @"C:\Dev\Quick\FirstYear\Ilya Daily, old template.html", @"C:\Dev\Quick\FirstYear\IlyaDaily_C{0}.html");
        }

        private static IEnumerable<TransactionObject> loadDbseedData()
        {
            return XDocument.Load(@"C:\Dev\Quick\FirstYear\db\dbseed.xml")
                            .Element("root")
                            .Elements("photo")
                            .Select(_ => TransactionObject.loadFromCache(_))
                            .ToList()
                            ;
        }

        private static IEnumerable<TransactionObject> loadDynamicData()
        {
            var sinkv2Root = @"C:\Users\maxlevy\Dropbox\Apps\FirstYear\sinkv2\Ilya, m3__2A96AA59-7C83-4CD6-946E-1CCE4DED7BBA\";

            var devices = new Tuple<string, string>[] {
	            new Tuple<string,string>("max", "27F69006-E9BE-4B14-A056-CF1A9CE27A81__64D58B9C-CEA1-42AB-9A1A-C764EF159C1B"),
	            new Tuple<string,string>("motoko", "4F9DA8D8-9B8C-4787-A30D-20A9D99BD4A1__D8BDFCC3-735E-43E0-A4BA-940F224838BF"),
            };


            var allXos = devices.Select(d =>
            {
                var localDbLastWriteTime = new FileInfo(localDbFilename).LastWriteTime;

                
                var logFiles = Directory.EnumerateFiles(sinkv2Root + d.Item2, "*.tlog")
                                        //.Where(_=> localDbLastWriteTime < new FileInfo(_).LastWriteTime )
                                        .ToList();

                return logFiles.Select((lf, i) =>
                {
                    var dat = String.Format(@"C:\Users\maxlevy\AppData\Local\Temp\dataFrom_{0}_Log{1}.dat", d.Item1, i);
                    var xml = dat + ".xml";

                    File.Copy(lf, dat, true);

                    Process.Start(@"C:\Dev\Quick\FirstYear\Translator.exe", String.Format("{0} {1}", dat, xml))
                            .WaitForExit();

                    return Processor.extractXos(xml);

                }
                    )
                    .SelectMany(_ => _)
                    .ToList();
            }
            )
            .SelectMany(_ => _)
            .ToList();

            Console.WriteLine("Total XOs: {0}", allXos.Count());

            var asdf = allXos.Where(_ => _.ObjectID == new Guid("93592DFF-E519-47D7-B2FE-D511982CBCCA"))
                                .OrderBy(_ => _.Timestamp)
                                .ToList();

            var pictureNotes = allXos.Where(_ => null != _.PictureNote
                                                && null != _.PictureNote.Thumbnail)

                ////.Where(_ => null != _ as XoJoy)
                //            .GroupBy(_ => _.ObjectID)
                //            .Select(_ => _.OrderByDescending(__ => __.Timestamp).First())
                //            .OrderBy(_ => _.Time)
                ////.Select(_ => _ as XoJoy)
                //            .ToList()
                            ;

            return pictureNotes;
        }

        static void updateLocalDb(IEnumerable<FYPhoto> freshPhotos, string dataFilename)
        {
            bool dirty = false;
            var curPhotos = loadLocalDb(dataFilename).ToList();

            // any titles were updated?
            foreach (var cp in curPhotos)
	        {
                var fp = freshPhotos.FirstOrDefault(_ => _.id == cp.id);
                if( null != fp
                    && !(String.IsNullOrEmpty(fp.title) && String.IsNullOrEmpty(cp.title))
                    && fp.title != cp.title)
                {
                    // this one was!
                    cp.title = fp.title;
                    dirty = true;
                }
	        }

            // see if there is anything new
            var news = freshPhotos.Except(curPhotos, new MyEqualityComparer());
            if( news.Any() )
            {
                curPhotos.AddRange(news);
                dirty = true;
            }

            if (dirty)
            {
                new XDocument(new XElement("root", curPhotos.Select(_ => _.toXElement()))).Save(dataFilename);
            }

            // generate the missing URLs now, id any
            if (curPhotos.Where(_ => String.IsNullOrEmpty(_.url) || String.IsNullOrEmpty(_.thumbUrl) ).Any())
            {
                var process = new Process() { StartInfo = new ProcessStartInfo( @"c:\Python27\python.exe",
                                                                                @"C:\Dev\Quick\FirstYear\cli_client.py")
                                                                                {
                                                                                    //UseShellExecute = false,
                                                                                    //RedirectStandardOutput = true,
                                                                                }
                                            };

                List<string> odr = new List<string>();

                process.OutputDataReceived += (sender, args) => odr.Add(args.Data);
                process.ErrorDataReceived += (sender, args) => odr.Add(args.Data);

                if(!process.Start())
                {
                    // can't start the tihngs
                    throw new ApplicationException("Falied to start the python thing");
                }

                process.WaitForExit();

                //process.BeginOutputReadLine();
            }
        }

        static IEnumerable<FYPhoto> loadLocalDb(string dataFilename)
        {
            var ldb = XDocument.Load(dataFilename);

            return ldb.Descendants("photo").Select(_ => new FYPhoto()
            {
                title = _.Attribute("title").Value,
                date = DateTime.Parse(_.Attribute("date").Value),
                id = _.Attribute("id").Value,
                url = _.Attribute("url").Value,
                thumbUrl = _.Attribute("thumbUrl").Value,
            });
        }


    }

    class FYPhoto
    {
        public FYPhoto() { }

        public string title { get; set; }
        public string id { get; set; }
        public DateTime date { get; set; }
        public string url { get; set; }
        public string thumbUrl { get; set; }

        public XElement toXElement()
        {
            return new XElement("photo", new XAttribute[] {	new XAttribute("title", title),
														new XAttribute("id", id),
														
														new XAttribute("date", date),
														
														new XAttribute("url", url??String.Empty),
														new XAttribute("thumbUrl", thumbUrl??String.Empty),
		}
        );
        }
    }


    class MyEqualityComparer : IEqualityComparer<FYPhoto>
    {
        public bool Equals(FYPhoto x, FYPhoto y)
        {
            return x.id.ToUpper() == y.id.ToUpper();
        }

        public int GetHashCode(FYPhoto obj)
        {
            return obj.id.ToUpper().GetHashCode();
        }
    }

}


