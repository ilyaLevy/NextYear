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

class FYPhoto
{
	public FYPhoto(){}
	
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

void Main()
{
	extractData(@"C:\Users\maxlevy\AppData\Local\Temp\dataFromDevice_0.dat.xml");
	return;



//	string dataFilename = @"C:\Dev\Quick\FirstYear\localDb_dbseed.xml";
	string dataFilename = @"C:\Dev\Quick\FirstYear\localDb.xml";
	//saveDbseed(filename);
	
	var sinkv2Root = @"C:\Users\maxlevy\Dropbox\Apps\FirstYear\sinkv2\Ilya, m3__2A96AA59-7C83-4CD6-946E-1CCE4DED7BBA\";
	
	var devices = new string[] {
	"27F69006-E9BE-4B14-A056-CF1A9CE27A81__64D58B9C-CEA1-42AB-9A1A-C764EF159C1B",
	//"4F9DA8D8-9B8C-4787-A30D-20A9D99BD4A1__D8BDFCC3-735E-43E0-A4BA-940F224838BF",
	};
	
	var freshPhotos = devices.Select((d,i)=>
	{
		//var tmp = Path.GetTempFileName();
		var tmp = String.Format(@"C:\Users\maxlevy\AppData\Local\Temp\dataFromDevice_{0}.dat", i);
		File.Copy(sinkv2Root + d + @"\TransactionLog0.tlog",tmp,true);
		
		Process.Start(	@"C:\Dev\Quick\FirstYear\Translator.exe",
						String.Format("{0} {1}", tmp, tmp + ".xml"))
				.WaitForExit();
				
		return processTransactionLog(tmp + ".xml");
	}
	)
	.SelectMany(_=>_)
	.ToList()
	//.Dump()
	;
	
	// if needed update the local db
	updateLocalDb(freshPhotos, dataFilename);
	
	Process.Start(	@"c:\Python27\python.exe",
					@"C:\Dev\Quick\FirstYear\cli_client.py")
			.WaitForExit();
			
	var templateFilename = @"C:\Dev\Quick\FirstYear\Ilya Daily, template.html";
	var outputFilename = @"C:\Dev\Quick\FirstYear\Ilya Daily.html";
	
	buildHtml(dataFilename, templateFilename, outputFilename);
	
	//ftpResults(outputFilename);
}

void ftpResults(string filename)
{
	FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://server23.000webhost.com/public_html/test.html");
	request.Method = WebRequestMethods.Ftp.UploadFile;
	
	// This example assumes the FTP site uses anonymous logon.
	request.Credentials = new NetworkCredential ("a1294892","1qaz2wsx");
	
	// Copy the contents of the file to the request stream.
	StreamReader sourceStream = new StreamReader(filename);
	byte [] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
	sourceStream.Close();
	request.ContentLength = fileContents.Length;
	
	Stream requestStream = request.GetRequestStream();
	requestStream.Write(fileContents, 0, fileContents.Length);
	requestStream.Close();
	
	FtpWebResponse response = (FtpWebResponse)request.GetResponse();
	
	String.Format("Upload File Complete, status {0}", response.StatusDescription).Dump();
	
	response.Close();
}

// Define other methods and classes here
void saveDbseed(string filename)
{
	var thumbUrls = new string[] {
	"B563F8B4-4961-44D1-B600-3170B0FA30FE",  "https://www.dropbox.com/s/g6d4fpmoxxuxg2p/B563F8B4-4961-44D1-B600-3170B0FA30FE%20%28Mobile%29.jpg",
	"68CEE8C5-DEB9-4B04-88B6-E328DC2B3972",  "https://www.dropbox.com/s/lw4irj1ie5fkg7a/68CEE8C5-DEB9-4B04-88B6-E328DC2B3972%20%28Mobile%29.jpg",
	"121A3244-10BA-4100-A7BC-3F2CD17D2C68",  "https://www.dropbox.com/s/g85d3hirpzto0hg/121A3244-10BA-4100-A7BC-3F2CD17D2C68%20%28Mobile%29.jpg",
	"E66036DB-9217-478B-8645-4CBB55115C94",  "https://www.dropbox.com/s/tgb2y6tqyh0407f/E66036DB-9217-478B-8645-4CBB55115C94%20%28Mobile%29.jpg",
	"15442617-BC9D-4638-9B91-F991E21CC46A",  "https://www.dropbox.com/s/qkbgdytd9dfaav8/15442617-BC9D-4638-9B91-F991E21CC46A%20%28Mobile%29.jpg",
	"C3B029A4-8141-4C46-939F-CEA74CBD21B6",  "https://www.dropbox.com/s/3cqzl14ksun75eg/C3B029A4-8141-4C46-939F-CEA74CBD21B6%20%28Mobile%29.jpg",
	"C939B7C0-F996-4D9B-92A1-8B0EB9F5CC15",  "https://www.dropbox.com/s/k4pbsywiq3bzh92/C939B7C0-F996-4D9B-92A1-8B0EB9F5CC15%20%28Mobile%29.jpg",
	"8966670E-FE1D-40AA-ABCE-7DAC1697B581",  "https://www.dropbox.com/s/ehbrj5jnypkidv6/8966670E-FE1D-40AA-ABCE-7DAC1697B581%20%28Mobile%29.jpg",
	"ABF17730-0D8F-4C41-837D-9904F6332DDB",  "https://www.dropbox.com/s/md1d3s4xanfvs1g/ABF17730-0D8F-4C41-837D-9904F6332DDB%20%28Mobile%29.jpg",
	"BA454E3F-EE34-4576-B335-F65A568A7B7B",  "https://www.dropbox.com/s/s6l3tr16umrp6z3/BA454E3F-EE34-4576-B335-F65A568A7B7B%20%28Mobile%29.jpg",
	"B7E6488C-B792-4769-AD3C-68F3CF877C94",  "https://www.dropbox.com/s/ms2xdy49kyqzwg7/B7E6488C-B792-4769-AD3C-68F3CF877C94%20%28Mobile%29.jpg",
	"A825597D-FC24-45C6-AB0F-7239CD0DC2A1",  "https://www.dropbox.com/s/6ynxqql6gount42/A825597D-FC24-45C6-AB0F-7239CD0DC2A1%20%28Mobile%29.jpg",
	"071A2D4E-737D-4B1D-A7D9-6D23BCD62A4F",  "https://www.dropbox.com/s/xfiio6h1u8is3lw/071A2D4E-737D-4B1D-A7D9-6D23BCD62A4F%20%28Mobile%29.jpg",
	"EC08AA0D-16D7-4B8B-B5B8-DE3E27A8DB5D",  "https://www.dropbox.com/s/06nq6s3add50t1x/EC08AA0D-16D7-4B8B-B5B8-DE3E27A8DB5D%20%28Mobile%29.jpg",
	"2932000E-C17F-45A4-A44F-16A070262665",  "https://www.dropbox.com/s/gy4stqxibhp9pl9/2932000E-C17F-45A4-A44F-16A070262665%20%28Mobile%29.jpg",
	"6DEEEC32-D7C2-4481-AEB5-91CA3D987CAC",  "https://www.dropbox.com/s/ekssvqn4gqkltp9/6DEEEC32-D7C2-4481-AEB5-91CA3D987CAC%20%28Mobile%29.jpg",
	"1176A28A-D63B-459D-B4E8-BB82225575D1",  "https://www.dropbox.com/s/6d2xl6u5d4hw687/1176A28A-D63B-459D-B4E8-BB82225575D1%20%28Mobile%29.jpg",
	"1E4589C6-BE6B-43EB-8874-F9B90C4EAE1B",  "https://www.dropbox.com/s/j0b5w5bq40khnih/1E4589C6-BE6B-43EB-8874-F9B90C4EAE1B%20%28Mobile%29.jpg",
	"0B7FBEAD-F731-496B-B530-91F5AEA27549",  "https://www.dropbox.com/s/ms800x41q2shv4z/0B7FBEAD-F731-496B-B530-91F5AEA27549%20%28Mobile%29.jpg",
	"C28A787C-AEAB-48D0-9CC0-E4EDF8D1114A",  "https://www.dropbox.com/s/dkfcg44qdxf7llt/C28A787C-AEAB-48D0-9CC0-E4EDF8D1114A%20%28Mobile%29.jpg",
	"98B50BA7-0975-47D2-9E97-4AED16DDF801",  "https://www.dropbox.com/s/v4zb78o39warpys/98B50BA7-0975-47D2-9E97-4AED16DDF801%20%28Mobile%29.jpg",
	"862F6C8B-E756-4898-911E-58A14B60F457",  "https://www.dropbox.com/s/bwqln3r3l7q24tz/862F6C8B-E756-4898-911E-58A14B60F457%20%28Mobile%29.jpg",
	"7E027533-5344-4503-811D-41C2020D42DD",  "https://www.dropbox.com/s/o6adun4xvnpcgdw/7E027533-5344-4503-811D-41C2020D42DD%20%28Mobile%29.jpg",
	"A628D515-807D-491A-8327-51F5FA397619",  "https://www.dropbox.com/s/bsc8915ferlh9o2/A628D515-807D-491A-8327-51F5FA397619%20%28Mobile%29.jpg",
	"791194AD-3255-47F6-94FA-6CDF31A67F91",  "https://www.dropbox.com/s/0esgzptpzcuhnsf/791194AD-3255-47F6-94FA-6CDF31A67F91%20%28Mobile%29.jpg",
	"D5318055-24E3-4284-A7D7-002C7066381D",  "https://www.dropbox.com/s/d5f8wwxw6e1h3u7/D5318055-24E3-4284-A7D7-002C7066381D%20%28Mobile%29.jpg",
	"DD77498E-FCE6-430A-A3D3-E79CD7150F7A",  "https://www.dropbox.com/s/oh9rkgatztm8srs/DD77498E-FCE6-430A-A3D3-E79CD7150F7A%20%28Mobile%29.jpg",
	"1AC74A23-697C-46A2-B5AB-C48ECAFA1C02",  "https://www.dropbox.com/s/uky2p4xnslzwxhu/1AC74A23-697C-46A2-B5AB-C48ECAFA1C02%20%28Mobile%29.jpg",
	"6A6F684B-C524-44FF-A59F-019D91700406",  "https://www.dropbox.com/s/0d86a99jlkwmk1o/6A6F684B-C524-44FF-A59F-019D91700406%20%28Mobile%29.jpg",
	"357D36CF-756C-41C3-ACEF-2DD81580B820",  "https://www.dropbox.com/s/3ch7nbvkrz8v65p/357D36CF-756C-41C3-ACEF-2DD81580B820%20%28Mobile%29.jpg",
	"05B3CF61-FEB0-41CD-93AA-740135B842CC",  "https://www.dropbox.com/s/1xagg98bpx9qgr3/05B3CF61-FEB0-41CD-93AA-740135B842CC%20%28Mobile%29.jpg",
	"D6C6A3C8-0594-41EB-9C19-C2B82F835AD8",  "https://www.dropbox.com/s/w8kw98en5dl4phu/D6C6A3C8-0594-41EB-9C19-C2B82F835AD8%20%28Mobile%29.jpg",
	"A7879F19-3ECF-4C0F-93C5-9BE24E228977",  "https://www.dropbox.com/s/0uz0gwsuclygd5e/A7879F19-3ECF-4C0F-93C5-9BE24E228977%20%28Mobile%29.jpg",
	"0CB9E522-BB28-418B-835D-96B7E2A900E4",  "https://www.dropbox.com/s/mofegu6h8x4jma6/0CB9E522-BB28-418B-835D-96B7E2A900E4%20%28Mobile%29.jpg",
	"FFAC4C88-F266-4358-B617-012BBA6CAE71",  "https://www.dropbox.com/s/jfr4xcgm8vj9j9d/FFAC4C88-F266-4358-B617-012BBA6CAE71%20%28Mobile%29.jpg",
	"EB46CEFC-7CAD-4419-8237-3238C715659B",  "https://www.dropbox.com/s/1lf18bjvdulid0v/EB46CEFC-7CAD-4419-8237-3238C715659B%20%28Mobile%29.jpg",
	"430AEA3B-AA17-4096-9BB5-FDF0B1E210DB",  "https://www.dropbox.com/s/38i1veegke3vfgo/430AEA3B-AA17-4096-9BB5-FDF0B1E210DB%20%28Mobile%29.jpg",
	"C7595712-66F5-4559-886A-EE01BABDFA84",  "https://www.dropbox.com/s/a0effsssj3919w5/C7595712-66F5-4559-886A-EE01BABDFA84%20%28Mobile%29.jpg",
	"CB5E48CE-8978-42E6-A901-5D058B76041C",  "https://www.dropbox.com/s/w342o50et951hrt/CB5E48CE-8978-42E6-A901-5D058B76041C%20%28Mobile%29.jpg",
	"74213DC1-58F8-4FC5-8B67-2CFCB2B823E5",  "https://www.dropbox.com/s/fqhpf7r5emfvchl/74213DC1-58F8-4FC5-8B67-2CFCB2B823E5%20%28Mobile%29.jpg",
	"CF5367DF-FCB3-4B8B-AD21-17B75D3B2FD8",  "https://www.dropbox.com/s/esqf493vuyfmfzu/CF5367DF-FCB3-4B8B-AD21-17B75D3B2FD8%20%28Mobile%29.jpg",
	"9FF48AE9-CDFF-493F-AE55-37D2F9C91A02",  "https://www.dropbox.com/s/jzdtkc1o5tomqev/9FF48AE9-CDFF-493F-AE55-37D2F9C91A02%20%28Mobile%29.jpg",
	"5F2C6E34-9056-4A2A-9E69-7AF61C5CFB27",  "https://www.dropbox.com/s/5kttuw3itv5gwcc/5F2C6E34-9056-4A2A-9E69-7AF61C5CFB27%20%28Mobile%29.jpg",
	"AAC6EE43-6416-43B1-BF16-8E2CA53F19E7",  "https://www.dropbox.com/s/wrvklpbpqunfibx/AAC6EE43-6416-43B1-BF16-8E2CA53F19E7%20%28Mobile%29.jpg",
	"43A8314F-A9CF-4B1A-8898-150FAE7EA843",  "https://www.dropbox.com/s/a9sbgfoljqvp7fi/43A8314F-A9CF-4B1A-8898-150FAE7EA843%20%28Mobile%29.jpg",
	"479A9570-9701-4AFA-90F6-CE1D4D7ACAC2",  "https://www.dropbox.com/s/r73o7elomul08zm/479A9570-9701-4AFA-90F6-CE1D4D7ACAC2%20%28Mobile%29.jpg",
	"658413E1-71D2-4BB8-9916-7C7B2C7E9DA7",  "https://www.dropbox.com/s/nwdtg0ehyyk4eya/658413E1-71D2-4BB8-9916-7C7B2C7E9DA7%20%28Mobile%29.jpg",
	"F4029444-D834-40D4-BC6B-75D18F4187F3",  "https://www.dropbox.com/s/ase5d5vtmtfpnb5/F4029444-D834-40D4-BC6B-75D18F4187F3%20%28Mobile%29.jpg",
	"1F09F78F-8E25-4440-8A0C-BECA38091607",  "https://www.dropbox.com/s/kap99gnzyl5gkup/1F09F78F-8E25-4440-8A0C-BECA38091607%20%28Mobile%29.jpg",
	"34BB87E8-1835-4706-A4AC-43394F2A1235",  "https://www.dropbox.com/s/d0dj50bayqgu56w/34BB87E8-1835-4706-A4AC-43394F2A1235%20%28Mobile%29.jpg",
	"6099712A-1F18-4638-8957-7AE4CA01387D",  "https://www.dropbox.com/s/6460rl6pril96df/6099712A-1F18-4638-8957-7AE4CA01387D%20%28Mobile%29.jpg",
	"07D498BC-6706-419B-95C9-BAAA7538B0C0",  "https://www.dropbox.com/s/new6uue53v91k12/07D498BC-6706-419B-95C9-BAAA7538B0C0%20%28Mobile%29.jpg",
	"2FEBDA32-DCCF-4697-A790-B7EEF0BAEDEA",  "https://www.dropbox.com/s/phdoep5rla85hyc/2FEBDA32-DCCF-4697-A790-B7EEF0BAEDEA%20%28Mobile%29.jpg",
	"01FA99E2-237E-4AAE-B162-E4230BD30216",  "https://www.dropbox.com/s/rn1g0h3bnwlw73q/01FA99E2-237E-4AAE-B162-E4230BD30216%20%28Mobile%29.jpg",
	"7DD1363A-666A-4930-9D69-283F39CAD44E",  "https://www.dropbox.com/s/x0huhzrsn3b5en4/7DD1363A-666A-4930-9D69-283F39CAD44E%20%28Mobile%29.jpg",
	"F1B19EE5-B776-4C53-95B6-D6F2BA4678EB",  "https://www.dropbox.com/s/pq06mo7kbejebdn/F1B19EE5-B776-4C53-95B6-D6F2BA4678EB%20%28Mobile%29.jpg",
	"8FAC574A-3405-4FE5-9476-1C425CAA0531",  "https://www.dropbox.com/s/1evqzrki6s7syn8/8FAC574A-3405-4FE5-9476-1C425CAA0531%20%28Mobile%29.jpg",
	"4B248B2D-645B-449C-84F5-977FB66F5D6F",  "https://www.dropbox.com/s/3vnp0y70ll3ye99/4B248B2D-645B-449C-84F5-977FB66F5D6F%20%28Mobile%29.jpg",
	"CACB9C46-CCE5-485A-886F-CE3249B62FB0",  "https://www.dropbox.com/s/inhf1ahf8a7bhzb/CACB9C46-CCE5-485A-886F-CE3249B62FB0%20%28Mobile%29.jpg",
	"98339434-369D-4552-A6BB-4FC66C9C7EE8",  "https://www.dropbox.com/s/i4dozuya19428nn/98339434-369D-4552-A6BB-4FC66C9C7EE8%20%28Mobile%29.jpg",
	"0C79676F-9CB0-4C01-A828-4F8F8A9F0A9B",  "https://www.dropbox.com/s/9jpknw3ry1kk06m/0C79676F-9CB0-4C01-A828-4F8F8A9F0A9B%20%28Mobile%29.jpg",
	"7A6C41BC-F274-429F-ABB1-E5CB2EA7A167",  "https://www.dropbox.com/s/9qrp9xaksgo42tq/7A6C41BC-F274-429F-ABB1-E5CB2EA7A167%20%28Mobile%29.jpg",
	"563F26C2-2929-49B3-BF3B-72E2B3C9F2D3",  "https://www.dropbox.com/s/ojpg8z5dfa6oq4o/563F26C2-2929-49B3-BF3B-72E2B3C9F2D3%20%28Mobile%29.jpg",
	"874A7268-BF80-48A4-9B45-2DCD05F47A88",  "https://www.dropbox.com/s/5qfxwendwjw3hec/874A7268-BF80-48A4-9B45-2DCD05F47A88%20%28Mobile%29.jpg",
	"F6DB9290-BF05-43A5-B0C2-302BB4BAE920",  "https://www.dropbox.com/s/tconn8h7sqf2dqv/F6DB9290-BF05-43A5-B0C2-302BB4BAE920%20%28Mobile%29.jpg",
	"1C701F16-211D-41AD-8C7D-3A9A0C6ED369",  "https://www.dropbox.com/s/ldejn3z1picigzj/1C701F16-211D-41AD-8C7D-3A9A0C6ED369%20%28Mobile%29.jpg",
	"4977E626-1425-44DC-B8F3-527A0F7ABE26",  "https://www.dropbox.com/s/wkoxto1saljpmkq/4977E626-1425-44DC-B8F3-527A0F7ABE26%20%28Mobile%29.jpg",
	"46BDC68F-F6D1-448E-B898-738DF944ADD8",  "https://www.dropbox.com/s/9k2nb6kjmamh2eh/46BDC68F-F6D1-448E-B898-738DF944ADD8%20%28Mobile%29.jpg",
	"7B78BD6B-C17D-4306-B0EE-073C8186B2D7",  "https://www.dropbox.com/s/4i37s1fhle8gmi3/7B78BD6B-C17D-4306-B0EE-073C8186B2D7%20%28Mobile%29.jpg",
	"A6585CBF-E349-459A-AD77-61B1B4DB2652",  "https://www.dropbox.com/s/s17v2nxn6us36ds/A6585CBF-E349-459A-AD77-61B1B4DB2652%20%28Mobile%29.jpg",
	"30FE19E1-3E27-4495-9DCD-D381F2CD185B",  "https://www.dropbox.com/s/jkfy1rx1n5p4hle/30FE19E1-3E27-4495-9DCD-D381F2CD185B%20%28Mobile%29.jpg",
	"121FCA94-D1C3-4E73-A95A-92A8466E37A5",  "https://www.dropbox.com/s/o7exgk18ajimb7e/121FCA94-D1C3-4E73-A95A-92A8466E37A5%20%28Mobile%29.jpg",
	"EE8D45D6-B5C6-422F-B862-82669F95CE12",  "https://www.dropbox.com/s/7y54wbwwpwg3tss/EE8D45D6-B5C6-422F-B862-82669F95CE12%20%28Mobile%29.jpg",
	"83626694-FEB4-46A7-944D-A81E9515E791",  "https://www.dropbox.com/s/jbzfqki1kpfa0o9/83626694-FEB4-46A7-944D-A81E9515E791%20%28Mobile%29.jpg",
	"3F9E9412-4A52-41DD-9068-B665A2C1C56A",  "https://www.dropbox.com/s/3js9wjv7lgo6lsd/3F9E9412-4A52-41DD-9068-B665A2C1C56A%20%28Mobile%29.jpg",
	"037E77C6-9AD3-47A3-85A3-1254D721696B",  "https://www.dropbox.com/s/pdbmi9yeit66y4i/037E77C6-9AD3-47A3-85A3-1254D721696B%20%28Mobile%29.jpg",
	"013CFE7A-AF0F-4B3D-A9FD-A7D479793C86",  "https://www.dropbox.com/s/z8f6pnb5qu4lwid/013CFE7A-AF0F-4B3D-A9FD-A7D479793C86%20%28Mobile%29.jpg",
	"FDEF66DD-E8ED-4DE3-9C61-64A4BEBF9EEA",  "https://www.dropbox.com/s/bvsgpyak19dmgfr/FDEF66DD-E8ED-4DE3-9C61-64A4BEBF9EEA%20%28Mobile%29.jpg",
	"08C7F411-3BA0-42CC-8A79-17F5783111B6",  "https://www.dropbox.com/s/51sp0l5o3wka6vk/08C7F411-3BA0-42CC-8A79-17F5783111B6%20%28Mobile%29.jpg",
	"46826012-2360-44CA-AE32-6DCD7015B926",  "https://www.dropbox.com/s/yf69v41puqdszvv/46826012-2360-44CA-AE32-6DCD7015B926%20%28Mobile%29.jpg",
	"7BEB5D5C-345E-40F2-9903-78F35799DCFA",  "https://www.dropbox.com/s/45kd9u4jrqlhqtl/7BEB5D5C-345E-40F2-9903-78F35799DCFA%20%28Mobile%29.jpg",
	"80E4DE78-B2BE-4720-9046-AC17E6027F5C",  "https://www.dropbox.com/s/sxauc4c0g5o0dkh/80E4DE78-B2BE-4720-9046-AC17E6027F5C%20%28Mobile%29.jpg",
	"071623B4-1311-4974-B325-2F50B5754BD9",  "https://www.dropbox.com/s/oyfiezdu3sxmwlr/071623B4-1311-4974-B325-2F50B5754BD9%20%28Mobile%29.jpg",
	"A7DC3736-B938-40EE-AD35-10DCC7219BF7",  "https://www.dropbox.com/s/gjo0e3ejqjnhv5y/A7DC3736-B938-40EE-AD35-10DCC7219BF7%20%28Mobile%29.jpg",
	"BB2137E0-69BF-4AF4-A29F-C76D1351B302",  "https://www.dropbox.com/s/zv5omyhrb9l4fao/BB2137E0-69BF-4AF4-A29F-C76D1351B302%20%28Mobile%29.jpg",
	"B440D9EC-C26E-473F-880E-8833BD97737D",  "https://www.dropbox.com/s/5oymg7s9q922zf3/B440D9EC-C26E-473F-880E-8833BD97737D%20%28Mobile%29.jpg",
	"16B40DBC-E0CA-4F24-B51C-0BDBFA86D074",  "https://www.dropbox.com/s/4bd19uild3ecg8v/16B40DBC-E0CA-4F24-B51C-0BDBFA86D074%20%28Mobile%29.jpg",
	"71716416-DFB6-4ECD-B82F-7622EB748CE2",  "https://www.dropbox.com/s/sfwm3ddmr90hvak/71716416-DFB6-4ECD-B82F-7622EB748CE2%20%28Mobile%29.jpg",
	"2200BD74-CF05-401E-AF63-30FD55D4D1FC",  "https://www.dropbox.com/s/u0elqxan4iyejkj/2200BD74-CF05-401E-AF63-30FD55D4D1FC%20%28Mobile%29.jpg",
	"7DDEAEA0-BED7-4522-B5B7-3214008F4B2B",  "https://www.dropbox.com/s/oy7vykjtdauw1bq/7DDEAEA0-BED7-4522-B5B7-3214008F4B2B%20%28Mobile%29.jpg",
	"71759D34-4879-4889-9234-F7339B69E078",  "https://www.dropbox.com/s/ha9npjs9ulhlaz2/71759D34-4879-4889-9234-F7339B69E078%20%28Mobile%29.jpg",
	"1EDA1F4F-D31F-44CB-847E-204844E1FC17",  "https://www.dropbox.com/s/gui7t9fftszmv5k/1EDA1F4F-D31F-44CB-847E-204844E1FC17%20%28Mobile%29.jpg",
	"B5AF0E36-7595-4276-A29C-4E54C58CB58E",  "https://www.dropbox.com/s/r1it09fbjq4cqec/B5AF0E36-7595-4276-A29C-4E54C58CB58E%20%28Mobile%29.jpg",
	"49DE6B2F-D125-44B2-BE78-B6903031CE34",  "https://www.dropbox.com/s/a7n0zlwt2vqv7ke/49DE6B2F-D125-44B2-BE78-B6903031CE34%20%28Mobile%29.jpg",
	"080D89C8-6E45-48E0-A399-E2317C5C5730",  "https://www.dropbox.com/s/l9z5mqbzg9mjblk/080D89C8-6E45-48E0-A399-E2317C5C5730%20%28Mobile%29.jpg",
	"3DFDD68E-A526-4C3A-8FA8-7CD953207000",  "https://www.dropbox.com/s/mv2ouaf37f7k2s3/3DFDD68E-A526-4C3A-8FA8-7CD953207000%20%28Mobile%29.jpg",
	"855D4B5C-7784-4B1D-9797-05789DED2FB7",  "https://www.dropbox.com/s/4xqrauoj4pbevwc/855D4B5C-7784-4B1D-9797-05789DED2FB7%20%28Mobile%29.jpg",
	"1B80F9F8-41FC-4752-859F-042B8A0AAD75",  "https://www.dropbox.com/s/emnjqgi5xfn1syw/1B80F9F8-41FC-4752-859F-042B8A0AAD75%20%28Mobile%29.jpg",
	"D294770E-D257-4282-9550-35257000B485",  "https://www.dropbox.com/s/hfmk4st7a8ocodk/D294770E-D257-4282-9550-35257000B485%20%28Mobile%29.jpg",
	"25B4C492-22DA-4049-9C34-CEBAE05C83A1",  "https://www.dropbox.com/s/ri10d9kil4y7t4z/25B4C492-22DA-4049-9C34-CEBAE05C83A1%20%28Mobile%29.jpg",
	"112D4C6D-5680-4402-BC4D-DBEDD91CA7D6",  "https://www.dropbox.com/s/fnf0tafkv32m280/112D4C6D-5680-4402-BC4D-DBEDD91CA7D6%20%28Mobile%29.jpg",
	"91F3A602-D69C-4116-9A50-94CF9B42474E",  "https://www.dropbox.com/s/a97w2hroeqkwf0n/91F3A602-D69C-4116-9A50-94CF9B42474E%20%28Mobile%29.jpg",
	"71F8FB83-7696-42B9-A005-67A568A93B0D",  "https://www.dropbox.com/s/dea3r5fziqqplik/71F8FB83-7696-42B9-A005-67A568A93B0D%20%28Mobile%29.jpg",
	"D02840B4-FF3A-466C-AAEF-04B52142356A",  "https://www.dropbox.com/s/50mibzarh36al8o/D02840B4-FF3A-466C-AAEF-04B52142356A%20%28Mobile%29.jpg",
	"E0501E70-60BE-46C5-B8FF-61CEF277AE30",  "https://www.dropbox.com/s/fi9wk83zotn6n9m/E0501E70-60BE-46C5-B8FF-61CEF277AE30%20%28Mobile%29.jpg",};
	
	var urls = new string[] {
	"B563F8B4-4961-44D1-B600-3170B0FA30FE", "https://www.dropbox.com/s/5lsyjw051ntnsvr/B563F8B4-4961-44D1-B600-3170B0FA30FE.jpg",
	"68CEE8C5-DEB9-4B04-88B6-E328DC2B3972", "https://www.dropbox.com/s/zbjgo8da0xjvhah/68CEE8C5-DEB9-4B04-88B6-E328DC2B3972.jpg",
	"121A3244-10BA-4100-A7BC-3F2CD17D2C68", "https://www.dropbox.com/s/4tzesmffk5xmjcl/121A3244-10BA-4100-A7BC-3F2CD17D2C68.jpg",
	"E66036DB-9217-478B-8645-4CBB55115C94", "https://www.dropbox.com/s/j25aw5bbs3qxsnm/E66036DB-9217-478B-8645-4CBB55115C94.jpg",
	"15442617-BC9D-4638-9B91-F991E21CC46A", "https://www.dropbox.com/s/j4xkjsiufadqkr4/15442617-BC9D-4638-9B91-F991E21CC46A.jpg",
	"C3B029A4-8141-4C46-939F-CEA74CBD21B6", "https://www.dropbox.com/s/i9nsxcspnrybxli/C3B029A4-8141-4C46-939F-CEA74CBD21B6.jpg",
	"C939B7C0-F996-4D9B-92A1-8B0EB9F5CC15", "https://www.dropbox.com/s/bar7iihfc2poo9g/C939B7C0-F996-4D9B-92A1-8B0EB9F5CC15.jpg",
	"8966670E-FE1D-40AA-ABCE-7DAC1697B581", "https://www.dropbox.com/s/w57jpazd2sy79ts/8966670E-FE1D-40AA-ABCE-7DAC1697B581.jpg",
	"ABF17730-0D8F-4C41-837D-9904F6332DDB", "https://www.dropbox.com/s/4gyms7oatbxeoj9/ABF17730-0D8F-4C41-837D-9904F6332DDB.jpg",
	"BA454E3F-EE34-4576-B335-F65A568A7B7B", "https://www.dropbox.com/s/weed3jv3o3v7csv/BA454E3F-EE34-4576-B335-F65A568A7B7B.jpg",
	"B7E6488C-B792-4769-AD3C-68F3CF877C94", "https://www.dropbox.com/s/84rs4r9yqmgcum4/B7E6488C-B792-4769-AD3C-68F3CF877C94.jpg",
	"A825597D-FC24-45C6-AB0F-7239CD0DC2A1", "https://www.dropbox.com/s/d5jd4gqg6mwn00d/A825597D-FC24-45C6-AB0F-7239CD0DC2A1.jpg",
	"071A2D4E-737D-4B1D-A7D9-6D23BCD62A4F", "https://www.dropbox.com/s/19vcm9ag4v2mc33/071A2D4E-737D-4B1D-A7D9-6D23BCD62A4F.jpg",
	"EC08AA0D-16D7-4B8B-B5B8-DE3E27A8DB5D", "https://www.dropbox.com/s/69aqthkq3a399ng/EC08AA0D-16D7-4B8B-B5B8-DE3E27A8DB5D.jpg",
	"2932000E-C17F-45A4-A44F-16A070262665", "https://www.dropbox.com/s/m6gm8fjdltrhlhe/2932000E-C17F-45A4-A44F-16A070262665.jpg",
	"6DEEEC32-D7C2-4481-AEB5-91CA3D987CAC", "https://www.dropbox.com/s/r5hgzxea7crydhd/6DEEEC32-D7C2-4481-AEB5-91CA3D987CAC.jpg",
	"1176A28A-D63B-459D-B4E8-BB82225575D1", "https://www.dropbox.com/s/z33o38fia9219n1/1176A28A-D63B-459D-B4E8-BB82225575D1.jpg",
	"1E4589C6-BE6B-43EB-8874-F9B90C4EAE1B", "https://www.dropbox.com/s/754xt3cm0j0ywfx/1E4589C6-BE6B-43EB-8874-F9B90C4EAE1B.jpg",
	"0B7FBEAD-F731-496B-B530-91F5AEA27549", "https://www.dropbox.com/s/c3mhoruvoyac6h4/0B7FBEAD-F731-496B-B530-91F5AEA27549.jpg",
	"C28A787C-AEAB-48D0-9CC0-E4EDF8D1114A", "https://www.dropbox.com/s/tayuozptrqjny7p/C28A787C-AEAB-48D0-9CC0-E4EDF8D1114A.jpg",
	"98B50BA7-0975-47D2-9E97-4AED16DDF801", "https://www.dropbox.com/s/r55h5dpw3afu94z/98B50BA7-0975-47D2-9E97-4AED16DDF801.jpg",
	"862F6C8B-E756-4898-911E-58A14B60F457", "https://www.dropbox.com/s/gc561b48f4gzpr4/862F6C8B-E756-4898-911E-58A14B60F457.jpg",
	"7E027533-5344-4503-811D-41C2020D42DD", "https://www.dropbox.com/s/y643cf8r8vazxoy/7E027533-5344-4503-811D-41C2020D42DD.jpg",
	"A628D515-807D-491A-8327-51F5FA397619", "https://www.dropbox.com/s/dkf4jqmm41q8cwj/A628D515-807D-491A-8327-51F5FA397619.jpg",
	"791194AD-3255-47F6-94FA-6CDF31A67F91", "https://www.dropbox.com/s/j2j1okx1vzt7b7l/791194AD-3255-47F6-94FA-6CDF31A67F91.jpg",
	"D5318055-24E3-4284-A7D7-002C7066381D", "https://www.dropbox.com/s/i3qyfnldtgw7gzl/D5318055-24E3-4284-A7D7-002C7066381D.jpg",
	"DD77498E-FCE6-430A-A3D3-E79CD7150F7A", "https://www.dropbox.com/s/7bw1gf4orf1d0b2/DD77498E-FCE6-430A-A3D3-E79CD7150F7A.jpg",
	"1AC74A23-697C-46A2-B5AB-C48ECAFA1C02", "https://www.dropbox.com/s/wstuw3iyhnronn1/1AC74A23-697C-46A2-B5AB-C48ECAFA1C02.jpg",
	"6A6F684B-C524-44FF-A59F-019D91700406", "https://www.dropbox.com/s/017shmot9a5dhkb/6A6F684B-C524-44FF-A59F-019D91700406.jpg",
	"357D36CF-756C-41C3-ACEF-2DD81580B820", "https://www.dropbox.com/s/uiz062tjfv9i0e5/357D36CF-756C-41C3-ACEF-2DD81580B820.jpg",
	"05B3CF61-FEB0-41CD-93AA-740135B842CC", "https://www.dropbox.com/s/fb5ujcdzo5piyxe/05B3CF61-FEB0-41CD-93AA-740135B842CC.jpg",
	"D6C6A3C8-0594-41EB-9C19-C2B82F835AD8", "https://www.dropbox.com/s/qo46mepedfybc1w/D6C6A3C8-0594-41EB-9C19-C2B82F835AD8.jpg",
	"A7879F19-3ECF-4C0F-93C5-9BE24E228977", "https://www.dropbox.com/s/pu6sgqgugm104yk/A7879F19-3ECF-4C0F-93C5-9BE24E228977.jpg",
	"0CB9E522-BB28-418B-835D-96B7E2A900E4", "https://www.dropbox.com/s/hebpmxgehlyanot/0CB9E522-BB28-418B-835D-96B7E2A900E4.jpg",
	"FFAC4C88-F266-4358-B617-012BBA6CAE71", "https://www.dropbox.com/s/bxt6638ztnl3sgg/FFAC4C88-F266-4358-B617-012BBA6CAE71.jpg",
	"EB46CEFC-7CAD-4419-8237-3238C715659B", "https://www.dropbox.com/s/fhqqwuzglgyr19v/EB46CEFC-7CAD-4419-8237-3238C715659B.jpg",
	"430AEA3B-AA17-4096-9BB5-FDF0B1E210DB", "https://www.dropbox.com/s/lc6vy9myomxb7dv/430AEA3B-AA17-4096-9BB5-FDF0B1E210DB.jpg",
	"C7595712-66F5-4559-886A-EE01BABDFA84", "https://www.dropbox.com/s/5fjej0hz7y9rfwi/C7595712-66F5-4559-886A-EE01BABDFA84.jpg",
	"CB5E48CE-8978-42E6-A901-5D058B76041C", "https://www.dropbox.com/s/de8vv5dn3sti87y/CB5E48CE-8978-42E6-A901-5D058B76041C.jpg",
	"74213DC1-58F8-4FC5-8B67-2CFCB2B823E5", "https://www.dropbox.com/s/y44vk5caagzwcv4/74213DC1-58F8-4FC5-8B67-2CFCB2B823E5.jpg",
	"CF5367DF-FCB3-4B8B-AD21-17B75D3B2FD8", "https://www.dropbox.com/s/8y2v2r6qpnb3r1i/CF5367DF-FCB3-4B8B-AD21-17B75D3B2FD8.jpg",
	"9FF48AE9-CDFF-493F-AE55-37D2F9C91A02", "https://www.dropbox.com/s/bgqfpyp2o9lxhjt/9FF48AE9-CDFF-493F-AE55-37D2F9C91A02.jpg",
	"5F2C6E34-9056-4A2A-9E69-7AF61C5CFB27", "https://www.dropbox.com/s/5nxgr726wqvhzjy/5F2C6E34-9056-4A2A-9E69-7AF61C5CFB27.jpg",
	"AAC6EE43-6416-43B1-BF16-8E2CA53F19E7", "https://www.dropbox.com/s/fslgv2lj2voercv/AAC6EE43-6416-43B1-BF16-8E2CA53F19E7.jpg",
	"43A8314F-A9CF-4B1A-8898-150FAE7EA843", "https://www.dropbox.com/s/9o1bb1e6iyh8n0d/43A8314F-A9CF-4B1A-8898-150FAE7EA843.jpg",
	"479A9570-9701-4AFA-90F6-CE1D4D7ACAC2", "https://www.dropbox.com/s/tbottef6r4ipzym/479A9570-9701-4AFA-90F6-CE1D4D7ACAC2.jpg",
	"658413E1-71D2-4BB8-9916-7C7B2C7E9DA7", "https://www.dropbox.com/s/w0qyfapfr4ou4in/658413E1-71D2-4BB8-9916-7C7B2C7E9DA7.jpg",
	"F4029444-D834-40D4-BC6B-75D18F4187F3", "https://www.dropbox.com/s/t30omejtrx4vfeq/F4029444-D834-40D4-BC6B-75D18F4187F3.jpg",
	"1F09F78F-8E25-4440-8A0C-BECA38091607", "https://www.dropbox.com/s/flspt3s50ohy75c/1F09F78F-8E25-4440-8A0C-BECA38091607.jpg",
	"34BB87E8-1835-4706-A4AC-43394F2A1235", "https://www.dropbox.com/s/76c7gc5jkc9kflh/34BB87E8-1835-4706-A4AC-43394F2A1235.jpg",
	"6099712A-1F18-4638-8957-7AE4CA01387D", "https://www.dropbox.com/s/l4xr3fp9ce2k6qx/6099712A-1F18-4638-8957-7AE4CA01387D.jpg",
	"07D498BC-6706-419B-95C9-BAAA7538B0C0", "https://www.dropbox.com/s/ftfx1h49j66ou5s/07D498BC-6706-419B-95C9-BAAA7538B0C0.jpg",
	"2FEBDA32-DCCF-4697-A790-B7EEF0BAEDEA", "https://www.dropbox.com/s/mn213d8wnu4r6os/2FEBDA32-DCCF-4697-A790-B7EEF0BAEDEA.jpg",
	"01FA99E2-237E-4AAE-B162-E4230BD30216", "https://www.dropbox.com/s/r7i8l36sb77zoj6/01FA99E2-237E-4AAE-B162-E4230BD30216.jpg",
	"7DD1363A-666A-4930-9D69-283F39CAD44E", "https://www.dropbox.com/s/8udkmxgwek3pxvj/7DD1363A-666A-4930-9D69-283F39CAD44E.jpg",
	"F1B19EE5-B776-4C53-95B6-D6F2BA4678EB", "https://www.dropbox.com/s/fovfj51exgfbh8l/F1B19EE5-B776-4C53-95B6-D6F2BA4678EB.jpg",
	"8FAC574A-3405-4FE5-9476-1C425CAA0531", "https://www.dropbox.com/s/mze8gonnmx5na3f/8FAC574A-3405-4FE5-9476-1C425CAA0531.jpg",
	"4B248B2D-645B-449C-84F5-977FB66F5D6F", "https://www.dropbox.com/s/5bpqtti1jdni3pu/4B248B2D-645B-449C-84F5-977FB66F5D6F.jpg",
	"CACB9C46-CCE5-485A-886F-CE3249B62FB0", "https://www.dropbox.com/s/s52tod91vucs6ue/CACB9C46-CCE5-485A-886F-CE3249B62FB0.jpg",
	"98339434-369D-4552-A6BB-4FC66C9C7EE8", "https://www.dropbox.com/s/w4qgmhjvmbxczt8/98339434-369D-4552-A6BB-4FC66C9C7EE8.jpg",
	"0C79676F-9CB0-4C01-A828-4F8F8A9F0A9B", "https://www.dropbox.com/s/ims460gpc12ie4y/0C79676F-9CB0-4C01-A828-4F8F8A9F0A9B.jpg",
	"7A6C41BC-F274-429F-ABB1-E5CB2EA7A167", "https://www.dropbox.com/s/2fjsesw06n9tj9i/7A6C41BC-F274-429F-ABB1-E5CB2EA7A167.jpg",
	"563F26C2-2929-49B3-BF3B-72E2B3C9F2D3", "https://www.dropbox.com/s/vgsg7t6eug5qkjd/563F26C2-2929-49B3-BF3B-72E2B3C9F2D3.jpg",
	"874A7268-BF80-48A4-9B45-2DCD05F47A88", "https://www.dropbox.com/s/qbow7bx27dyq41z/874A7268-BF80-48A4-9B45-2DCD05F47A88.jpg",
	"F6DB9290-BF05-43A5-B0C2-302BB4BAE920", "https://www.dropbox.com/s/9v0cug3h3j73dmn/F6DB9290-BF05-43A5-B0C2-302BB4BAE920.jpg",
	"1C701F16-211D-41AD-8C7D-3A9A0C6ED369", "https://www.dropbox.com/s/fij76n0wgpddeim/1C701F16-211D-41AD-8C7D-3A9A0C6ED369.jpg",
	"4977E626-1425-44DC-B8F3-527A0F7ABE26", "https://www.dropbox.com/s/cse41mh6ao0xpvg/4977E626-1425-44DC-B8F3-527A0F7ABE26.jpg",
	"46BDC68F-F6D1-448E-B898-738DF944ADD8", "https://www.dropbox.com/s/nvt95riaorlh6q4/46BDC68F-F6D1-448E-B898-738DF944ADD8.jpg",
	"7B78BD6B-C17D-4306-B0EE-073C8186B2D7", "https://www.dropbox.com/s/x8s8fcenf9spgfh/7B78BD6B-C17D-4306-B0EE-073C8186B2D7.jpg",
	"A6585CBF-E349-459A-AD77-61B1B4DB2652", "https://www.dropbox.com/s/nqx1wr1xl36e9s4/A6585CBF-E349-459A-AD77-61B1B4DB2652.jpg",
	"30FE19E1-3E27-4495-9DCD-D381F2CD185B", "https://www.dropbox.com/s/d2gxvvuos3w8nqc/30FE19E1-3E27-4495-9DCD-D381F2CD185B.jpg",
	"121FCA94-D1C3-4E73-A95A-92A8466E37A5", "https://www.dropbox.com/s/pberugndj0gd852/121FCA94-D1C3-4E73-A95A-92A8466E37A5.jpg",
	"EE8D45D6-B5C6-422F-B862-82669F95CE12", "https://www.dropbox.com/s/78kjw7tcbj32uf2/EE8D45D6-B5C6-422F-B862-82669F95CE12.jpg",
	"83626694-FEB4-46A7-944D-A81E9515E791", "https://www.dropbox.com/s/tk0i3tr2r43gb3t/83626694-FEB4-46A7-944D-A81E9515E791.jpg",
	"3F9E9412-4A52-41DD-9068-B665A2C1C56A", "https://www.dropbox.com/s/jd3cwfxbgmvp7v5/3F9E9412-4A52-41DD-9068-B665A2C1C56A.jpg",
	"037E77C6-9AD3-47A3-85A3-1254D721696B", "https://www.dropbox.com/s/rr4e0agxq3lkcdx/037E77C6-9AD3-47A3-85A3-1254D721696B.jpg",
	"013CFE7A-AF0F-4B3D-A9FD-A7D479793C86", "https://www.dropbox.com/s/0hzh95h7bubii23/013CFE7A-AF0F-4B3D-A9FD-A7D479793C86.jpg",
	"FDEF66DD-E8ED-4DE3-9C61-64A4BEBF9EEA", "https://www.dropbox.com/s/5ojejpjjuxteyfb/FDEF66DD-E8ED-4DE3-9C61-64A4BEBF9EEA.jpg",
	"08C7F411-3BA0-42CC-8A79-17F5783111B6", "https://www.dropbox.com/s/5ys9tsmzxlkttyh/08C7F411-3BA0-42CC-8A79-17F5783111B6.jpg",
	"46826012-2360-44CA-AE32-6DCD7015B926", "https://www.dropbox.com/s/789uc7aci0cpi4v/46826012-2360-44CA-AE32-6DCD7015B926.jpg",
	"7BEB5D5C-345E-40F2-9903-78F35799DCFA", "https://www.dropbox.com/s/fbmu2wo8ha6hhn6/7BEB5D5C-345E-40F2-9903-78F35799DCFA.jpg",
	"80E4DE78-B2BE-4720-9046-AC17E6027F5C", "https://www.dropbox.com/s/2cx8x76qexonkw8/80E4DE78-B2BE-4720-9046-AC17E6027F5C.jpg",
	"071623B4-1311-4974-B325-2F50B5754BD9", "https://www.dropbox.com/s/25tnjiqbeugjrij/071623B4-1311-4974-B325-2F50B5754BD9.jpg",
	"A7DC3736-B938-40EE-AD35-10DCC7219BF7", "https://www.dropbox.com/s/41ffggpk9a5hmq1/A7DC3736-B938-40EE-AD35-10DCC7219BF7.jpg",
	"BB2137E0-69BF-4AF4-A29F-C76D1351B302", "https://www.dropbox.com/s/sndp5uq5xtxfbc7/BB2137E0-69BF-4AF4-A29F-C76D1351B302.jpg",
	"B440D9EC-C26E-473F-880E-8833BD97737D", "https://www.dropbox.com/s/mgfaaq1vuhri5ie/B440D9EC-C26E-473F-880E-8833BD97737D.jpg",
	"16B40DBC-E0CA-4F24-B51C-0BDBFA86D074", "https://www.dropbox.com/s/uft80h4qm6z6a01/16B40DBC-E0CA-4F24-B51C-0BDBFA86D074.jpg",
	"71716416-DFB6-4ECD-B82F-7622EB748CE2", "https://www.dropbox.com/s/9pixzte3vdoghk6/71716416-DFB6-4ECD-B82F-7622EB748CE2.jpg",
	"2200BD74-CF05-401E-AF63-30FD55D4D1FC", "https://www.dropbox.com/s/92b6e1flaopa870/2200BD74-CF05-401E-AF63-30FD55D4D1FC.jpg",
	"7DDEAEA0-BED7-4522-B5B7-3214008F4B2B", "https://www.dropbox.com/s/mq9q45wmxbyxpc8/7DDEAEA0-BED7-4522-B5B7-3214008F4B2B.jpg",
	"71759D34-4879-4889-9234-F7339B69E078", "https://www.dropbox.com/s/cssmv2a02dss9r6/71759D34-4879-4889-9234-F7339B69E078.jpg",
	"1EDA1F4F-D31F-44CB-847E-204844E1FC17", "https://www.dropbox.com/s/4cwsykr8abhcxff/1EDA1F4F-D31F-44CB-847E-204844E1FC17.jpg",
	"B5AF0E36-7595-4276-A29C-4E54C58CB58E", "https://www.dropbox.com/s/hhl71cxd1q70fp0/B5AF0E36-7595-4276-A29C-4E54C58CB58E.jpg",
	"49DE6B2F-D125-44B2-BE78-B6903031CE34", "https://www.dropbox.com/s/6god7iwbn5gmlae/49DE6B2F-D125-44B2-BE78-B6903031CE34.jpg",
	"080D89C8-6E45-48E0-A399-E2317C5C5730", "https://www.dropbox.com/s/i49ko351km7k0l4/080D89C8-6E45-48E0-A399-E2317C5C5730.jpg",
	"3DFDD68E-A526-4C3A-8FA8-7CD953207000", "https://www.dropbox.com/s/cf85off9vwc8vty/3DFDD68E-A526-4C3A-8FA8-7CD953207000.jpg",
	"855D4B5C-7784-4B1D-9797-05789DED2FB7", "https://www.dropbox.com/s/6nyzy8ktgqjp5nw/855D4B5C-7784-4B1D-9797-05789DED2FB7.jpg",
	"1B80F9F8-41FC-4752-859F-042B8A0AAD75", "https://www.dropbox.com/s/qgwjzycz5q0t5vn/1B80F9F8-41FC-4752-859F-042B8A0AAD75.jpg",
	"D294770E-D257-4282-9550-35257000B485", "https://www.dropbox.com/s/077cq2xxhwv6s3z/D294770E-D257-4282-9550-35257000B485.jpg",
	"25B4C492-22DA-4049-9C34-CEBAE05C83A1", "https://www.dropbox.com/s/2jlcazqcucwwzve/25B4C492-22DA-4049-9C34-CEBAE05C83A1.jpg",
	"112D4C6D-5680-4402-BC4D-DBEDD91CA7D6", "https://www.dropbox.com/s/1ynwtea98iqg7ud/112D4C6D-5680-4402-BC4D-DBEDD91CA7D6.jpg",
	"91F3A602-D69C-4116-9A50-94CF9B42474E", "https://www.dropbox.com/s/rz7xox61i3er4z5/91F3A602-D69C-4116-9A50-94CF9B42474E.jpg",
	"71F8FB83-7696-42B9-A005-67A568A93B0D", "https://www.dropbox.com/s/ptlypnz81urylws/71F8FB83-7696-42B9-A005-67A568A93B0D.jpg",
	"D02840B4-FF3A-466C-AAEF-04B52142356A", "https://www.dropbox.com/s/z75mlx62mownkte/D02840B4-FF3A-466C-AAEF-04B52142356A.jpg",
	"E0501E70-60BE-46C5-B8FF-61CEF277AE30", "https://www.dropbox.com/s/jczcyvshwv8h9oj/E0501E70-60BE-46C5-B8FF-61CEF277AE30.jpg",
	};
	
	var map = new Dictionary<string,string>();
	
	urls.Select((s,i) => {
							if( 0 == i % 2 )
							{
								map.Add(s,urls[i+1]);
							}
							return true;
						}
				).Count();
	
	var thumbMap = new Dictionary<string,string>();
	thumbUrls.Select((s,i) => {
							if( 0 == i % 2 )
							{
								thumbMap.Add(s,thumbUrls[i+1]);
							}
							return true;
						}
				).Count();
	
	var t0 = new DateTime(1970,1,1);

	var l = Joys
		.Select(joy=> new { joy.Note,
							Time = (int)(joy.Time),
							Filename = Pictures.Where(_=>_.ActivityID == joy.ID).Single().FileName } )
		.ToList()
		.Select(_=>new { _.Note,
						Date = (t0 + new TimeSpan(0,0,_.Time)),
						_.Filename,
						Url = map[_.Filename],
						Thumb = thumbMap.ContainsKey(_.Filename) ? thumbMap[_.Filename] : null
		})
		.GroupBy(_=>_.Date.ToString("MMMM d"))
		.OrderByDescending(_=>_.First().Date)
		//.Dump(0)
		;
	
	
	var d = new XDocument();
	d.Add(	new XElement("root",
			l.SelectMany(_=>_).Select(_=> new XElement("photo", new XAttribute[] { new XAttribute("title", _.Note ?? String.Empty),
																			new XAttribute("date", _.Date),																		
																			new XAttribute("id", _.Filename),																		
																			new XAttribute("url", _.Url),
																			new XAttribute("thumbUrl", _.Thumb) } ) )
																			)
				);
	d.Save(filename);
	return;
}


void buildHtml(string dataFilename, string templateFilename, string outputFilename)
{
	var ldb = XDocument.Load(dataFilename);
	
	var l = ldb.Descendants("photo").Select(_=> new { 	title = _.Attribute("title").Value,
													date = DateTime.Parse(_.Attribute("date").Value),
													id = _.Attribute("id").Value,
													url = _.Attribute("url").Value,
													thumbUrl = _.Attribute("thumbUrl").Value,
		})
		.GroupBy(_=>_.date.ToString("MMMM d"))
		.OrderByDescending(_=>_.First().date)
		//.Dump()
		;
	
	var formatTitle = "<li class='title-box'><h2>{0}</a></h2></li>\n";
	var format = "<li><a target='_blank' href='{0}?dl=0'><img src='{1}?dl=1' alt='{2}'><h3>{2}</h3><br/>{3}</a></li>\n";
	
	

	l.Select(_=>
		_.Aggregate( new StringBuilder(String.Format(formatTitle, _.Key)), 
						(a,n) => ( a.Append( String.Format(format, n.url, n.thumbUrl, n.title, n.date)))
						).ToString())
		//.Dump("Complex", 0)
		;
	
	
	formatTitle = "<div class='item'><h2>{0}</h2></div>";
	format = @"
	<div class='item'>
		<div class='image'>
			<img src='{1}?dl=1'/>
	        <span class='title'>{2}</span>
		</div> 
	</div>";
	
	
	l.Select(_=>
		_.Aggregate( new StringBuilder(String.Format(formatTitle, _.Key)), 
						(a,n) => ( a.Append( String.Format(format, n.url, n.thumbUrl, n.title, n.date)))
						).ToString())
		//.Dump("Simple", 0)
		;
	
	
	formatTitle = "<div class='item w2'><h2>{0}</h2></div>";
	format = @"<div class='item'><a target='_blank' href={0}><img src='{1}?dl=1' alt='{2}'><span class='imgTitle'>{2}</span></a></div>";
	
	
	l.Select(_=>
		_.Aggregate( new StringBuilder(String.Format(formatTitle, _.Key)), 
						(a,n) => ( a.Append( String.Format(format, n.url, n.thumbUrl, n.title, n.date)))
						).ToString())
		//.Dump("Mansion", 0)
		;
	
	formatTitle = "<li><h1>{0}</h1></li>";
	format = @"
	<li>
		<div class='image'>
			<a href='{0}' target='_blank'>
				<img src='{1}?dl=1' />
				<span class='imgTitle'>{2}</span>
			</a>
		</div>
	</li>";
	
	var items = l.Select(_=>
		_.Aggregate( new StringBuilder(String.Format(formatTitle, _.Key)), 
						(a,n) => ( a.Append( String.Format(format, n.url, n.thumbUrl, n.title, n.date)))
						).ToString())
		//.Dump("Mansion1")
		;
		
	var add_intes_here = "<!--##ADD ITEMS HERE##-->";
	File.WriteAllText(outputFilename,File.ReadAllText(templateFilename).Replace(add_intes_here, String.Join("\n", items)));
}


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

