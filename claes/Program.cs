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

namespace claes
{
    class DLProgress
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.Run(new MainWindow(args));
        }
    }

    class MainWindow : Form
    {
        private WebClient wc;
        private Label title;
        private Label progress;
        private Label adress;
        private string url;
        private ProgressBar bar;
        private string fileName;
        private Button runBtn;
        private Button folderBtn;

        public MainWindow(string[] args)
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 500;
            this.Height = 200;
            this.Text = "ファイルのダウンロード";
            this.setComponents();

            url = "";
            if (args.Length > 0) url = args[0];

            this.Shown += new EventHandler(FormShow);
        }

        private void FormShow(Object sender, EventArgs e)
        {
            if (url ==""){
                MessageBox.Show("引数にURLが指定されていません。強制終了します。");
                Application.Exit();
            }

            SaveFileDialog dialog = new SaveFileDialog();
            DialogResult result = dialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                fileName = dialog.FileName;//ダウンロードしたファイルの保存先
                this.startDownLoad();
                adress.Text = "保存先:" +fileName;
            }
            else
            {
                Application.Exit();
            }
        }

        private void startDownLoad()
        {
            //ダウンロード基のURL
            Uri address = new Uri(url);
            //非同期ダウンロードを開始する
            wc = new WebClient();
            wc.DownloadFileAsync(address, fileName);

            wc.DownloadProgressChanged +=
                    new DownloadProgressChangedEventHandler(downloadChanged);
            wc.DownloadFileCompleted +=
                    new AsyncCompletedEventHandler(downloadCompleted);
        }

        private void actionRun(object sender, EventArgs e)
        {
            if (runBtn.Name =="Cancel") wc.CancelAsync();
            if (runBtn.Name =="Close") Application.Exit();
        }

        private void actionOpenFolder(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(fileName);
        }

        private void downloadChanged(object sender,
                                       DownloadProgressChangedEventArgs e)
        {
            progress.Text = e.TotalBytesToReceive + "byte 中 " +
                                                 e.BytesReceived + "byte";
            if (e.TotalBytesToReceive > e.BytesReceived)
            {
                bar.Maximum = (int)(e.TotalBytesToReceive / 1000);
                bar.Value = (int)(e.BytesReceived / 1000);
            }
            this.Text = e.ProgressPercentage + "% ファイルのダウンロード";
        }


        private void downloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show("ダウンロードが中断されました。");
                Application.Exit();
            }
            else if (e.Cancelled)
            {
                title.Text = "ダウンロードがキャンセルされました。";
            }
            else
            {
                title.Text = "ダウンロードが完了しました。";
                folderBtn.Enabled = false;
            }
            runBtn.Name ="Close";
            runBtn.Text ="閉じる";
        }

        private void actionClose(object sender, EventArgs e) { this.Close(); }

        private void setComponents()
        {
            Font font = new Font("Meiryo UI", 10);

            bar = new ProgressBar()
            {
                Size = new Size(450, 20),
                Location = new Point(20, 40),
                Minimum = 0,
                Maximum = 1000000,
                Value = 0,
                Parent = this,
            };

            runBtn = new Button()
            {
                Size = new Size(100, 25),
                Font = font,
                Location = new Point(360, 120),
                Text = "キャンセル",
                Name = "Cancel",
                Parent = this,
            };
            runBtn.Click += new EventHandler(actionRun);

            folderBtn = new Button()
            {
                Size = new Size(100, 25),
                Font = font,
                Location = new Point(250, 120),
                Text = "フォルダを開く",
                Enabled = false,
                Parent = this,
            };
            folderBtn.Click += new EventHandler(actionOpenFolder);

            title = new Label()
            {
                Text = "準備中…",
                Font = font,
                Size = new Size(450, 20),
                Location = new Point(20, 20),
                Parent = this,
            };

            progress = new Label()
            {
                Text = "準備中…",
                Font = font,
                Size = new Size(450, 20),
                Location = new Point(20, 70),
                Parent = this,
            };

            adress = new Label()
            {
                Text = "準備中…",
                Font = font,
                Size = new Size(450, 20),
                Location = new Point(20, 95),
                Parent = this,
            };

        }
    }
}
