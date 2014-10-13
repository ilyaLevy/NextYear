#!/usr/bin/env python

import cmd
import locale
import os
import pprint
import shlex
import sys

import xml.etree.ElementTree as ET

PY3 = sys.version_info[0] == 3

if PY3:
    from io import StringIO
else:
    from StringIO import StringIO

from dropbox import client, rest, session

# XXX Fill in your consumer key and secret below
# You can find these at http://www.dropbox.com/developers/apps
APP_KEY = 'ron0m5ovj8vq9f7'  # GuidoFullAccessApp
APP_SECRET = 'ussirz6zlo6s6x6'

def command(login_required=True):
    """a decorator for handling authentication and exceptions"""
    def decorate(f):
        def wrapper(self, args):
            if login_required and self.api_client is None:
                self.stdout.write("Please 'login' to execute this command\n")
                return

            try:
                return f(self, *args)
            except TypeError, e:
                self.stdout.write(str(e) + '\n')
            except rest.ErrorResponse, e:
                msg = e.user_error_msg or str(e)
                self.stdout.write('Error: %s\n' % msg)

        wrapper.__doc__ = f.__doc__
        return wrapper
    return decorate

class DropboxTerm(cmd.Cmd):
    TOKEN_FILE = "token_store.txt"

    def __init__(self, app_key, app_secret):
        cmd.Cmd.__init__(self)
        self.app_key = app_key
        self.app_secret = app_secret
        self.current_path = ''
        self.prompt = "Dropbox> "

        self.api_client = None
        try:
            serialized_token = open(self.TOKEN_FILE).read()
            if serialized_token.startswith('oauth1:'):
                access_key, access_secret = serialized_token[len('oauth1:'):].split(':', 1)
                sess = session.DropboxSession(self.app_key, self.app_secret)
                sess.set_token(access_key, access_secret)
                self.api_client = client.DropboxClient(sess)
                print "[loaded OAuth 1 access token]"
            elif serialized_token.startswith('oauth2:'):
                access_token = serialized_token[len('oauth2:'):]
                self.api_client = client.DropboxClient(access_token)
                print "[loaded OAuth 2 access token]"
            else:
                print "Malformed access token in %r." % (self.TOKEN_FILE,)
        except IOError:
            pass # don't worry if it's not there

    @command()
    def do_ls(self):
        """list files in current remote directory"""
        resp = self.api_client.metadata(self.current_path)

        if 'contents' in resp:
            for f in resp['contents']:
                name = os.path.basename(f['path'])
                encoding = locale.getdefaultlocale()[1] or 'ascii'
                self.stdout.write(('%s\n' % name).encode(encoding))

    @command()
    def do_cd(self, path):
        """change current working directory"""
        if path == "..":
            self.current_path = "/".join(self.current_path.split("/")[0:-1])
        else:
            self.current_path += "/" + path

    @command(login_required=False)
    def do_login(self):
        """log in to a Dropbox account"""
        flow = client.DropboxOAuth2FlowNoRedirect(self.app_key, self.app_secret)
        authorize_url = flow.start()
        sys.stdout.write("1. Go to: " + authorize_url + "\n")
        sys.stdout.write("2. Click \"Allow\" (you might have to log in first).\n")
        sys.stdout.write("3. Copy the authorization code.\n")
        code = raw_input("Enter the authorization code here: ").strip()

        try:
            access_token, user_id = flow.finish(code)
        except rest.ErrorResponse, e:
            self.stdout.write('Error: %s\n' % str(e))
            return

        with open(self.TOKEN_FILE, 'w') as f:
            f.write('oauth2:' + access_token)
        self.api_client = client.DropboxClient(access_token)

    @command(login_required=False)
    def do_login_oauth1(self):
        """log in to a Dropbox account"""
        sess = session.DropboxSession(self.app_key, self.app_secret)
        request_token = sess.obtain_request_token()
        authorize_url = sess.build_authorize_url(request_token)
        sys.stdout.write("1. Go to: " + authorize_url + "\n")
        sys.stdout.write("2. Click \"Allow\" (you might have to log in first).\n")
        sys.stdout.write("3. Press ENTER.\n")
        raw_input()

        try:
            access_token = sess.obtain_access_token()
        except rest.ErrorResponse, e:
            self.stdout.write('Error: %s\n' % str(e))
            return

        with open(self.TOKEN_FILE, 'w') as f:
            f.write('oauth1:' + access_token.key + ':' + access_token.secret)
        self.api_client = client.DropboxClient(sess)

    @command()
    def do_logout(self):
        """log out of the current Dropbox account"""
        self.api_client = None
        os.unlink(self.TOKEN_FILE)
        self.current_path = ''

    @command()
    def do_cat(self, path):
        """display the contents of a file"""
        f, metadata = self.api_client.get_file_and_metadata(self.current_path + "/" + path)
        self.stdout.write(f.read())
        self.stdout.write("\n")

    @command()
    def do_mkdir(self, path):
        """create a new directory"""
        self.api_client.file_create_folder(self.current_path + "/" + path)

    @command()
    def do_rm(self, path):
        """delete a file or directory"""
        self.api_client.file_delete(self.current_path + "/" + path)

    @command()
    def do_mv(self, from_path, to_path):
        """move/rename a file or directory"""
        self.api_client.file_move(self.current_path + "/" + from_path,
                                  self.current_path + "/" + to_path)

    @command()
    def do_share(self, path):
        """Create a link to share the file at the given path."""
        print self.api_client.share(path)['url']

    @command()
    def do_account_info(self):
        """display account information"""
        f = self.api_client.account_info()
        pprint.PrettyPrinter(indent=2).pprint(f)

    @command(login_required=False)
    def do_exit(self):
        """exit"""
        return True

    @command()
    def do_get(self, from_path, to_path):
        """
        Copy file from Dropbox to local file and print out the metadata.

        Examples:
        Dropbox> get file.txt ~/dropbox-file.txt
        """
        to_file = open(os.path.expanduser(to_path), "wb")

        f, metadata = self.api_client.get_file_and_metadata(self.current_path + "/" + from_path)
        print 'Metadata:', metadata
        to_file.write(f.read())

    @command()
    def do_thumbnail(self, from_path, to_path, size='large', format='JPEG'):
        """
        Copy an image file's thumbnail to a local file and print out the
        file's metadata.

        Examples:
        Dropbox> thumbnail file.txt ~/dropbox-file.txt medium PNG
        """
        to_file = open(os.path.expanduser(to_path), "wb")

        f, metadata = self.api_client.thumbnail_and_metadata(
                self.current_path + "/" + from_path, size, format)
        print 'Metadata:', metadata
        to_file.write(f.read())

    @command()
    def do_put(self, from_path, to_path):
        """
        Copy local file to Dropbox

        Examples:
        Dropbox> put ~/test.txt dropbox-copy-test.txt
        """
        from_file = open(os.path.expanduser(from_path), "rb")

        encoding = locale.getdefaultlocale()[1] or 'ascii'
        full_path = (self.current_path + "/" + to_path).decode(encoding)
        self.api_client.put_file(full_path, from_file)

    @command()
    def do_put_chunk(self, from_path, to_path, length, offset=0, upload_id=None):
        """Put one chunk of a file to Dropbox.

        Examples:
        Dropbox> put_chunk ~/test-1kb.txt dropbox-copy-test.txt 1000
        Dropbox> put_chunk ~/test-1kb.txt dropbox-copy-test.txt 24 1000 <upload_id>
        Dropbox> commit_chunks auto/dropbox-copy-test.txt <upload-id>
        """
        length = int(length)
        offset = int(offset)
        with open(from_path) as to_upload:
            to_upload.seek(offset)
            new_offset, upload_id = self.api_client.upload_chunk(StringIO(to_upload.read(length)),
                                                                 offset, upload_id)
            print 'For upload id: %r, uploaded bytes [%d-%d]' % (upload_id, offset, new_offset)

    @command()
    def do_commit_chunks(self, to_path, upload_id):
        """Commit the previously uploaded chunks for the given file.

        Examples:
        Dropbox> commit_chunks auto/dropbox-copy-test.txt <upload-id>
        """
        metadata = self.api_client.commit_chunked_upload(to_path, upload_id)
        print 'Metadata:', metadata

    @command()
    def do_search(self, string):
        """Search Dropbox for filenames containing the given string."""
        results = self.api_client.search(self.current_path, string)
        for r in results:
            self.stdout.write("%s\n" % r['path'])

    @command(login_required=False)
    def do_help(self):
        # Find every "do_" attribute with a non-empty docstring and print
        # out the docstring.
        all_names = dir(self)
        cmd_names = []
        for name in all_names:
            if name[:3] == 'do_':
                cmd_names.append(name[3:])
        cmd_names.sort()
        for cmd_name in cmd_names:
            f = getattr(self, 'do_' + cmd_name)
            if f.__doc__:
                self.stdout.write('%s: %s\n' % (cmd_name, f.__doc__))

    # the following are for command line magic and aren't Dropbox-related
    def emptyline(self):
        pass

    def do_EOF(self, line):
        self.stdout.write('\n')
        return True

    def parseline(self, line):
        parts = shlex.split(line)
        if len(parts) == 0:
            return None, None, line
        else:
            return parts[0], parts[1:], line