	root
		.Descendants("dict").ToList()
		.Where(_=>
	{
		var b4=_.ElementsBeforeSelf();
		
		return b4.Any() && b4.Last().Value.StartsWith("$");
	})
	.Select(_=>{ 
	
		_.Remove();
				
		return true; 
	} )
	.Count();
	
	root.Descendants("key").ToList()
		.Where(_=>_.Value.StartsWith("$"))
		.Select(_=>{ _.Remove(); return true;} )
		.Count();
			
	return root;
}


string valueOf(string key, XElement ele)
{
	var k = ele.Descendants("key").Where(_=>_.Value == key ).FirstOrDefault();
	if(null == k)
	{
		return null;
	}
	
	var v = k.ElementsAfterSelf().First().Descendants("string").FirstOrDefault();
	if( null == v )
	{
		v = k.ElementsAfterSelf().First().Descendants("data").FirstOrDefault();
	}
	if( null == v )
	{
		v = k.Parent.Descendants("real").FirstOrDefault();
	}
	
	return null == v ? null : v.Value;
}

IEnumerable<FYPhoto> processTransactionLog(string logFilename)
{
	var data = extractData(logFilename);
		
	var html = data.Where(d=>d.ContainsKey("PictureNote")
				&& null != valueOf("FileName", d["PictureNote"])
				)
		.Select(d=> 
	{
		var thumbnail = Path.GetTempFileName()+".png";
		
		var PictureNote = valueOf("Thumbnail", d["PictureNote"]);
		if( null != PictureNote)
		{
			File.WriteAllBytes(	thumbnail,System.Convert.FromBase64String(PictureNote) );
		}

		var t0 = new DateTime(2001,1,1);
		
		return new FYPhoto() {	title = d["Note"].Value ?? String.Empty,
								id = valueOf("FileName", d["PictureNote"]) ?? String.Empty,
								date = t0+ new TimeSpan(0,0,(int)(Single.Parse(valueOf("NS.time", d["Timestamp"])))),
								};
	})
	//.Dump()
	;
	
	return html;
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


IEnumerable<FYPhoto> loadLocalDb(string dataFilename)
{
	var ldb = XDocument.Load(dataFilename);
	
	return ldb.Descendants("photo").Select(_=> new FYPhoto() { 	title = _.Attribute("title").Value,
													date = DateTime.Parse(_.Attribute("date").Value),
													id = _.Attribute("id").Value,
													url = _.Attribute("url").Value,
													thumbUrl = _.Attribute("thumbUrl").Value,
		}) ;
}



void updateLocalDb(IEnumerable<FYPhoto> freshPhotos, string dataFilename)
{
	var curPhotos = loadLocalDb(dataFilename);

	// see if there is anything new
	var newPhotos = freshPhotos.Where(_=>!curPhotos.Any(c=>c.id == _.id)).Dump("new photos");
	if( !newPhotos.Any() )
	{
		return;
	}
	
	var xd = XDocument.Load(dataFilename);
	newPhotos.Select(_=>{ xd.Element("root").Add( _.toXElement() ); return true; } ).Count();
	
	xd.Save(dataFilename);
	
	// generate the URLs now
}


void dumpData(IEnumerable<IDictionary<string, XElement>> data)
{
	var root = new XElement("root");
	
	data.Select((_,i)=>
	{
		var di = new XElement("d"+i.ToString());
		
		_	.Keys
			.Select(k=>
			{
				di.Add(new XElement(k, _[k]));
				return true;
			})
			.Count();
		
		root.Add(di);
		return true;
	})
	.Count();
	
	var doc = new XDocument();
	doc.Add(root);
	doc.Save(@"c:\temp\big.xml");
}


List<Dictionary<string,XElement>> extractData(string logFilename)
{
	var root = XDocument.Load(logFilename)
						.Elements().First().Element("dict");
	
	var bigArray = root.Element("array");
	
	// enumerate the elements, save them in an array
	//var numberedEls = bigArray.Elements().Select((_,i)=>{_.Add( new XAttribute("_id_", i)); return _; });
	//new XElement("root",numberedEls).Save(logFilename + ".xml");

	Dictionary<int, XElement> bricks = new Dictionary<int, XElement>();

	bigArray.Elements().Select((_,i)=>{bricks.Add(i,_); return true;}).Count();
	//bricks.Count().Dump();
	//bricks[36].Dump("b36");
	
	bricks[0].Dump("b0");
	bricks[1].Dump("b1");
	bricks[2].Dump("b2");
	bricks[3].Dump("b3");
	
	// replace the pointers with the data
	bigArray
		.Descendants("dict")
		.Select(_=>translate(_, bricks)).Count();
			
	// compress
	var lst = new List<XElement>();
	
	bigArray
		.Descendants("dict")
		.Where(_=>_.Descendants().Count() == 2 )
		.ToList()
		.Take(3)
		.Select((kv,i)=>
		{
			kv.Parent.Dump("kv_" + i.ToString());
			
			lst.Add(kv.Parent.Parent);
			
			kv.Parent.Add(new XAttribute("val_" + i.ToString(), kv.Descendants().Last().Value));
			//kv.Remove();
			return true;
		}
		)
		.Count();
		
	lst.Take(30).Dump("LST");

//	return null;

	
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
	
	//data.Count().Dump();
	//data[0].Dump();
	
	dumpData(data);

	return data;
}