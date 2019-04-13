using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace claes
{
    class MyComputerConfiguration
    {
        public string GameHomeDirectoryName { get; set; }
        public string GameInstallDirectory { get; set; }
        public string GameHomeDirectory { get; set; }
        public string CurrentDirectory { get; set; }
        public string ModsetDirectory { get; set; }
        public string ModDirectory { get; set; }
        public string SettingsTxtFile { get; set; }

        public MyComputerConfiguration(string gameHomeDirectoryName)
        {
            GameHomeDirectoryName = gameHomeDirectoryName;

            GetCurrentDirectory();
            GetGameHomeDirectory();
            GetModSetDirectory();
            GetModDirectory();
            GetSettingsTxtFile();
        }

        private void GetCurrentDirectory()
        {
            // exeのあるディレクトリ
            CurrentDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
        } 

        private void GetGameHomeDirectory()
        {
            // レジストリを見て、MyDocumentsの場所を探す
            var regkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders",
                false
            );

            if (regkey == null)
            {
                throw new Exception("レジストリからMyDocumentsの位置を見つけられませんでした");
            }

            var myDocumentsPath = (string)regkey.GetValue("Personal");
            if(myDocumentsPath == null)
            {
                throw new Exception("MyDocumentsの位置が不明です");
            }

            // ゲームホームディレクトリを見つける
            var tmp = Path.Combine(myDocumentsPath, "Paradox Interactive", GameHomeDirectoryName);
            if (!Directory.Exists(tmp))
            {
                throw new Exception("Gameのホームディレクトリが不正です");
            }

            GameHomeDirectory = tmp;
        }

        private void GetModSetDirectory()
        {
            var tmp = Path.Combine(GameHomeDirectory, "claes.set");
            if (!Directory.Exists(tmp))
            {
                ModsetDirectory = null;
            }
            else
            {
                ModsetDirectory = tmp;
            }
        }

        private void GetModDirectory()
        {
            var tmp = Path.Combine(GameHomeDirectory, "mod");
            if (!Directory.Exists(tmp))
            {
                throw new Exception("modフォルダがありません");
            }
            ModDirectory = tmp;
        }

        private void GetSettingsTxtFile()
        {
            var tmp = Path.Combine(GameHomeDirectory, "settings.txt");
            if (!File.Exists(tmp))
            {
                throw new Exception("settings.txtがありません");
            }
            SettingsTxtFile = tmp;
        }

    }
}
