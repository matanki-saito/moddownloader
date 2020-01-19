using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace dllautoupdater
{
    class Program
    {
        static void Main(string[] args)
        {
            // repository ID
            var repositoryId = "144728261";
            {
                var index = Array.IndexOf(args, "-repoid");
                if (index != -1 && args.Length > index + 1) repositoryId = args[index + 1];
            }

            // EXE
            var exePath = Path.Combine("..","eu4.exe");
            {
                var index = Array.IndexOf(args, "-exe");
                if (index != -1 && args.Length > index + 1) exePath = args[index + 1];
            }
            // check
            if (!File.Exists(exePath)) throw new Exception("exeが見つかりません");
            // exe md5
            var exeMd5 = CalculateMd5(exePath);

            // DLL
            var dllPath = Path.Combine("Plugin.dll");
            {
                var index = Array.IndexOf(args, "-dll");
                if (index != -1 && args.Length > index + 1) dllPath = args[index + 1];
            }
            // dll md5
            var dllMd5 = CalculateMd5(dllPath);

            // PHASE
            var phase = "prod";
            {
                var index = Array.IndexOf(args, "-phase");
                if(index != -1 && args.Length > index+1) phase = args[index+1];
            }
            var baseUrl = (phase == "prod") ?
                "https://d3mq2c18xv0s3o.cloudfront.net/" :
                "https://triela.ga:8443/";

            // assembly request params
            var nameValueCollection = new NameValueCollection();
            if (dllMd5 != null) nameValueCollection.Add("dll_md5", dllMd5);
            if (phase != null) nameValueCollection.Add("phase", phase);

            // get file
            using (var webClient = new WebClient())
            {
                webClient.QueryString = nameValueCollection;
                var uri = new Uri(baseUrl + "api/v1/distribution/" + repositoryId + "/" + exeMd5);

                try
                {
                    var data = webClient.DownloadData(uri);
                    File.WriteAllBytes(dllPath, data);
                }
                catch (Exception e)
                {
                    // 410 gone
                    Console.WriteLine(e);
                }
            }
        }

        public static string CalculateMd5(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
