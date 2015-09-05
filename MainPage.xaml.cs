using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Alezza.Decode.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;
using SQLitePCL;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Alezza.Decode
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IList<LaneInfo> LaneInfos { set; get; }
        private IList<CacheElement> CacheElements { set; get; }
        private readonly SecurityService _securityService = new SecurityService();
        private readonly StorageFolder _localFolder = ApplicationData.Current.LocalFolder;

        public MainPage()
        {
            this.InitializeComponent();
            LoadDatabase();
            Test();
        }

        private void LoadDatabase()
        {
            LoadLandInfo();
            LoadCacheElement();
            MapLandInfoAndCache();
        }

        private void MapLandInfoAndCache()
        {
            var tmpList = LaneInfos;
            for (var index = 0; index < tmpList.Count; index++)
            {
                var laneInfo = tmpList[index];
                var element = CacheElements.SingleOrDefault(s => s.Key.Contains(laneInfo.Url));
                if (element == null) continue;
                var info = LaneInfos[index];
                element.MapValue(ref info);
            }
        }

        private void LoadCacheElement()
        {
            CacheElements = new List<CacheElement>();
            var dbCache = new SQLiteConnection("blobs.db");
            var statement = dbCache.Prepare("select * from CacheElement where CacheElement.Key like '%index.json%'");
            while (statement.Step() ==  SQLiteResult.ROW)
            {
                var cache = CreateCache(statement);
                CacheElements.Add(cache);
            }
        }

        private static CacheElement CreateCache(ISQLiteStatement statement)
        {
            return new CacheElement()
            {
                Key = (string) statement[0],
                Value = (byte[]) statement[2],
            };
        }

        private void LoadLandInfo()
        {
            LaneInfos = new List<LaneInfo>();
            var dbLaneInfo = new SQLiteConnection("Database.db");
            var statement = dbLaneInfo.Prepare("select * from lane_info");
            while (statement.Step() == SQLiteResult.ROW)
            {
              
                    var landInfo = CreateLandInfo(statement);
                    LaneInfos.Add(landInfo);
            }
        }

        private LaneInfo CreateLandInfo(ISQLiteStatement statement)
        {
            return  new LaneInfo()
            {
                Isbn = (string)statement[0],
                Url = (string)statement[1]
            };
        }


        public async void Test()
        {
            var sourceBooksEncrypt = await _localFolder.GetFolderAsync("Encrypt");
            var sourceBookDecrypt = await _localFolder.GetFolderAsync("Decrypt");
            var encryptFolders = await sourceBooksEncrypt.GetFoldersAsync();//d977232c-6d5b-4d0d-b7e6-59c5143f8aec
            foreach (var encryptFolder in encryptFolders)
            {
                Debug.WriteLine("Begin:"+ encryptFolder.Name);
                var key = _securityService.GetXOEncryptKey(encryptFolder.Name);
                var decryptFolder =
                     await
                         sourceBookDecrypt.CreateFolderAsync(encryptFolder.Name, CreationCollisionOption.FailIfExists);

                try
                {
                    
                 
                    var folders = await GetFolders(encryptFolder);
                    foreach (var folder in folders)
                    {
                        var childDecryptFolder =
                            await decryptFolder.CreateFolderAsync(folder.Name, CreationCollisionOption.OpenIfExists);
                        var encryptFiles = await folder.GetFilesAsync();
                        foreach (var encryptFile in encryptFiles)
                        {
                            var decryptedData = await DecryptedData(encryptFile, key);
                            var decryptFile =
                                await
                                    childDecryptFolder.CreateFileAsync(encryptFile.Name,
                                        CreationCollisionOption.ReplaceExisting);
                            await FileIO.WriteBufferAsync(decryptFile, decryptedData);
                        }
                    }
                    var filesEncrypt = (await encryptFolder.GetFilesAsync()).ToList();
                    
                   await FilesDecrypt(filesEncrypt, key, decryptFolder);

                    var landInfo = LaneInfos.Single(s => s.Isbn.Equals(decryptFolder.Name));
                    await FixNameBook(landInfo, decryptFolder);

                    Debug.WriteLine("END: "+ encryptFolder.Name);
                    await encryptFolder.DeleteAsync();
                    Debug.WriteLine("Delete : "+ encryptFolder.Name);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ERROR: " + encryptFolder.Name);
                    await Log(decryptFolder, ex.Message);
                    await decryptFolder.RenameAsync("--ERROR-- " + encryptFolder.Name);
                }
            }
        }

        private static async Task FixNameBook(LaneInfo landInfo, StorageFolder decryptFolder)
        {
            
            var folderName = StripUnicodeCharactersFromString(landInfo.Meta.Title).Replace(new []{'\\','/',':','[',']',':', '|', '<', '>', '+', ';', '=', '.', '?' },"");
            await decryptFolder.RenameAsync(folderName);
            var files = await decryptFolder.GetFilesAsync();
          
            foreach (var section in landInfo.Sections)
            {
                var file = files.SingleOrDefault(s => (s.Name).Equals(section.ShortName));
                if (file==null)
                {
                    await Log(decryptFolder, "NotFount : " + section.Name + " Hash Name : " + section.ShortName);
                    continue;
                }
                await file.RenameAsync(section.Name);
            }
            files = await decryptFolder.GetFilesAsync();
            var storageFile = await DetectToc(files);
            if (storageFile!=null)
            {
                if (!storageFile.Name.Equals("TOC.html"))
                {
                    await storageFile.RenameAsync("TOC.html");
                }
            }
            var dataIndexJson = JsonConvert.SerializeObject(landInfo, Formatting.Indented);
            var indexFile = await decryptFolder.CreateFileAsync("index.json");
            await FileIO.WriteTextAsync(indexFile, dataIndexJson);
        }

     

        private async Task<IBuffer> DecryptedData(StorageFile encryptFile, string key)
        {
            var buffer = await FileIO.ReadBufferAsync(encryptFile);
            var decryptedData = _securityService.XOEncrypt(key, buffer);
            return decryptedData;
        }

        private async Task FilesDecrypt(IEnumerable<StorageFile> encryptFiles, string key,
            StorageFolder folderBookDecrypt)
        {
            foreach (var encryptFile in encryptFiles)
            {
                string fileName = encryptFile.Name;
                var decryptedData = await DecryptedData(encryptFile, key);
                var contents =
                    Windows.Security.Cryptography.CryptographicBuffer.ConvertBinaryToString(
                        Windows.Security.Cryptography.BinaryStringEncoding.Utf8, decryptedData);
             
              
                var fileDecrypt =
                    await folderBookDecrypt.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(fileDecrypt, contents, UnicodeEncoding.Utf8);
            }
        }

        public async Task<IReadOnlyList<StorageFolder>> GetFolders(StorageFolder storageFolder)
        {
            return await storageFolder.GetFoldersAsync();
        }


        public static String StripUnicodeCharactersFromString(string inputValue)
        {
            string strFormD = inputValue.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < strFormD.Length; i++)
            {
                System.Globalization.UnicodeCategory uc =
                    System.Globalization.CharUnicodeInfo.GetUnicodeCategory(strFormD[i]);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(strFormD[i]);
                }
            }
            sb = sb.Replace('Đ', 'D');
            sb = sb.Replace('đ', 'd');
            return (sb.ToString().Normalize(NormalizationForm.FormD));
        }

        public static async Task Log(StorageFolder folder, string msg)
        {
            var fileError = await folder.CreateFileAsync("error.txt", CreationCollisionOption.OpenIfExists);
            string lineError = msg;
            await FileIO.AppendTextAsync(fileError, lineError, UnicodeEncoding.Utf8);
        }

        private static async Task<StorageFile> DetectToc(IReadOnlyList<StorageFile> files)
        {
            foreach (var file in files)
            {
                var content = await FileIO.ReadTextAsync(file, UnicodeEncoding.Utf8);
                if (IsTocFile(content))
                {
                    return file;
                }
            }
            return null;
        }

        private static bool IsTocFile(string content)
        {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(content);
            if (document.DocumentNode != null)
            {
                var count = GetNodeHrefToc(document.DocumentNode).Count();
                if (count > 1)
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<HtmlNode> GetNodeHrefToc(HtmlNode documentNode)
        {
            return documentNode.Descendants().Where(a => a.Name == "a" && a.Attributes["href"] != null &&
                                                         a.Attributes["href"].Value.Contains(".html"));
        }
    }


    public static class ExtensionMethods
    {
        public static string Replace(this string s, char[] separators, string newVal)
        {
            string[] temp;

            temp = s.Split(separators);
            return String.Join(newVal, temp);
        }
    }
   
}