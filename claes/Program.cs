using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.IO.Compression;

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

        public ProgressBar ProgressBar;
        public Button CancelBtn;
        public Label Progress;
        public Label Title;

    }

    public static class ZipArchiveExtensions
    {
        public static void ExtractToDirectory(this string archivePath, string destinationDirectoryName, bool overwrite)
        {
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                if (!overwrite)
                {
                    archive.ExtractToDirectory(destinationDirectoryName);
                    return;
                }
                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                    string directory = Path.GetDirectoryName(completeFileName);

                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    if (file.Name != "")
                        file.ExtractToFile(completeFileName, true);
                }
            }
        }
    }

    public class KeyFile
    {
        public KeyFile(string keyFilePath, string title)
        {
            this.keyFilePath = keyFilePath;
            this.title = title;
        }

        public string keyFilePath { get; set; }
        public string title { get; set; }
    }

    class Downloader
    {
        private DownloadView dView;
        private Form uiThread;
        private string title;

        public Downloader(DownloadView downloadView, Form uiThread, string title)
        {
            this.title = title;
            this.uiThread = uiThread;
            dView = downloadView;
            dView.CancelBtn.Click += new EventHandler(ActionRun);

            uiThread.Invoke(new Action<string>(delegate (string _title) {
                dView.Title.Text = _title;
            }), title);
        }

        public Task StartDownLoad(Uri uri, string fileName)
        {
            Task downloadTask;
            using (WebClient wc = new WebClient())
            {
                wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadChanged);
                downloadTask = wc.DownloadFileTaskAsync(uri, fileName);
            }

            return downloadTask;
        }

        private void ActionRun(object sender, EventArgs e)
        {
            //if (dView.CancelBtn.Name == "Cancel") wcw.CancelAsync();
            //if (dView.CancelBtn.Name == "Close") Application.Exit();
        }

        public void DownloadChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            uiThread.Invoke(new Action<long, long>(delegate (long totalBytes, long byteRec) {
                dView.Progress.Text = e.TotalBytesToReceive + "byte / " + e.BytesReceived + "byte";

                if (totalBytes > byteRec)
                {
                    dView.ProgressBar.Maximum = (int)(totalBytes / 1000);
                    dView.ProgressBar.Value = (int)(byteRec / 1000);
                }
            }), e.TotalBytesToReceive, e.BytesReceived);

        }
    }


    class MainWindow : Form
    {
        private string currentDirectory;
        private string cacheDirectory;
        private string keyDirectory;
        private string modDirectory;
        private ConcurrentDictionary<string, KeyFile> keyDict;
        private ConcurrentDictionary<string, string> keyFilePath2Md5;
        private List<DownloadView> downloadViews;


        static string CalculateMD5(string filename)
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



        public MainWindow()
        {
            SetUp();
            System.Diagnostics.Debug.WriteLine("start");
            this.Shown += new EventHandler(FormShow);
            System.Diagnostics.Debug.WriteLine("finish");
        }

        private void SetUp()
        {
            System.Diagnostics.Debug.WriteLine("setup実行します");

            // カレントディレクトリを基準とする
            currentDirectory = Directory.GetCurrentDirectory();

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
            DirectoryInfo di = new System.IO.DirectoryInfo(keyDirectory);
            FileInfo[] files = di.GetFiles("*.key", SearchOption.TopDirectoryOnly);
            keyDict = new ConcurrentDictionary<string, KeyFile>();
            Parallel.ForEach(files, fileInfo =>
            {
                // 行読み取り
                string lines = "";
                using (StreamReader r = new StreamReader(fileInfo.FullName))
                {
                    lines = r.ReadToEnd();
                }

                // 行で分割
                string [] lists = lines.Split('\n');
                if(lists.Length >= 2)
                {
                    keyDict.GetOrAdd(Path.GetFileNameWithoutExtension(fileInfo.Name), new KeyFile(lists[1], lists[0]));
                }
            });

            // キーファイルを集約してMD5化する。ほとんど同じファイルを見ているはずなので…
            keyFilePath2Md5 = new ConcurrentDictionary<string, string>();
            Parallel.ForEach(keyDict.Values.Select(x => x.keyFilePath).Distinct(), keyFilePath =>
            {
                keyFilePath2Md5.GetOrAdd(keyFilePath, CalculateMD5(keyFilePath));
            });

            // Viewを作成する
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 820;
            this.Height = 300;
            this.AutoScroll = true;
            this.Text = "ファイルのダウンロード";
            TableLayoutPanel TableLayoutPanel1 = new TableLayoutPanel()
            {
                Location = new Point(0, 0),
                Width = 800,
                Height = 250,
                Margin = new Padding(0, 0, 0, 0),
                Padding = new Padding(0, 0, 0, 0),
                Parent = this
            };
            downloadViews = new List<DownloadView>();
            for (int i = 0; i < files.Length; i++)
            {
                SetComponents(i, TableLayoutPanel1);
            }

            System.Diagnostics.Debug.WriteLine("setup実行しました");
        }


        private void SetComponents(int n, TableLayoutPanel table)
        {
            Font font = new Font("Meiryo UI", 8);

            Label title = new Label()
            {
                Text = "Name",
                Font = font,
                Size = new Size(200, 20)
            };
            table.Controls.Add(title, 1, n);

            Label progress = new Label()
            {
                Text = "準備中…",
                Font = font,
                Size = new Size(200, 20)
            };
            table.Controls.Add(progress, 2, n);

            ProgressBar bar = new ProgressBar()
            {
                Size = new Size(250, 20),
                Minimum = 0,
                Maximum = 1000000,
                Value = 0
            };
            table.Controls.Add(bar, 3, n);


            Button runBtn = new Button()
            {
                Size = new Size(100, 20),
                Font = font,
                Text = "キャンセル",
                Name = "Cancel"
            };
            table.Controls.Add(runBtn, 4, n);
            this.downloadViews.Add(new DownloadView(bar, runBtn, progress, title));

        }

        private async void FormShow(Object sender, EventArgs e)
        {
            // ダウンロードは別スレッドに投げて非同期でやる
            List<Task> tasks = new List<Task>();
            int idx = 0;
            foreach (KeyValuePair<string,KeyFile> item in keyDict){
                Downloader dl = new Downloader(downloadViews[idx++],this, item.Value.title);
                string exeMd5 = keyFilePath2Md5.GetOrAdd(item.Value.keyFilePath,"NO-MD5");
                string cdnUrl = "https://d3mq2c18xv0s3o.cloudfront.net/api/v1/distribution/" + item.Key + "/" + exeMd5;
                tasks.Add(dl.StartDownLoad(new Uri(cdnUrl), Path.Combine(cacheDirectory,item.Key)));
            };

            // 終わるまで待ってる
            try
            {
                await Task.WhenAll(tasks);
            }catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message); ;
            }
            System.Diagnostics.Debug.WriteLine("All file downloading is finished.");

            // zipファイル展開
            // これは非同期でやらない。なぜならば展開に順番が必要だから
            DirectoryInfo di = new System.IO.DirectoryInfo(cacheDirectory);
            FileInfo[] files = di.GetFiles("*", SearchOption.TopDirectoryOnly);
            foreach(FileInfo info in files)
            {
                ZipArchiveExtensions.ExtractToDirectory(info.FullName, modDirectory, true);
            }

            Application.Exit();
        }
        private void actionClose(object sender, EventArgs e) { this.Close(); }

    }

    class DLProgress
    {
        //[STAThread]
        static void Main(string[] args)
        {
            Application.Run(new MainWindow());
        }
    }
}
