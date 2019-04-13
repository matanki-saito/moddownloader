using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace claes
{
    public partial class ModSetChangerForm : Form
    {
        private List<ModSet> modsets;
        private List<string> installedMods;

        private System.Diagnostics.Process eu4process;

        private MyComputerConfiguration config;

        private SettingsTxt settingsTxt;

        private FileSystemWatcher currentModDirectoryObserver = null;

        public ModSetChangerForm()
        {
            InitializeComponent();
            OptionalInitilaize();
            // WakeUpEu4();
        }

        private void WakeUpEu4()
        {
            if(eu4process != null)
            {
                eu4process.CloseMainWindow();
            }

            eu4process = new System.Diagnostics.Process();
            eu4process.StartInfo.FileName = @"C:\Program Files (x86)\Steam\steamapps\common\Europa Universalis IV\eu4.exe";

            bool result = eu4process.Start();
        }

        private void OptionalInitilaize()
        {
            config = new MyComputerConfiguration("Europa Universalis IV");

            if (config.ModsetDirectory != null) {
                LoadModSets();
                SetUpCombox();
            }

            EnumInstalledMods();

            // Modフォルダの監視を開始する
            SetModDirectoryObserver();

            settingsTxt = new SettingsTxt(config.SettingsTxtFile);
            settingsTxt.Load();
        }

        private void SetUpCombox()
        {
            foreach(var item in modsets)
            {
                comboBox1.Items.Add(item.name);
            }   
        }

        private void EnumInstalledMods()
        {
            var di = new DirectoryInfo(config.ModDirectory);
            var files = di.GetFiles("*.mod", SearchOption.TopDirectoryOnly);

            installedMods = new List<string>();
            foreach (var fileInfo in files)
            {
                installedMods.Add(fileInfo.Name);
            }
        }

        private void LoadModSets()
        {
            var di = new DirectoryInfo(config.ModsetDirectory);
            var files = di.GetFiles("*.yml", SearchOption.TopDirectoryOnly);

            var deserializer = new DeserializerBuilder().Build();

            modsets = new List<ModSet>();
            foreach (var fileInfo in files)
            {
                using (var text = new StreamReader(fileInfo.FullName))
                {
                    modsets.Add(deserializer.Deserialize<ModSet>(text));
                }
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            // 全てのチェックがオンなことを確認する
            for(var i=0; i< checkedListBox1.Items.Count; i++)
            {
                if (!checkedListBox1.GetItemChecked(i))
                {
                    MessageBox.Show("全てのチェックが付いている必要があります");
                    return;
                }
            }

            // 必要なModリストを作る
            var requiredModFilePaths = new List<string>();
            foreach(var item in modsets[comboBox1.SelectedIndex].required)
            {
                requiredModFilePaths.Add(item.file);
            }
            settingsTxt.ActiveMods = requiredModFilePaths;

            // 更新
            settingsTxt.Update();

            // WakeUpEu4();
        }

        private void ComboBox1_ItemChecked(object sender, ItemCheckEventArgs e)
        {
            if (e.CurrentValue == CheckState.Checked)
            {
                if (e.NewValue == CheckState.Checked)
                {
                    /* NOT CHANGED */
                }
                else
                {
                    // オフにすることはできません
                    e.NewValue = CheckState.Checked;
                }
            }else
            {
                // チェックが入りました
                if(e.NewValue == CheckState.Checked)
                {
                    var currentSet = modsets[comboBox1.SelectedIndex];
                    var targetMod = currentSet.required[e.Index];
                    if (targetMod.s_id != 0)
                    {
                        // streamのページを開かせる
                        System.Diagnostics.Process.Start("https://steamcommunity.com/sharedfiles/filedetails/?l=japanese&id=" + targetMod.s_id);
                        //　オンにすることはここではできない
                        e.NewValue = CheckState.Unchecked;
                    } else
                    {
                        /* keyファイルを設置する */
                    }
                }
                else
                {
                    /* NOT CHANGED */
                }
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetModList();
        }

        private void SetModList()
        {
            var index = comboBox1.SelectedIndex;
            var requiredMods = modsets[index].required;

            // ModSetを切り替えたら表はリセットする
            checkedListBox1.Items.Clear();

            // URL開いてしまうので一時無効化する
            checkedListBox1.ItemCheck -= ComboBox1_ItemChecked;

            for (var i = 0; i < requiredMods.Count(); i++)
            {
                var item = requiredMods[i];

                checkedListBox1.Items.Add(item.name);
                var flag = false;
                if (this.installedMods.Contains(item.file))
                {
                    flag = true;
                }
                checkedListBox1.SetItemChecked(i, flag);
            }

            checkedListBox1.ItemCheck += ComboBox1_ItemChecked;
        }

        // https://dobon.net/vb/dotnet/file/filesystemwatcher.html
        private void SetModDirectoryObserver()
        {
            if (currentModDirectoryObserver != null)
            {
                currentModDirectoryObserver.EnableRaisingEvents = false;
                currentModDirectoryObserver.Dispose();
                currentModDirectoryObserver = null;
            }

            currentModDirectoryObserver = new System.IO.FileSystemWatcher
            {
                //監視するディレクトリを指定
                Path = config.ModDirectory,
                //ファイル、フォルダ名の変更を監視する
                NotifyFilter = (
                  System.IO.NotifyFilters.LastWrite
                | System.IO.NotifyFilters.FileName
                ),
                //すべてのファイルを監視
                Filter = "*.mod",
                SynchronizingObject = this
            };

            currentModDirectoryObserver.Created += ModDirectoryChangedEventHandler;
            currentModDirectoryObserver.Deleted += ModDirectoryChangedEventHandler;

            //監視を開始する
            currentModDirectoryObserver.EnableRaisingEvents = true;
        }

        private void ModDirectoryChangedEventHandler(Object source, FileSystemEventArgs e)
        {
            EnumInstalledMods();
            SetModList();
        }
    }
}
