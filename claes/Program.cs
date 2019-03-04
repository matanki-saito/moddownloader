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

            int idx = 0;
            keyDict = new Dictionary<string, string>();
            foreach (FileInfo fileInfo in files)
            {
                string tmodfilePath = fileInfo.FullName;

                using (StreamReader r = new StreamReader(tmodfilePath))
                {
                    string keyFilePath = r.ReadToEnd();
                    string keyFileMd5 = CalculateMD5(keyFilePath);

                    keyDict.Add(Path.GetFileNameWithoutExtension(fileInfo.Name), keyFileMd5);
                }

                // コンポーネントを作成
                SetComponents(idx++);
            }

            //
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 500;
            this.Height = 200;
            this.AutoScroll = true;
            this.Text = "ファイルのダウンロード";
        }



        private void SetComponents(int n)
        {
            n++;

            Font font = new Font("Meiryo UI", 10);

            ProgressBar bar = new ProgressBar()
            {
                Size = new Size(450, 20),
                Location = new Point(20, 40* n),
                Minimum = 0,
                Maximum = 1000000,
                Value = 0,
                Parent = this,
            };

            Button runBtn = new Button()
            {
                Size = new Size(100, 25),
                Font = font,
                Location = new Point(360, 120* n),
                Text = "キャンセル",
                Name = "Cancel",
                Parent = this,
            };
            //runBtn.Click += new EventHandler(actionRun);

            Label title = new Label()
            {
                Text = "準備中…",
                Font = font,
                Size = new Size(450, 20),
                Location = new Point(20, 20 * n),
                Parent = this,
            };

            Label progress = new Label()
            {
                Text = "準備中…",
                Font = font,
                Size = new Size(450, 20),
                Location = new Point(20, 70 * n),
                Parent = this,
            };

            Label address = new Label()
            {
                Text = "準備中…",
                Font = font,
                Size = new Size(450, 20),
                Location = new Point(20, 95 * n),
                Parent = this,
            };

            this.downloadViews.Add(new DownloadView(bar, runBtn, progress, address, title));

        }

        private void FormShow(Object sender, EventArgs e)
        {
            int idx = 0;

            Parallel.ForEach(keyDict, item => {

                Downloader dl = new Downloader(downloadViews[idx++]);

                string cdnUrl = "https://d3mq2c18xv0s3o.cloudfront.net/api/v1/distribution/" + item.Key + "/" + item.Value;

                dl.StartDownLoad(new Uri(cdnUrl), Path.Combine(cacheDirectory, "a.zip"), this);

            });

            System.Diagnostics.Debug.WriteLine("form finish");
        }
        private void actionClose(object sender, EventArgs e) { this.Close(); }

    }

    class Downloader
    {
        private DownloadView dView;
        private WebClient wcw;
        private Form a;

        public Downloader(DownloadView downloadView)
        {
            dView = downloadView;
            dView.CancelBtn.Click += new EventHandler(ActionRun);
        }

        public void StartDownLoad(Uri uri, string fileName, Form a)
        {

            this.a = a;

            //非同期ダウンロードを開始する
            using (WebClient wc = new WebClient())
            {
                wcw = wc;

                wc.DownloadFileAsync(uri, fileName);

                wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadChanged);

            }


            System.Diagnostics.Debug.WriteLine("dl finish");
        }

        private void ActionRun(object sender, EventArgs e)
        {
            //if (dView.CancelBtn.Name == "Cancel") wcw.CancelAsync();
            //if (dView.CancelBtn.Name == "Close") Application.Exit();
        }

        public void DownloadChanged(object sender,DownloadProgressChangedEventArgs e)
        {
            //string aaa = e.TotalBytesToReceive + "byte 中 " + e.BytesReceived + "byte";


            a.Invoke(new Action<long, long>(delegate (long totalBytes, long byteRec) {
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
        public DownloadView(ProgressBar progressBar, Button cancelBtn, Label progress, Label address, Label title)
        {
            ProgressBar = progressBar;
            CancelBtn = cancelBtn;
            Progress = progress;
            Address = address;
            Title = title;
        }

        public ProgressBar ProgressBar;
        public Button CancelBtn;
        public Label Progress;
        public Label Address;
        public Label Title;

    }
}
