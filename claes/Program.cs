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

namespace claes
{
    class DLProgress
    {
        //[STAThread]
        static void Main(string[] args)
        {
            Application.Run(new MainWindow());
        }
    }

    class MainWindow : Form
    {
        private string currentDirectory;
        private string cacheDirectory;
        private string keyDirectory;
        private Dictionary<string,string> keyDict;
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
            currentDirectory = Directory.GetCurrentDirectory();

            downloadViews = new List<DownloadView>();

            // cacheフォルダをチェック
            cacheDirectory = Path.Combine(currentDirectory, "claes.cache");
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            // ./resourceフォルダをチェック
            keyDirectory = Path.Combine(currentDirectory, "claes.key");
            if (!Directory.Exists(keyDirectory))
            {
                Directory.CreateDirectory(keyDirectory);
            }

            // resourceフォル内のリソースを列挙
            DirectoryInfo di = new System.IO.DirectoryInfo(keyDirectory);
            FileInfo[] files = di.GetFiles("*.key", SearchOption.TopDirectoryOnly);

            TableLayoutPanel TableLayoutPanel1 = new TableLayoutPanel()
            {
                Location = new Point(0, 0),
                Width = 550,
                Height = 250,
                Margin= new Padding(0,0,0,0),
                Padding = new Padding(0, 0, 0, 0),
                Parent = this
            };


            int idx = 0;
            keyDict = new Dictionary<string, string>();
            foreach (FileInfo fileInfo in files)
            {
                string tmodfilePath = fileInfo.FullName;

                using (StreamReader r = new StreamReader(tmodfilePath))
                {
                    string keyFilePath = r.ReadToEnd();

                    keyDict.Add(Path.GetFileNameWithoutExtension(fileInfo.Name), keyFilePath);
                }

                // コンポーネントを作成
                SetComponents(idx++, TableLayoutPanel1);
            }

            //
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 580;
            this.Height = 300;
            this.AutoScroll = true;
            this.Text = "ファイルのダウンロード";
        }



        private void SetComponents(int n, TableLayoutPanel table)
        {
            Font font = new Font("Meiryo UI", 8);

            Label title = new Label()
            {
                Text = "Name",
                Font = font,
                Size = new Size(50, 20)
            };
            table.Controls.Add(title, 1, n);

            Label progress = new Label()
            {
                Text = "準備中…",
                Font = font,
                Size = new Size(100, 20)
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
            int idx = 0;

            List<Task> tasks = new List<Task>();

            foreach (KeyValuePair<string,string> item in keyDict){

                Downloader dl = new Downloader(downloadViews[idx++]);

                string exeMd5 = CalculateMD5(item.Value);

                string cdnUrl = "https://d3mq2c18xv0s3o.cloudfront.net/api/v1/distribution/" + item.Key + "/" + exeMd5;

                tasks.Add(dl.StartDownLoad(new Uri(cdnUrl), Path.Combine(cacheDirectory,item.Key), this));

            };

            await Task.WhenAll(tasks);

            System.Diagnostics.Debug.WriteLine("form finish");

            //Application.Exit();
        }
        private void actionClose(object sender, EventArgs e) { this.Close(); }

    }

    class Downloader
    {
        private DownloadView dView;
        private Form uiThread;

        public Downloader(DownloadView downloadView)
        {
            dView = downloadView;
            dView.CancelBtn.Click += new EventHandler(ActionRun);
        }

        public Task StartDownLoad(Uri uri, string fileName, Form uiThread)
        {
            this.uiThread = uiThread;
            
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

        public void DownloadChanged(object sender,DownloadProgressChangedEventArgs e)
        {

            uiThread.Invoke(new Action<long, long>(delegate (long totalBytes, long byteRec) {
                    dView.Progress.Text = e.TotalBytesToReceive + "byte / " + e.BytesReceived + "byte";

                    if (totalBytes > byteRec)
                    {
                        dView.ProgressBar.Maximum = (int)(totalBytes / 1000);
                        dView.ProgressBar.Value = (int)(byteRec / 1000);
                    }
                }
            ), e.TotalBytesToReceive, e.BytesReceived);

        }
    }

    class DownloadView
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
}
