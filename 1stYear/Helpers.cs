using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace _1stYear
{
    class Helpers
    {
    }

    static class Aux
    {
        public static string keyAttr( this XElement ele, string attrName )
        {
            var myKey = ele.Elements("key").Where(k => k.Value == attrName);

            if( !myKey.Any() )
            {
                return null;
            }

            if (null != myKey.Single().Attribute("NS.string"))
            {
                return myKey.Single().Attribute("NS.string").Value;
            }

            return myKey.Single().Attributes().Single().Value;
        }

        public static bool boolValueOf(this XElement ele, string key, string subKey)
        {
            var k = ele.Descendants("key").Where(_ => _.Value == key).FirstOrDefault();

            var sk = k.ElementsAfterSelf().First()
                        .Descendants("key").Where(_ => _.Value == subKey).FirstOrDefault();

            if(sk == null)
            {
                // UF
                return false;
            }

            return Boolean.Parse(sk.ElementsAfterSelf().First().Name.LocalName);
        }

        public static string valueOf(this XElement ele, string key, string subKey)
        {
            var k = ele.Descendants("key").Where(_ => _.Value == key).FirstOrDefault();
            if (null == k)
            {
                // throw new ApplicationException("wrong key name: " + key);
                return null;
            }

            return k.ElementsAfterSelf().First().valueOf(subKey);
        }

        public static string valueOf(this XElement ele, string key)
        {
            var k = ele.Descendants("key").Where(_ => _.Value == key).FirstOrDefault();
            if (null == k)
            {
                // throw new ApplicationException("wrong key name: " + key);

                var v1 = ele.Descendants().Single();

                return v1.Value;
            }

            var firstAfter = k.ElementsAfterSelf().First();

            if (firstAfter.Name == "string"
                || firstAfter.Name == "data")
            {
                return firstAfter.Value;
            }

            var v = firstAfter.Descendants("string").FirstOrDefault();
            if (null == v)
            {
                v = firstAfter.Descendants("data").FirstOrDefault();
            }
            if (null == v)
            {
                v = k.Parent.Descendants("real").FirstOrDefault();
            }

            return null == v ? null : v.Value;
        }
    }

}
