<Query Kind="Program">
  <Reference>&lt;ProgramFilesX86&gt;\Open XML SDK\V2.5\lib\DocumentFormat.OpenXml.dll</Reference>
  <Reference Relative="..\..\tools\TLC_2.6.45.0.Single\Microsoft.MachineLearning.CommandLine.dll">C:\dev\tools\TLC_2.6.45.0.Single\Microsoft.MachineLearning.CommandLine.dll</Reference>
  <Reference Relative="..\..\tools\TLC_2.6.45.0.Single\Microsoft.MachineLearning.Core.dll">C:\dev\tools\TLC_2.6.45.0.Single\Microsoft.MachineLearning.Core.dll</Reference>
  <Reference Relative="..\..\tools\TLC_2.6.45.0.Single\Microsoft.MachineLearning.Learners.dll">C:\dev\tools\TLC_2.6.45.0.Single\Microsoft.MachineLearning.Learners.dll</Reference>
  <Reference>&lt;ProgramFilesX86&gt;\Microsoft Visual Studio 12.0\Visual Studio Tools for Office\PIA\Office15\Microsoft.Office.Interop.OneNote.dll</Reference>
  <Reference>&lt;ProgramFilesX64&gt;\Common Files\microsoft shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.dll</Reference>
  <Reference>&lt;ProgramFilesX64&gt;\Common Files\microsoft shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.Runtime.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.dll</Reference>
  <Reference Relative="..\..\tools\TLC_2.6.45.0.Single\TMSNlearnPrediction.dll">C:\dev\tools\TLC_2.6.45.0.Single\TMSNlearnPrediction.dll</Reference>
  <NuGetReference>Humanizer</NuGetReference>
  <Namespace>DocumentFormat.OpenXml.Packaging</Namespace>
  <Namespace>DocumentFormat.OpenXml.Wordprocessing</Namespace>
  <Namespace>Humanizer</Namespace>
  <Namespace>Microsoft.MachineLearning</Namespace>
  <Namespace>Microsoft.MachineLearning.CommandLine</Namespace>
  <Namespace>Microsoft.MachineLearning.Learners</Namespace>
  <Namespace>Microsoft.MachineLearning.Model</Namespace>
  <Namespace>Microsoft.Office.Interop.OneNote</Namespace>
  <Namespace>Microsoft.TMSN.TMSNlearn</Namespace>
  <Namespace>sp = Microsoft.SharePoint.Client</Namespace>
  <Namespace>System.Net</Namespace>
</Query>

static class Aux
{
	public static bool boolValueOf(this XElement ele, string key, string subKey)
	{
		var k = ele.Descendants("key").Where(_=>_.Value == key ).FirstOrDefault();
		
		var sk = k.ElementsAfterSelf().First()
					.Descendants("key").Where(_=>_.Value == subKey ).FirstOrDefault();
		
		return Boolean.Parse( sk.ElementsAfterSelf().First().Name.LocalName );
	}

	public static string valueOf(this XElement ele, string key, string subKey)
	{
		var k = ele.Descendants("key").Where(_=>_.Value == key ).FirstOrDefault();
		if(null == k)
		{
			//throw new ApplicationException("wrong key name: " + key);
			return null;
		}
				
		return k.ElementsAfterSelf().First().valueOf(subKey);
	}

	public static string valueOf(this XElement ele, string key)
	{
		var k = ele.Descendants("key").Where(_=>_.Value == key ).FirstOrDefault();
		if(null == k)
		{
			// throw new ApplicationException("wrong key name: " + key);
			return null;
		}

		var firstAfter = k.ElementsAfterSelf().First();
		
		if( firstAfter.Name == "string"
			|| firstAfter.Name == "data" )
		{
			return firstAfter.Value;
		}
		
		var v = firstAfter.Descendants("string").FirstOrDefault();
		if( null == v )
		{
			v = firstAfter.Descendants("data").FirstOrDefault();
		}
		if( null == v )
		{
			v = k.Parent.Descendants("real").FirstOrDefault();
		}
		
		return null == v ? null : v.Value;
	}
}

class TransactionObject
{
	static readonly DateTime t0 = new DateTime(2001,1,1);
	
