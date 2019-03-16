using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO.Compression;
using System.Reflection;

namespace claes
{
    public class DownloadView
    {
        public DownloadView(ProgressBar progressBar, Button cancelBtn, Label progress, Label title)
        {
            ProgressBar = progressBar;
            CancelBtn = cancelBtn;
            Progress = progress;
            Title = title;
        }       

        public ProgressBar ProgressBar { get; }
        public Button CancelBtn { get; }
        public Label Progress { get; }
        public Label Title { get; }

    }

    public class KeyFile
    {
        public KeyFile(string keyFilePath, string title, bool develop)
        {
            KeyFilePath = keyFilePath;
            Title = title;
            DownloadComplete = false;
            Develop = develop;
        }

        public string KeyFilePath { get; }
        public string Title { get;}
        public bool DownloadComplete { set; get; }
        
        public bool Develop { set; get; }
        public string TempFileName { get; set; }
        public string TempFileMd5 { get; set; }
        
        public string cachedMd5 { get; set; }
    }

    internal class Downloader
    {
        private const string BaseUrl = "https://d3mq2c18xv0s3o.cloudfront.net/api/v1/distribution/";
        private const string BaseDevUrl = "https://triela.tk:8443/api/v1/distribution/";
        
        private readonly DownloadView dView;
        private readonly MainWindow uiThread;
        private readonly KeyFile keyFile;

        public Downloader(DownloadView downloadView, MainWindow uiThread, KeyFile keyFile)
        {
            this.uiThread = uiThread;
            this.keyFile = keyFile;
            dView = downloadView;
            dView.CancelBtn.Click += ActionRun;

            uiThread.Invoke(new Action<string>(delegate (string titleD) {
                dView.Title.Text = titleD;
            }), keyFile.Title);
        }

        private static void ActionRun(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("cancel button");
        }

        public Task StartDownLoad(string key, string exeMd5)
        {
            Task downloadTask;
            using (var wc = new WebClient())
            {
                var f = keyFile.TempFileName =Path.GetTempFileName();
                wc.DownloadProgressChanged += DownloadChanged;
                wc.DownloadFileCompleted += MyDownloadFileCompletedFunc;

                var nvc = new NameValueCollection();

                if (keyFile.cachedMd5 != null)
                {
                    nvc.Add("dll_md5",keyFile.cachedMd5);
                }
                if (keyFile.Develop)
                {
                    nvc.Add("phase","dev");
                }

                wc.QueryString = nvc;                
                var uri = new Uri((keyFile.Develop ? BaseDevUrl : BaseUrl) + key + "/" + exeMd5);                
                downloadTask = wc.DownloadFileTaskAsync(uri,f);
            }

            return downloadTask;
        }

        private void MyDownloadFileCompletedFunc(object sender,AsyncCompletedEventArgs args)
        {
            if (args.Error == null)
            {
                keyFile.DownloadComplete = true;
            }

            keyFile.TempFileMd5 = Util.CalculateMd5(keyFile.TempFileName);
        }