def main_org():
    if APP_KEY == '' or APP_SECRET == '':
        exit("You need to set your APP_KEY and APP_SECRET!")
    term = DropboxTerm(APP_KEY, APP_SECRET)
    term.cmdloop()


class FirstYearPlus(cmd.Cmd):

    def __init__(self):

        self.api_client = None
        try:
            access_token = 'qsEe-HKsKCEAAAAAAAAGVx_DNOVFQCrjtcsAEFNeTeenQ1NwKsis-51HZDpYjwG2';
            self.api_client = client.DropboxClient(access_token)
        except IOError:
            pass # don't worry if it's not there

    def do_ls(self, path):
        """list files in current remote directory"""
        resp = self.api_client.metadata(path)

        images = []

        if 'contents' in resp:
            for f in resp['contents']:
                name = os.path.basename(f['path'])
                encoding = locale.getdefaultlocale()[1] or 'ascii'
                #self.stdout.write(('%s\n' % name).encode(encoding))
                images.append(name)

        return images

    def do_share(self, path):
        """Create a link to share the file at the given path."""
        return self.api_client.share(path)['url']


    def do_thumbnail(self, from_path, size='large', format='JPEG'):
        """
        Copy an image file's thumbnail to a local file and print out the
        file's metadata.

        Examples:
        Dropbox> thumbnail file.txt ~/dropbox-file.txt medium PNG
        """
        f, metadata = self.api_client.thumbnail_and_metadata(from_path, size, format)

        print 'Metadata:', metadata

    def getSharedUrl(self, filepathname):

            urlShort = self.do_share(filepathname)

            import urllib2
            page = urllib2.urlopen(urlShort)
            url = page.geturl()

            from urlparse import urlparse
            o = urlparse(url)
            imgUrl = o.scheme + '://' + o.netloc + o.path

            return imgUrl