	public Dictionary<string,XElement> data { get; private set; }

	// properties common to all the XOs
	public Guid ObjectID { get; private set; }

	public string xoClass { get; private set; }

	public DateTime Time { get; private set; }
	public DateTime Timestamp { get; private set; }

	public PictureNote PictureNote { get; private set; }

	protected static Dictionary<string,XElement> split(XElement xo)
	{
		var tas = new Dictionary<string,XElement>();

		// a list of key/dict
		var kds = xo.Elements();
				
		kds.Where(_=>_.Name == "key").Select(k=>
		{
			tas.Add(k.Value, k.ElementsAfterSelf().First());
			
			return true;
		}
		)
		.Count();
		
		return tas;
	}

	protected TransactionObject(Dictionary<string,XElement> data)
	{
		this.data = data;
		
		xoClass = data["$class"].valueOf("$classname");

		var guid = data["ObjectID"].valueOf("NS.string");
		ObjectID = null == guid ? new Guid() : new Guid(guid);

		Timestamp =	t0
					+ new TimeSpan(0,0,(int)(Single.Parse(data["Timestamp"].valueOf("NS.time"))));

		
		if( data.Keys.Contains("Time") )
		{
			Time =	t0
					+ new TimeSpan(0,0,(int)(Single.Parse(data["Time"].valueOf("NS.time"))));
		}

		if( data.Keys.Contains("PictureNote") )
		{
			PictureNote = new PictureNote(data["PictureNote"]);
		}


	}

	public static TransactionObject loadXo(XElement xo)
	{
		var data = split(xo.Element("dict"));
		
		if( data["$class"].valueOf("$classname") == "Joy" )
		{
			return new XoJoy(data);
		}
		
		return new TransactionObject(data);
	}

	static string activityName(XElement xo)
	{
		if( xo.Elements().Count() != 1 )
		{
			throw new ApplicationException("expected only one element here: " + xo.Name);
		}
	
		var d1 = xo.Elements().Single();
		
		if( d1.Name != "dict" )
		{
			throw new ApplicationException("expected to find DICT instead of " + d1.Name);
		}
		
		var cls = d1.Elements("key").Where(_=>_.Value == "$class" );
		if( cls.Count() != 1 )
		{
			throw new ApplicationException("expected to find only one key==$class");
		}
		
					
		var clsName = cls.Single().ElementsAfterSelf().First().Descendants("key").Where(_=>_.Value == "$classname");
		
		if(clsName.Count() !=1 )
		{
			throw new ApplicationException("expected to find only one <key>$classname</key>");
		}
		
		return clsName.Single().ElementsAfterSelf().First().Value;
	}
}

class PictureNote
{
	static readonly DateTime t0 = new DateTime(2001,1,1);

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
		
		//IsNew = pn.boolValueOf("NS.objects", "IsNew");
		//Deleted = pn.boolValueOf("NS.objects", "Deleted");
		
		guid = pn.valueOf("ActivityID", "NS.string");
		ActivityID = null == guid ? new Guid() : new Guid(guid);
		
		guid = pn.valueOf("ObjectID", "NS.string");
		ObjectID = null == guid ? new Guid() : new Guid(guid);
		
		// not sure what are these negative times mean :(
		//	<key>NS.time</key>
		//	<real>-978307200.000000</real>
		// (new TimeSpan(0,0,(int)(Single.Parse(pn.valueOf("Timestamp", "NS.time"))))).Dump("adsfasdf");

		var dt = pn.valueOf("Timestamp", "NS.time");
		Timestamp =	t0
					+ new TimeSpan(0,0,null == dt ? 0 : (int)(Single.Parse(dt)));
	}
}

class XoJoy : TransactionObject 
{
	public string Note { get; private set; }

	public XoJoy(Dictionary<string,XElement> data) : base(data)
	{
		if(data.Keys.Contains("Note") )
		{
			Note = data["Note"].Descendants("string").First().Value;
		}
	}
}


// how this structs work:
// http://www.cclgroupltd.com/geek-post-nskeyedarchiver-files-what-are-they-and-how-can-i-use-them/

