<Query Kind="Program">
  <Reference>&lt;ProgramFilesX86&gt;\Open XML SDK\V2.5\lib\DocumentFormat.OpenXml.dll</Reference>
  <Reference Relative="..\..\tools\TLC_2.6.45.0.Single\Microsoft.MachineLearning.CommandLine.dll">C:\Dev\tools\TLC_2.6.45.0.Single\Microsoft.MachineLearning.CommandLine.dll</Reference>
  <Reference Relative="..\..\tools\TLC_2.6.45.0.Single\Microsoft.MachineLearning.Core.dll">C:\Dev\tools\TLC_2.6.45.0.Single\Microsoft.MachineLearning.Core.dll</Reference>
  <Reference Relative="..\..\tools\TLC_2.6.45.0.Single\Microsoft.MachineLearning.Learners.dll">C:\Dev\tools\TLC_2.6.45.0.Single\Microsoft.MachineLearning.Learners.dll</Reference>
  <Reference>&lt;ProgramFilesX86&gt;\Microsoft Visual Studio 12.0\Visual Studio Tools for Office\PIA\Office15\Microsoft.Office.Interop.OneNote.dll</Reference>
  <Reference>&lt;ProgramFilesX64&gt;\Common Files\microsoft shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.dll</Reference>
  <Reference>&lt;ProgramFilesX64&gt;\Common Files\microsoft shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.Runtime.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.dll</Reference>
  <Reference Relative="..\..\tools\TLC_2.6.45.0.Single\TMSNlearnPrediction.dll">C:\Dev\tools\TLC_2.6.45.0.Single\TMSNlearnPrediction.dll</Reference>
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

