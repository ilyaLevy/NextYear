using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace _1stYear
{
    class TransactionObject
    {
        static readonly DateTime t0 = new DateTime(2001, 1, 1);


        public int id { get; private set; }
        public XElement xml { get; private set; }

        // properties common to all the XOs
        public Dictionary<string, XElement> data { get; private set; }
        public Guid ObjectID { get; private set; }

        public string xoClass { get; private set; }

        public DateTime Time { get; private set; }
        public DateTime Timestamp { get; private set; }

        public PictureNote PictureNote { get; private set; }

        public string Note { get; private set; }

        protected TransactionObject(XElement xo)
        {
            this.data = null;

            xml = xo;
            id = Int16.Parse(xml.Attributes().Single().Value);

            xoClass = xo.keyAttr("$class");

            ObjectID = Guid.Parse(xo.keyAttr("ObjectID"));

            Note = xo.keyAttr("Note");

            if (null != xo.keyAttr("Time") )
            {
                Time = DateTime.Parse(xo.keyAttr("Time"));
            }
            Timestamp = DateTime.Parse(xo.keyAttr("Timestamp"));

            var pn = xo.Elements("key").Where(_=>_.Value=="PictureNote");
            if( pn.Any())
            {
                var arr = pn.Single().ElementsAfterSelf().First().Descendants("array").Single();
                var ele = arr.Elements().First();

                if( 1 < arr.Elements().Count())
                {
                    var notDel = arr.Elements().Where(_ => _.keyAttr("Deleted") == "false");

                    ele = notDel.Single();
                }

                PictureNote = new PictureNote(ele, false);

            }

        }

        protected TransactionObject(Dictionary<string, XElement> data)
        {
            this.data = data;

            xoClass = data["$class"].valueOf("$classname");

            var objId = data["ObjectID"].valueOf("NS.string");
            if( null == objId )
            {
                objId = data["ObjectID"].Descendants("string").First().Value;
            }
            ObjectID = Guid.Parse(objId);

            if (data.Keys.Contains("Note"))
            {
                Note = data["Note"].Descendants("string").First().Value;
            }

            if (data.Keys.Contains("PictureNote"))
            {
                PictureNote = new PictureNote(data["PictureNote"]);
            }

            if( data.Keys.Contains("Time") )
            {
                var time = data["Time"].valueOf("NS.time");
                Time = t0
                    + new TimeSpan(0, 0, null == time ? 0 : (int)(Single.Parse(time)))
                        + TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            }

            var timestamp = data["Timestamp"].valueOf("NS.time");
            Timestamp = t0
                + new TimeSpan(0, 0, null == timestamp ? 0 : (int)(Single.Parse(timestamp)))
                        + TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
        }

        protected static Dictionary<string, XElement> split(XElement xo)
        {
            var tas = new Dictionary<string, XElement>();

            // a list of key/dict
            var kds = xo.Elements();

            kds.Where(_ => _.Name == "key").Select(k =>
            {
                tas.Add(k.Value, k.ElementsAfterSelf().First());

                return true;
            }
            )
            .Count();

            return tas;
        }
        public static TransactionObject loadXoNew(XElement xo)
        {
            return new TransactionObject(xo);
        }

        public static TransactionObject loadXo(XElement xo)
        {
            var data = split(xo.Element("dict"));

            if (data["$class"].valueOf("$classname") == "Joy")
            {
                return new XoJoy(data);
            }

            //else if (data["$class"].valueOf("$classname") == "Journal")
            //{
            //    var xobj = new TransactionObject(data);
            //}
            //else
            //{
            //    var xobj = new TransactionObject(data);
            //}

            return new TransactionObject(data);
        }

        static string activityName(XElement xo)
        {
            if (xo.Elements().Count() != 1)
            {
                throw new ApplicationException("expected only one element here: " + xo.Name);
            }

            var d1 = xo.Elements().Single();

            if (d1.Name != "dict")
            {
                throw new ApplicationException("expected to find DICT instead of " + d1.Name);
            }

            var cls = d1.Elements("key").Where(_ => _.Value == "$class");
            if (cls.Count() != 1)
            {
                throw new ApplicationException("expected to find only one key==$class");
            }


            var clsName = cls.Single().ElementsAfterSelf().First().Descendants("key").Where(_ => _.Value == "$classname");

            if (clsName.Count() != 1)
            {
                throw new ApplicationException("expected to find only one <key>$classname</key>");
            }

            return clsName.Single().ElementsAfterSelf().First().Value;
        }
    }

    class PictureNote
    {
        static readonly DateTime t0 = new DateTime(2001, 1, 1);

        public string Thumbnail { get; private set; }
        public Guid FileName { get; private set; }

        public DateTime Timestamp { get; private set; }

        public bool IsNew { get; private set; }
        public bool Deleted { get; private set; }

        public Guid ActivityID { get; private set; }
        public Guid ObjectID { get; private set; }

        //public int MediaType { get; private set; }

        public PictureNote(XElement pn)
        {
            Thumbnail = pn.valueOf("Thumbnail", "NS.data");

            var guid = pn.valueOf("FileName", "NS.string");
            FileName = null == guid ? new Guid() : new Guid(guid);

            IsNew = pn.boolValueOf("NS.objects", "IsNew");
            Deleted = pn.boolValueOf("NS.objects", "Deleted");

            var aid = pn.valueOf("ActivityID", "NS.string");
            ActivityID = null == aid ? new Guid() : new Guid(aid);

            var oid = pn.valueOf("ObjectID", "NS.string");
            ObjectID = null == oid ? new Guid() : new Guid(oid);

            // not sure what are these negative times mean :(
            //	<key>NS.time</key>
            //	<real>-978307200.000000</real>
            // (new TimeSpan(0,0,(int)(Single.Parse(pn.valueOf("Timestamp", "NS.time"))))).Dump("adsfasdf");

            var ts = pn.valueOf("Timestamp", "NS.time");
            Timestamp = t0
                + new TimeSpan(0, 0, null == ts ? 0 : (int)(Single.Parse(ts)))
                        + TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
        }

        public PictureNote(XElement pn, bool b)
        {
            var keys = pn.Descendants("key").ToList();

            Thumbnail = keys.Single(_=>_.Value == "Thumbnail").Attribute("NS.data").Value;

            FileName = new Guid(keys.Single(_ => _.Value == "FileName").Attributes().Single().Value);

            IsNew = Boolean.Parse(pn.keyAttr("IsNew"));
            Deleted = Boolean.Parse(pn.keyAttr("Deleted"));

            var aid = pn.keyAttr("ActivityID");
            ActivityID = null == aid ? new Guid() : new Guid(aid);

            var oid = pn.keyAttr("ObjectID");
            ObjectID = null == oid ? new Guid() : new Guid(oid);

            // not sure what are these negative times mean :(
            //	<key>NS.time</key>
            //	<real>-978307200.000000</real>
            // (new TimeSpan(0,0,(int)(Single.Parse(pn.valueOf("Timestamp", "NS.time"))))).Dump("adsfasdf");

            Timestamp = DateTime.Parse(pn.keyAttr("Timestamp"));
        }
    }

    class XoJoy : TransactionObject
    {
        public XoJoy(Dictionary<string, XElement> data)
            : base(data)
        {
        }
    }
}