// picture: 1176A28A-D63B-459D-B4E8-BB82225575D1
void Main()
{
	var logFilenames = new string[] {
	// max
	//@"C:\Users\maxlevy\Dropbox\Apps\FirstYear\sinkv2\Ilya, m3__2A96AA59-7C83-4CD6-946E-1CCE4DED7BBA\27F69006-E9BE-4B14-A056-CF1A9CE27A81__64D58B9C-CEA1-42AB-9A1A-C764EF159C1B\TransactionLog0.tlog",
	
	// motoko
	@"C:\Users\maxlevy\Dropbox\Apps\FirstYear\sinkv2\Ilya, m3__2A96AA59-7C83-4CD6-946E-1CCE4DED7BBA\4F9DA8D8-9B8C-4787-A30D-20A9D99BD4A1__D8BDFCC3-735E-43E0-A4BA-940F224838BF\TransactionLog1.tlog",
	};
	
	logFilenames.Select((_,i)=>
	{
		var dat = @"C:\Users\maxlevy\AppData\Local\Temp\mydata" + i.ToString() + ".dat"; //Path.GetTempFileName();
		var xml = dat + ".xml";
		
		File.Copy(_,dat,true);
		
		var translationProcess = 
			Process.Start(	@"C:\Dev\Quick\FirstYear\Translator.exe",
						String.Format("{0} {1}", dat, xml));
		
		translationProcess.WaitForExit();
		
		extractThumbnails(xml);
		
		return processTransactionLog(xml);
	}
	).Count();
}

void extractThumbnails(string xml)
{
	var doc = XDocument.Load(xml);
	
	doc
		.Descendants("data")
		.Select((d,i)=>{
		
			var thumbnail = Path.GetTempFileName()+".png";
			
			File.WriteAllBytes(	thumbnail, System.Convert.FromBase64String(d.Value));

			d.Value = thumbnail;
			
			return thumbnail;
		})
		// not sure whats wrong with this line - but it just doesnt work!!!
		//.Dump("Extracted thumbnails from " + xml)
		.Count();
	
	doc.Save(xml);
}

