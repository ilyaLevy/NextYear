<Query Kind="Program">
  <Connection>
    <ID>d0f4b1f9-cb11-43da-8fba-767d7911f61e</ID>
    <Persist>true</Persist>
    <Driver Assembly="IQDriver" PublicKeyToken="5b59726538a49684">IQDriver.IQDriver</Driver>
    <Provider>System.Data.SQLite</Provider>
    <CustomCxString>Data Source=C:\Users\maxlevy\Dropbox\Apps\FirstYear\sinkv2\Ilya, m3__2A96AA59-7C83-4CD6-946E-1CCE4DED7BBA\4F9DA8D8-9B8C-4787-A30D-20A9D99BD4A1__F099DF1E-77CD-4133-8143-773C84151FDB\test.dbseed;FailIfMissing=True</CustomCxString>
    <AttachFileName>C:\Users\maxlevy\Dropbox\Apps\FirstYear\sinkv2\Ilya, m3__2A96AA59-7C83-4CD6-946E-1CCE4DED7BBA\4F9DA8D8-9B8C-4787-A30D-20A9D99BD4A1__F099DF1E-77CD-4133-8143-773C84151FDB\test.dbseed</AttachFileName>
    <DisplayName>FirstYear</DisplayName>
    <DriverData>
      <StripUnderscores>false</StripUnderscores>
      <QuietenAllCaps>false</QuietenAllCaps>
    </DriverData>
  </Connection>
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
	var photos = new List<XElement>();
	
	photos.AddRange(
		Joys
			.Where(_=>_.HasPicture.ToString() != "0").ToList().Select(_=> makePhoto(_))
			);

	photos.AddRange(
		Milestones
			.Where(_=>_.HasPicture.ToString() != "0").ToList().Select(_=> makePhoto(_))
			);
	
	photos.AddRange(
		Sleeps
			.Where(_=>_.HasPicture.ToString() != "0").ToList().Select(_=> makePhoto(_))
			);
	
	photos.AddRange(
		Growths
			.Where(_=>_.HasPicture.ToString() != "0").ToList().Select(_=> makePhoto(_))
			);

	photos.AddRange(
		Diapers
			.Where(_=>_.HasPicture.ToString() != "0").ToList().Select(_=> makePhoto(_))
			);

	photos.AddRange(
		Formulas
			.Where(_=>_.HasPicture.ToString() != "0").ToList().Select(_=> makePhoto(_))
			);

	// load the URLs we have already translated
	var oldData = XDocument.Load(@"C:\Dev\Quick\FirstYear\localDb_dbseed.xml").Descendants("photo");
	
	photos.Select(_=>
	{
		var urlAttr = new XAttribute("url", Path.GetFileNameWithoutExtension(thumbPath(_.Attribute("id").Value)));
		
		var cached = oldData.FirstOrDefault(o=>o.Attribute("id").Value == urlAttr.Value);
		if( cached != null )
		{
			_.Attribute("url").Value = cached.Attribute("url").Value;
		}
		return 0;
	})
	.Count()
	;
	
	// dump the files with no URLs:
	photos	.Where(_=>!_.Attribute("url").Value.StartsWith("http"))
			.Select(_=>_.Attribute("url").Value)
			.Dump("Files with no URLs");
	
	
	var doc = new XDocument();
	doc.Add(new XElement("root", photos.OrderBy(_=>DateTime.Parse(_.Attribute("date").Value))));
	doc.Save(@"C:\Dev\Quick\FirstYear\dbseed.xml");
	
	(Pictures.Count() - photos.Count()).Dump("Missing photos");
	
	//  <photo date="2014-08-14T17:45:52" 
	//		id="361c7902-175c-46c7-8cf8-5f4724657570" 
	//		thumbUrl="C:\Users\maxlevy\AppData\Local\Temp\tmp5C68.tmp.png" 
	//		title="This is a room where we are going to spend the next week " 
	//		url="https://www.dropbox.com/s/vzxtafr22qz0f6g/361c7902-175c-46c7-8cf8-5f4724657570.jpg" />
	
	(
		Joys.Where(_=>_.HasPicture.ToString() == "1" ).Count()
		+ Milestones.Where(_=>_.HasPicture.ToString() == "1" ).Count()
		+ Baths.Where(_=>_.HasPicture.ToString() == "1" ).Count()
		- Pictures.Count()
	)
	.Dump();
	
	var lps = Pictures
		.Select(_=>_.ActivityID)
		.Except(Joys.Select(_=>_.ID))
		.Except(Milestones.Select(_=>_.ID))
		.Except(Sleeps.Select(_=>_.ID))
		.Except(Formulas.Select(_=>_.ID))
//		.Except(Baths.Select(_=>_.ID))
		.Except(Diapers.Select(_=>_.ID))
		.Except(Growths.Select(_=>_.ID))
	//	.Dump()
		;
		
		lps.Count().Dump("Outstanding pictures");
		
	var ps = Pictures
		.Where(_=>lps.Contains(_.ActivityID))
		.ToList();
		ps.Select(_=>
		{
			var thumbnail = Path.Combine(Path.GetTempPath(), _.ActivityID +".png");
			
			File.WriteAllBytes(	thumbnail, _.Thumbnail);
			
			return thumbnail;
		}
		)
		.Select(_=>String.Format("<img src='file:///{0}'/> <br/>", _) )
		;
}

// Define other methods and classes here
string thumbPath(string id)
{
	var picture = Pictures.Single(_=>_.ActivityID == id);
	
	var filename = Path.Combine(@"C:\Dev\Quick\FirstYear\db", picture.FileName +".png");
	
	File.WriteAllBytes(	filename, picture.Thumbnail);
	File.WriteAllText(	filename + ".base64", System.Convert.ToBase64String(picture.Thumbnail));
	
	return filename;
}

string getTime(float time)
{
	var t0 = new DateTime(1970,1,1);
	
	return	(t0
			+ new TimeSpan(0,0,(int)(time))
			//+ TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now)
			).ToString();
			;
}

XElement makePhoto(dynamic _)
{
	return new XElement("photo", new XAttribute[] {	new XAttribute("title",_.Note ?? ""),
																	new XAttribute("date",getTime(_.Time)),
																	new XAttribute("id",_.ID),
																	new XAttribute("thumbUrl",thumbPath(_.ID)),
																	new XAttribute("url", Path.GetFileNameWithoutExtension(thumbPath(_.ID))),
																	new XAttribute("src","Joys"),
																	}
																	);
}