void Main()
{
	var filename = @"C:\Users\maxlevy\AppData\Local\Temp\dataFrom_max_Log1.dat.xml_bigArray.xml";
	
	var xdoc = XDocument.Load(filename);
	
	var keys = xdoc.Descendants("key");
	
	// remove all togather each               <key>$classes</key><array>...
	keys.ToList()
		.Where(_=>_.Value=="$classes")
		.Select(_=>
		{
			var arr = _.ElementsAfterSelf().First();
			
			if( arr.Name != "array" )
			{
				throw new ApplicationException("expected to find a dict, found " + arr.Name + " with value=" + _.Value);
			}
			
			arr.Remove();
			_.Remove();
			
			return true;
		}
		)
		.Count()
		.Dump()
		;
	
	
	// 1. <key> followed by a <dict> wiht a single element
	keys.Select(k=>
	{
		var dict = k.ElementsAfterSelf().First();
	
		if( dict.Name == "true" || dict.Name == "false")
		{
			// the trivial one
			k.Add( new XAttribute("val", dict.Name) );
			dict.Remove();
			return true;
		}
	
		if( dict.Name == "string" 
			|| dict.Name == "real" 
			|| dict.Name == "integer"
			|| dict.Name == "data")
		{
			// the trivial one
			k.Add( new XAttribute("val", dict.Value) );
			dict.Remove();
			return true;
		}
		
		if( k.Value == "NS.objects" )
		{
			if( null == dict.Elements() )
			{
				dict.Remove();
				k.Remove();
				
				return true;
			}
			
			return false;		
		}
		
		if( dict.Name != "dict" )
		{
			throw new ApplicationException("expected to find a dict, found " + dict.Name + " for key value=" + k.Value);
		}
		
		if( 1 == dict.Elements().Count()
			&& dict.Elements().First().Name == "string" )
		{
			k.Add( new XAttribute("val", dict.Elements().First().Value) );
			dict.Remove();
			return true;
		}
		
		return true;
	})
	.Count()
	.Dump("Step 1")
	;
	
	
	// 2. <key>Timestamp</key> has too much data
	flatten(keys, "Timestamp", "NS.time" );
	
	// 3 <key>$class</key> is also too fat
	flatten(keys, "$class", "$classname" );
	
	// 4 <key>Time</key>
	flatten(keys, "Time", "NS.time" );
	
	flatten(keys, "DOB", "NS.time" );

	flatten(keys, "ObjectID", "NS.string" );

	flatten(keys, "FileName", "NS.string" );

	flatten(keys, "ActivityID", "NS.string" );
			
	// 5. <dict> inside of <dict>
	xdoc.Descendants("dict")
		.Where(_=>_.Elements().Count() == 1 && _.Elements().First().Name == "dict" )
		.ToList()
		.Select(_=>
		{
			var inner_dict = _.Element("dict");
			inner_dict.Remove();
			_.ReplaceWith(inner_dict);
			return true;
		}
		)
		.Count()
		.Dump("dbl-dict");
	
	
//    <dict>
//      <key val="NSArray">$class</key>
//      <key>NS.objects</key>
//      <array></array>
//    </dict>
	xdoc.Descendants("array")
		.ToList()
		.Where(_=>_.Elements() == null || !_.Elements().Any() )
		.Select(_=>
		{
			if( _.Parent.ElementsBeforeSelf().Last().Name == "key" )
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
		.Dump("empty arrays")
		;
		

	// move the attributes
	xdoc.Descendants("dict")
		.Where(_=>_.Elements()
					.Where(k=>	k.Name == "key"
								&& null != k.Attribute("val")).Count() == _.Elements().Count() )
		.ToList()
		.Select(_=>
	{
		var dst = _.ElementsBeforeSelf().LastOrDefault();
		if( null != dst )
		{
			var lst = new List<XAttribute>();
			
			_.Elements()
				.ToList()
				.Select(k=> {	lst.Add( new XAttribute(k.Value.Replace('$','_'), k.Attribute("val").Value ));
								return 0;
							})
				.Count();
				
			lst.Select(att=>{ dst.Add(att); return true; }).Count();
	
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
	.Dump()
	;
	
	// time
	xdoc.Descendants("key")
		.Where(_=>_.Value == "Timestamp" || _.Value == "Time")
		.Select(_=>
	{
		var t = new DateTime(2001, 1, 1)
			+ new TimeSpan(0, 0, (int)(Single.Parse(_.Attributes().Single().Value)))
			+ TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);

		_.Attributes().Single().Value = t.ToString();
		
		return 0;
	})
	.Count()
	.Dump("Time fixed")
	;
	

	xdoc.Descendants("key")
		.Where(_=>_.Value == "Thumbnail")
		.Select(_=>
	{
		var f = _.ElementsAfterSelf("key")
					.Where(fn=>fn.Value == "FileName")
					.Single()
					.Attributes()
					.Single()
					.Value;

		var newFilename = Path.Combine( Path.GetTempPath(), f+ "_.png");
		
		if( !File.Exists(newFilename) )
		{
			File.Copy(_.Attribute("NS.data").Value, newFilename);
		}
	
		_.Attribute("NS.data").Value = newFilename;
		
		return 0;
			
	})
	.Count();
	
	
	
	xdoc.Descendants("dict")
		.Where(_=>null == _.Elements() || !_.Elements().Any() )
		.ToList()
		.Select(_=>{_.Remove(); return 0;})
		.Count()
		.Dump("empty dicts")
		;
		
		
	xdoc.Element("root").Elements("dict")
		.GroupBy(_=>_.Elements("key").Where(k=>k.Value=="$class").Single().Attributes().Single().Value	
					+ "_" + _.Elements("key").Where(k=>k.Value=="ObjectID").Single().Attributes().Single().Value)
		//.Where(gr=>gr.Any(g=>g.Descendants("key").Where(d=>d.Value == "Thumbnail").Any()))
		//.Select(gr=>gr.OrderBy(g=>g.Elements("key").Where(d=>d.Value == "Timestamp").Single().Value).Last())
		//.OrderBy(_=>_.Key)
		//.Where(_=>_.Key.StartsWith( "Joy" ) )d
		.Dump();

	
	xdoc.Save(filename + ".xml");
	
}

// Define other methods and classes here
void flatten(IEnumerable<XElement> keys, string v1, string v2)
{
	keys.Where(_=>_.Value == v1 && null == _.Attribute("val") )
		.Select(_=>
		{
			var nxt = _.ElementsAfterSelf().First();
			
			if( nxt.Name != "dict" )
			{
				throw new ApplicationException("wrong element next: " + nxt.Name + " with value " + nxt.Value);
			}
			
			try
			{
				_.Add( new XAttribute("val", nxt	.Descendants("key")
													.Where(d=>d.Value == v2)
													.First()
													.Attributes()
													.First()
													.Value ) );
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