// substitutes bring[i] instead of ref(i)
XElement translate(XElement root, Dictionary<int, XElement> bricks)
{
	root.Descendants("dict").ToList()
		.Where(_=>_.Descendants().Count()==2 && _.Elements().First().Value=="CF$UID")
		.Select(_=>
	{
			var key = Int16.Parse(_.Elements().Last().Value);
			if( bricks.ContainsKey(key))
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

	
	root
		.Descendants("dict").ToList()
		.Where(_=>
	{
		var b4=_.ElementsBeforeSelf();
		
		return b4.Any() && b4.Last().Value.StartsWith("$");
	}).Select(_=>{ 
	
		_.Remove();
				
		return true; 
	} ).Count();
	
	root.Descendants("key").ToList()
		.Where(_=>_.Value.StartsWith("$"))
		.Select(_=>{ _.Remove(); return true;} )
		.Count();
			
	return root;
}



bool processTransactionLog(string logFilename)
{
	var root = XDocument.Load(logFilename)
				.Elements().First().Element("dict");	
	
	var bigArray = root.Element("array");
	
	// enumerate the elements, save them in an array
	//var numberedEls = bigArray.Elements().Select((_,i)=>{_.Add( new XAttribute("_id_", i)); return _; });
	//new XElement("root",numberedEls).Save(logFilename + ".xml");

	// collect all the building-blocks, aka bricks:
	Dictionary<int, XElement> bricks = new Dictionary<int, XElement>();
	bigArray.Elements().Select((_,i)=>{bricks.Add(i,_); return true;}).Count();
	//bricks.Count().Dump();
	//bricks[36].Dump("b36");
	
//	bricks
//		.Values
//		.Select(_=>{ _.Descendants("data").Remove(); return true; } )
//		.Count();

	// replace the pointers with the data
	bigArray
		.Descendants("dict")
		.Select(_=>translate(_, bricks))
		.Count();

	var xos = bigArray.Descendants("key").Where(_=>_.Value == "TransactionObject" )
				.Select(_=>_.ElementsAfterSelf().First())
				;
				
	var doc = new XDocument();
	doc.Add( new XElement("root", xos ));
	doc.Save(@"c:\temp\bigArray.xml");
		
	var testId = new Guid("44C7D316-D6BF-4BB3-B3C1-277019EE5188");
//	testId = new Guid("2CF5ED75-BCF6-47E9-A162-584A1213B0D3");
	testId = new Guid("CC585403-2DEB-450D-BD6F-CA5C9512B7A0");
	
	var objs = xos.Select(_=>TransactionObject.loadXo(_))
					.Where(_=>_.ObjectID == testId)
					;
	
	objs
		.Select(_=>_.data)
		//.Take(3)
		//.Where(_=> null != _ as XoJoy )
		.Dump();
	
	
		
	return false;

	//bigArray.Elements("dict").Select(_=>_.Elements().First().Value).Distinct().Dump("Elements");	

	var notes = bigArray.Elements("dict").Where(_=>_.Elements().First().Value=="Note");
	
	var notEmptyNotes = notes.Where(_=>_.Descendants().ElementAt(2).Value != "");
	//notEmptyNotes.Count().Dump("notEmptyNotes");
	//notEmptyNotes.Skip(0).First().Dump();
	
	var data = new List<Dictionary<string,XElement>>();
	notEmptyNotes	.Select(_=>
	{
		var a = _.Elements().Where((e,i)=>0==i%2);
		var b = _.Elements().Where((e,i)=>0!=i%2);
		
		return a.Zip(b, (x,y)=>new Tuple<string, XElement>(x.Value,y) );
	})
	.Select(c=>
	{
		var dic = new Dictionary<string,XElement>();
		data.Add(dic);
		
		c.Select(t=>{ dic.Add(t.Item1, t.Item2); return true; } ).Count();
		
		return true;
	}).Count();
	
	data.Count().Dump();
	//data[0].Dump();
	
	var html = data.Where(d=>d.ContainsKey("PictureNote")
				//&& null != valueOf("FileName", d["PictureNote"])
				)
		.Select(d=> 
	{
		var thumbnail = d["PictureNote"].valueOf("Thumbnail");
		
		var t0 = new DateTime(2001,1,1);

		return new {	note = d["Note"].Value,
						filename = d["PictureNote"].valueOf("FileName"),
						thumbnail = thumbnail,
						date = t0+ new TimeSpan(0,0,(int)(Single.Parse(d["Timestamp"].valueOf("NS.time")))),
							};
	})
	.Dump()
	;
	
	File.WriteAllText(@"c:\temp\firstYear1.html",
	String.Format("<html><body>{0}",
	String.Join("<br/><br/>",
		html.Select(_=>String.Format("<img src='file:///{0}'/><br/>{1} @{2}", _.thumbnail, _.note, _.date)))));
		
	return true;
	
	var dicts = root.Element("array").Elements("dict");
	
	var pictures = dicts.Where(_=>_.Elements().First().Value == "Note" ).Take(1);
	var thumbnails = dicts.Where(_=>_.Elements().First().Value == "Thumbnail" ).Take(1);
	
	pictures.Dump();
	
	foreach( var dict in dicts.Skip(0).Take(100) )
	//foreach( var dict in thumbnails )
	{
		//dict.Dump("dict");
		dict2json( dict ).Dump();
	}
	
	return true;
}

IEnumerable<string> dict2json(XElement dict, int depth = 0)
{
	var kvs = dict.Elements();
	
	if(!kvs.Any())
	{
		List<string> res = new List<string>();
		
		if( dict.Name == "data" )
		{
			res.Add("'=== DATA ==='");
		}
		else if(!String.IsNullOrEmpty(dict.Value))
		{
			res.Add(String.Format("'{0}'",dict.Value));
		}
		else
		{
			res.Add(String.Format("'{0}'",dict.Name.ToString()));
		}
		
		return res;
	}
	
	//kvs.Dump("kvs");
		
	var indentation = Enumerable.Range(0,depth).Aggregate(new StringBuilder(), (a,n) => a.Append("\t")).ToString();
	
	return kvs.Zip(kvs.Skip(1), (k,v) => String.Format("{{\n\t{2}'{0}': {1}\n{2}}}", k.Value, String.Join( "\n", dict2json(v, depth+1)), indentation) )
				.Where((_,i)=>0 == i%2);
}