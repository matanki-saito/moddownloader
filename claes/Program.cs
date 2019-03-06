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

        public ProgressBar ProgressBar { get; }
        public Button CancelBtn { get; }
        public Label Progress { get; }
        public Label Title { get; }

    }

    public class KeyFile
    {
        public KeyFile(string keyFilePath, string title)
        {
            KeyFilePath = keyFilePath;
            Title = title;
        }

        public string KeyFilePath { get; }
        public string Title { get;}
    }

    internal class Downloader
    {
        private static string baseUrl = "https://d3mq2c18xv0s3o.cloudfront.net/api/v1/distribution/";
        
        private readonly DownloadView dView;
        private readonly Form uiThread;

        public Downloader(DownloadView downloadView, Form uiThread, string title)
        {
            this.uiThread = uiThread;
            dView = downloadView;
            dView.CancelBtn.Click += ActionRun;

            uiThread.Invoke(new Action<string>(delegate (string titleD) {
                dView.Title.Text = titleD;
            }), title);
        }

        private static void ActionRun(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("cancel button");
        }

        public Task StartDownLoad(string key, string exeMd5, string fileName)
        {
            Task downloadTask;
            using (var wc = new WebClient())
            {
                wc.DownloadProgressChanged += DownloadChanged;
                downloadTask = wc.DownloadFileTaskAsync(new Uri(baseUrl + key + "/" + exeMd5), fileName);
            }

            return downloadTask;
        }

        private void DownloadChanged(object sender, DownloadProgressChangedEventArgs e)
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
    
    internal class MainWindow : Form
    {
        private string currentDirectory;
        private string cacheDirectory;
        private string keyDirectory;
        private string modDirectory;
        private ConcurrentDictionary<string, KeyFile> keyDict;
        private ConcurrentDictionary<string, string> keyFilePath2Md5;
        private List<DownloadView> downloadViews;

        private static string CalculateMd5(string filename)
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
            Shown += FormShow;
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
                    var lists = lines.Split('\n');
                    if(lists.Length >= 2)
                    {
                        keyDict.GetOrAdd(Path.GetFileNameWithoutExtension(fileInfo.Name), new KeyFile(lists[1], lists[0]));
                    }
                }
            });

            // キーファイルを集約してMD5化する。ほとんど同じファイルを見ているはずなので…
            keyFilePath2Md5 = new ConcurrentDictionary<string, string>();
            Parallel.ForEach(keyDict.Values.Select(x => x.KeyFilePath).Distinct(), keyFilePath =>
            {
                keyFilePath2Md5.GetOrAdd(keyFilePath, CalculateMd5(keyFilePath));
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
            for (var i = 0; i < files.Length; i++)
            {
                SetComponents(i, tableLayoutPanel1);
            }

            System.Diagnostics.Debug.WriteLine("setup実行しました");
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

        private async void FormShow(Object sender, EventArgs e)
        {
            // ダウンロードは別スレッドに投げて非同期でやる
            var tasks = new List<Task>();
            var idx = 0;
            foreach (var item in keyDict){
                var dl = new Downloader(downloadViews[idx++],this, item.Value.Title);
                var exeMd5 = keyFilePath2Md5.GetOrAdd(item.Value.KeyFilePath,"NO-MD5");
                tasks.Add(dl.StartDownLoad(item.Key,exeMd5 , Path.GetTempFileName()));
            }

            // 終わるまで待ってる
            try
            {
                await Task.WhenAll(tasks);
            }catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            System.Diagnostics.Debug.WriteLine("All file downloading is finished.");

            // zipファイル展開
            // これは非同期でやらない。なぜならば展開に順番が必要だから
            var di = new DirectoryInfo(cacheDirectory);
            var files = di.GetFiles("*", SearchOption.TopDirectoryOnly);
            foreach(var info in files)
            {
                ExtractToDirectory(info.FullName, modDirectory, true);
            }

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

    class DLProgress
    {
        [STAThread]
        private static void Main()
        {
            Application.Run(new MainWindow());
        }
    }
}