        private void DownloadChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            uiThread.Invoke(new Action<long, long>(delegate (long totalBytes, long byteRec) {
                dView.Progress.Text = e.TotalBytesToReceive + "byte / " + e.BytesReceived + "byte";
                
                if (!uiThread.viewLatch)
                {
                    uiThread.ShowInTaskbar = true;
                    uiThread.WindowState = FormWindowState.Normal;
                    uiThread.viewLatch = true;
                }
                
                if (totalBytes <= byteRec) return;
                
                dView.ProgressBar.Maximum = (int)(totalBytes / 1000);
                dView.ProgressBar.Value = (int)(byteRec / 1000);
            }), e.TotalBytesToReceive, e.BytesReceived);
        }
    }

    internal static class Util
    {
        public static string CalculateMd5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
    
    internal class MainWindow : Form
    {
        private string currentDirectory;
        private string cacheDirectory;
        private string keyDirectory;
        private string modDirectory;
        private ConcurrentDictionary<string, KeyFile> keyDict;
        private ConcurrentDictionary<string, string> keyFilePath2Md5;
        private List<DownloadView> downloadViews;
        public bool viewLatch { get; set; }
        
        public MainWindow()
        {
            viewLatch = false;
            SetUp(); 
            Shown += FormShow;
        }

        private void SetUp()
        {
            // exeのあるディレクトリを基準とする
            currentDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;

            // cacheフォルダをチェック
            cacheDirectory = Path.Combine(currentDirectory, "claes.cache");
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            // フォルダをチェック
            keyDirectory = Path.Combine(currentDirectory, "claes.key");
            if (!Directory.Exists(keyDirectory))
            {
                Directory.CreateDirectory(keyDirectory);
            }

            // ./modフォルダをチェック
            modDirectory = Path.Combine(currentDirectory, "mod");
            if (!Directory.Exists(modDirectory))
            {
                Directory.CreateDirectory(modDirectory);
            }
            
            // .keyを見る
            var di = new DirectoryInfo(keyDirectory);
            var files = di.GetFiles("*.key", SearchOption.TopDirectoryOnly);
            keyDict = new ConcurrentDictionary<string, KeyFile>();
            Parallel.ForEach(files, fileInfo =>
            {
                // 行読み取り
                using (var r = new StreamReader(fileInfo.FullName))
                {
                    var lines = r.ReadToEnd();
                    // 行で分割
                    string [] del = { "\r\n" };
                    var lists = lines.Split(del,StringSplitOptions.None);
                    if(lists.Length >= 2)
                    {
                        keyDict.GetOrAdd(Path.GetFileNameWithoutExtension(fileInfo.Name),
                            new KeyFile(
                                lists[1],
                                lists[0],
                                lists.Length > 2 && lists[2] == "dev"
                                )
                            );
                    }
                }
            });
            
            // .cacheを見る
            di = new DirectoryInfo(cacheDirectory);
            files = di.GetFiles("*.cache", SearchOption.TopDirectoryOnly);
            Parallel.ForEach(files, fileInfo =>
            {
                // 行読み取り
                using (var r = new StreamReader(fileInfo.FullName))
                {
                    var lines = r.ReadToEnd();
                    // 行で分割
                    string [] del = { "\r\n" };
                    
                    var lists = lines.Split(del,StringSplitOptions.None);
                    if (lists.Length != 1) return;
                    
                    var key = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    var k = keyDict.GetOrAdd(key,new KeyFile("?", "no-title",false));
                    k.cachedMd5 = lists[0];
                    keyDict.AddOrUpdate(key, new KeyFile("?", "no-title",false), (s, file) => k);
                }
            });

            // キーファイルを集約してMD5化する。ほとんど同じファイルを見ているはずなので…
            keyFilePath2Md5 = new ConcurrentDictionary<string, string>();
            Parallel.ForEach(keyDict.Values.Select(x => x.KeyFilePath).Distinct(), keyFilePath =>
            {
                keyFilePath2Md5.GetOrAdd(keyFilePath, Util.CalculateMd5(keyFilePath));
            });

            // Viewを作成する
            StartPosition = FormStartPosition.CenterScreen;
            Width = 820;
            Height = 300;
            AutoScroll = true;
            Text = "ファイルのダウンロード";
            var tableLayoutPanel1 = new TableLayoutPanel
            {
                Location = new Point(0, 0),
                Width = 800,
                Height = 250,
                Margin = new Padding(0, 0, 0, 0),
                Padding = new Padding(0, 0, 0, 0),
                Parent = this
            };
            downloadViews = new List<DownloadView>();
            for (var i = 0; i < keyDict.Count; i++)
            {
                SetComponents(i, tableLayoutPanel1);
            }
        }


        private void SetComponents(int n, TableLayoutPanel table)
        {
            var font = new Font("Meiryo UI", 8);

            var title = new Label
            {
                Text = "Name",
                Font = font,
                Size = new Size(200, 20)
            };
            table.Controls.Add(title, 1, n);

            var progress = new Label
            {
                Text = "準備中…",
                Font = font,
                Size = new Size(200, 20)
            };
            table.Controls.Add(progress, 2, n);

            var bar = new ProgressBar
            {
                Size = new Size(250, 20),
                Minimum = 0,
                Maximum = 1000000,
                Value = 0
            };
            table.Controls.Add(bar, 3, n);

            var runBtn = new Button
            {
                Size = new Size(100, 20),
                Font = font,
                Text = "キャンセル",
                Name = "Cancel"
            };
            table.Controls.Add(runBtn, 4, n);
            downloadViews.Add(new DownloadView(bar, runBtn, progress, title));

        }

        private async void FormShow(object sender, EventArgs e)
        {   
            // ダウンロードは別スレッドに投げて非同期でやる
            var tasks = new List<Task>();
            var idx = 0;
            foreach (var item in keyDict)
            {
                var dl = new Downloader(downloadViews[idx++], this,item.Value);
                var exeMd5 = keyFilePath2Md5.GetOrAdd(item.Value.KeyFilePath,"NO-MD5");
                tasks.Add(dl.StartDownLoad(item.Key,exeMd5));
            }

            // 終わるまで待ってる
            try
            {
                await Task.WhenAll(tasks);
            }catch(Exception ex)
            {
                // 410とか
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            await Task.Run(() =>
            {
                // zipファイル展開
                // これは非同期でやらない。なぜならば展開に順番が必要だから
                foreach (var item in keyDict)
                {
                    if (!item.Value.DownloadComplete) continue;

                    ExtractToDirectory(item.Value.TempFileName, modDirectory, true);

                    // キャッシュフォルダ作る
                    using (var w = new StreamWriter(Path.Combine(cacheDirectory, item.Key + ".cache")))
                    {
                        w.Write(item.Value.TempFileMd5);
                    }
                }
            });
            
            Application.Exit();
        }
        
        private static void ExtractToDirectory(string archivePath, string destinationDirectoryName, bool overwrite)
        {
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                if (!overwrite)
                {
                    archive.ExtractToDirectory(destinationDirectoryName);
                    return;
                }
                foreach (var file in archive.Entries)
                {
                    var completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                    var directory = Path.GetDirectoryName(completeFileName);

                    if (!Directory.Exists(directory) && directory != null)
                        Directory.CreateDirectory(directory);

                    if (file.Name != "")
                        file.ExtractToFile(completeFileName, true);
                }
            }
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 非表示
            // http://bbs.wankuma.com/index.cgi?mode=red&namber=31080&KLOG=55
            Form f = new MainWindow();
            f.ShowInTaskbar = false;
            f.WindowState = FormWindowState.Minimized;
            Application.Run(f);
        }
    }
}