def main():
    # access token:
    # qsEe-HKsKCEAAAAAAAAGVx_DNOVFQCrjtcsAEFNeTeenQ1NwKsis-51HZDpYjwG2

    fyp = FirstYearPlus()

    dbxRoot = '/Apps/FirstYear/sinkv2/Ilya, m3__2A96AA59-7C83-4CD6-946E-1CCE4DED7BBA/Media/'

    filenames = [
'496F39B0-D358-4758-9493-DC78F2FB128F',
'31D1EE80-39A5-476F-984E-EC72C17C138D',
'9DB4095D-26CA-4B18-9AA4-0C6D9F7CD104',
'909CAB10-C722-4A03-9769-FDAC3E78C3DD',
'59743CD9-FAFB-4894-A0E4-738140FB3C72',
'0A45816C-F1B9-4B92-8FC7-9EFD9928359C',
'74179837-B4FF-4BA1-A1A6-4679DB4CCDC5',
'6831B132-1970-4220-BCEA-1361B2C0BAC1',
'F3A04EB5-C575-4354-92FC-CA109BD73D64',
'B98728B2-6C1A-4FD5-AA07-C6DE14DD4126',
'7EFBFEE3-98BC-4764-B07F-8FB61A32A517',

        ]

    filenames_dbseed = [
'B563F8B4-4961-44D1-B600-3170B0FA30FE', 
'68CEE8C5-DEB9-4B04-88B6-E328DC2B3972', 
'121A3244-10BA-4100-A7BC-3F2CD17D2C68', 
'E66036DB-9217-478B-8645-4CBB55115C94', 
'15442617-BC9D-4638-9B91-F991E21CC46A', 
'C3B029A4-8141-4C46-939F-CEA74CBD21B6', 
'C939B7C0-F996-4D9B-92A1-8B0EB9F5CC15', 
'8966670E-FE1D-40AA-ABCE-7DAC1697B581', 
'ABF17730-0D8F-4C41-837D-9904F6332DDB', 
'BA454E3F-EE34-4576-B335-F65A568A7B7B', 
'B7E6488C-B792-4769-AD3C-68F3CF877C94', 
'A825597D-FC24-45C6-AB0F-7239CD0DC2A1', 
'071A2D4E-737D-4B1D-A7D9-6D23BCD62A4F', 
'EC08AA0D-16D7-4B8B-B5B8-DE3E27A8DB5D', 
'2932000E-C17F-45A4-A44F-16A070262665', 
'6DEEEC32-D7C2-4481-AEB5-91CA3D987CAC', 
'1176A28A-D63B-459D-B4E8-BB82225575D1', 
'1E4589C6-BE6B-43EB-8874-F9B90C4EAE1B', 
'0B7FBEAD-F731-496B-B530-91F5AEA27549', 
'C28A787C-AEAB-48D0-9CC0-E4EDF8D1114A', 
'98B50BA7-0975-47D2-9E97-4AED16DDF801', 
'862F6C8B-E756-4898-911E-58A14B60F457', 
'7E027533-5344-4503-811D-41C2020D42DD', 
'A628D515-807D-491A-8327-51F5FA397619', 
'791194AD-3255-47F6-94FA-6CDF31A67F91', 
'D5318055-24E3-4284-A7D7-002C7066381D', 
'DD77498E-FCE6-430A-A3D3-E79CD7150F7A', 
'1AC74A23-697C-46A2-B5AB-C48ECAFA1C02', 
'6A6F684B-C524-44FF-A59F-019D91700406', 
'357D36CF-756C-41C3-ACEF-2DD81580B820', 
'05B3CF61-FEB0-41CD-93AA-740135B842CC', 
'D6C6A3C8-0594-41EB-9C19-C2B82F835AD8', 
'A7879F19-3ECF-4C0F-93C5-9BE24E228977', 
'0CB9E522-BB28-418B-835D-96B7E2A900E4', 
'FFAC4C88-F266-4358-B617-012BBA6CAE71', 
'EB46CEFC-7CAD-4419-8237-3238C715659B', 
'430AEA3B-AA17-4096-9BB5-FDF0B1E210DB', 
'C7595712-66F5-4559-886A-EE01BABDFA84', 
'CB5E48CE-8978-42E6-A901-5D058B76041C', 
'74213DC1-58F8-4FC5-8B67-2CFCB2B823E5', 
'CF5367DF-FCB3-4B8B-AD21-17B75D3B2FD8', 
'9FF48AE9-CDFF-493F-AE55-37D2F9C91A02', 
'5F2C6E34-9056-4A2A-9E69-7AF61C5CFB27', 
'AAC6EE43-6416-43B1-BF16-8E2CA53F19E7', 
'43A8314F-A9CF-4B1A-8898-150FAE7EA843', 
'479A9570-9701-4AFA-90F6-CE1D4D7ACAC2', 
'658413E1-71D2-4BB8-9916-7C7B2C7E9DA7', 
'F4029444-D834-40D4-BC6B-75D18F4187F3', 
'1F09F78F-8E25-4440-8A0C-BECA38091607', 
'34BB87E8-1835-4706-A4AC-43394F2A1235', 
'6099712A-1F18-4638-8957-7AE4CA01387D', 
'07D498BC-6706-419B-95C9-BAAA7538B0C0', 
'2FEBDA32-DCCF-4697-A790-B7EEF0BAEDEA', 
'01FA99E2-237E-4AAE-B162-E4230BD30216', 
'7DD1363A-666A-4930-9D69-283F39CAD44E', 
'F1B19EE5-B776-4C53-95B6-D6F2BA4678EB', 
'8FAC574A-3405-4FE5-9476-1C425CAA0531', 
'4B248B2D-645B-449C-84F5-977FB66F5D6F', 
'CACB9C46-CCE5-485A-886F-CE3249B62FB0', 
'98339434-369D-4552-A6BB-4FC66C9C7EE8', 
'0C79676F-9CB0-4C01-A828-4F8F8A9F0A9B', 
'7A6C41BC-F274-429F-ABB1-E5CB2EA7A167', 
'563F26C2-2929-49B3-BF3B-72E2B3C9F2D3', 
'874A7268-BF80-48A4-9B45-2DCD05F47A88', 
'F6DB9290-BF05-43A5-B0C2-302BB4BAE920', 
'1C701F16-211D-41AD-8C7D-3A9A0C6ED369', 
'4977E626-1425-44DC-B8F3-527A0F7ABE26', 
'46BDC68F-F6D1-448E-B898-738DF944ADD8', 
'7B78BD6B-C17D-4306-B0EE-073C8186B2D7', 
'A6585CBF-E349-459A-AD77-61B1B4DB2652', 
'30FE19E1-3E27-4495-9DCD-D381F2CD185B', 
'121FCA94-D1C3-4E73-A95A-92A8466E37A5', 
'EE8D45D6-B5C6-422F-B862-82669F95CE12', 
'83626694-FEB4-46A7-944D-A81E9515E791', 
'3F9E9412-4A52-41DD-9068-B665A2C1C56A', 
'037E77C6-9AD3-47A3-85A3-1254D721696B', 
'013CFE7A-AF0F-4B3D-A9FD-A7D479793C86', 
'FDEF66DD-E8ED-4DE3-9C61-64A4BEBF9EEA', 
'08C7F411-3BA0-42CC-8A79-17F5783111B6', 
'46826012-2360-44CA-AE32-6DCD7015B926', 
'7BEB5D5C-345E-40F2-9903-78F35799DCFA', 
'80E4DE78-B2BE-4720-9046-AC17E6027F5C', 
'071623B4-1311-4974-B325-2F50B5754BD9', 
'A7DC3736-B938-40EE-AD35-10DCC7219BF7', 
'BB2137E0-69BF-4AF4-A29F-C76D1351B302', 
'B440D9EC-C26E-473F-880E-8833BD97737D', 
'16B40DBC-E0CA-4F24-B51C-0BDBFA86D074', 
'71716416-DFB6-4ECD-B82F-7622EB748CE2', 
'2200BD74-CF05-401E-AF63-30FD55D4D1FC', 
'7DDEAEA0-BED7-4522-B5B7-3214008F4B2B', 
'71759D34-4879-4889-9234-F7339B69E078', 
'1EDA1F4F-D31F-44CB-847E-204844E1FC17', 
'B5AF0E36-7595-4276-A29C-4E54C58CB58E', 
'49DE6B2F-D125-44B2-BE78-B6903031CE34', 
'080D89C8-6E45-48E0-A399-E2317C5C5730', 
'3DFDD68E-A526-4C3A-8FA8-7CD953207000', 
'855D4B5C-7784-4B1D-9797-05789DED2FB7', 
'1B80F9F8-41FC-4752-859F-042B8A0AAD75', 
'D294770E-D257-4282-9550-35257000B485', 
'25B4C492-22DA-4049-9C34-CEBAE05C83A1', 
'112D4C6D-5680-4402-BC4D-DBEDD91CA7D6', 
'91F3A602-D69C-4116-9A50-94CF9B42474E', 
'71F8FB83-7696-42B9-A005-67A568A93B0D', 
'D02840B4-FF3A-466C-AAEF-04B52142356A', 
'E0501E70-60BE-46C5-B8FF-61CEF277AE30', 
        ]

    #for filename in filenames:

    #    print   "\"" + filename + "\", ", "\"" + fyp.getSharedUrl(dbxRoot + filename + ".jpg") + "\",", "\"" + fyp.getSharedUrl(dbxRoot + filename + " (Mobile).jpg") + "\","

    #    import time
    #    time.sleep(1)

    import xml.etree.ElementTree as ET
    tree = ET.parse('C:\\Dev\\Quick\\FirstYear\\localDb.xml')
    root = tree.getroot()

    for photo in root.findall('photo'):
        if( '' == photo.attrib['url'] ):
            print photo.attrib['id'], ' - url '
            photo.set('url',fyp.getSharedUrl(dbxRoot + photo.attrib['id'] + ".jpg"))
            tree.write('C:\\Dev\\Quick\\FirstYear\\localDb.xml')


        if( '' == photo.attrib['thumbUrl'] ):
            print photo.attrib['id'], ' - thumb '
            photo.set('thumbUrl',fyp.getSharedUrl(dbxRoot + photo.attrib['id'] + " (Mobile).jpg"))
            tree.write('C:\\Dev\\Quick\\FirstYear\\localDb.xml')

    #sorted(p.attrib[0] for p in root.findall('photo'))
    # sorted(s.upper() for s in names if len(s) == 5)





if __name__ == '__main__':
    main()


