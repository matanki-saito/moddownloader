using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        public string SteamInstallDirectory { get; set; }
        public string SteamDefaultAppsDirectory { get; set; }
        public int TargetAppId { get; set; }
        public string GameExeName { get; set; }
        public string GameExePath { get; set; }


        public MyComputerConfiguration(string gameHomeDirectoryName,int targetAppId,string gameExeName)
        {
            GameHomeDirectoryName = gameHomeDirectoryName;
            TargetAppId = targetAppId;
            GameExeName = gameExeName;

            GetCurrentDirectory();
            GetGameHomeDirectory();
            GetModSetDirectory();
            GetModDirectory();
            GetSettingsTxtFile();
            GetSteamInstallDirectory();
            GetGameInstallDirectory();

            GameExePath = Path.Combine(GameInstallDirectory, GameExeName);
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

            regkey.Close();

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

        private void GetSteamInstallDirectory()
        {
            // レジストリを見て、Steamのインストール先を探す
            // 32bitを見て、なければ64bitのキーを探しに行く。それでもなければそもそもインストールされていないと判断する
            var steamInstallKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Valve\Steam",
                false
            );

            if (steamInstallKey == null)
            {
                steamInstallKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\WOW6432Node\Valve\Steam",
                    false
                );

                if (steamInstallKey == null)
                {
                    throw new Exception("steamがインストールされていません");
                }
            }

            var tmp = (string)steamInstallKey.GetValue("InstallPath");
            if (tmp == null)
            {
                throw new Exception("steamのインストール先が不明です");
            }
            steamInstallKey.Close();
            SteamInstallDirectory = tmp;

            // steamappsディレクトリを探す。これはsteamのインストール先を変えてもかならず直下にできる
            tmp = Path.Combine(SteamInstallDirectory, "steamapps");
            if (!Directory.Exists(tmp)) 
            {
                throw new Exception("steamappsフォルダがありません");
            }
            SteamDefaultAppsDirectory = tmp;
        }

        private void GetGameInstallDirectory()
        {
            // acfがあるフォルダを列挙
            var acfDirPaths = new List<string>
            {
                SteamDefaultAppsDirectory
            };
            var allAcfDirPaths = acfDirPaths.Concat(GetLibDirectoriesFromVdf(SteamDefaultAppsDirectory));

            // 各ディレクトリについて処理
            string gameInstallDirPath = null;
            foreach(var dirPath in allAcfDirPaths)
            {
                gameInstallDirPath = GetGameInstallDir(dirPath, TargetAppId);
                if(gameInstallDirPath == null)
                {
                    continue;
                }
                else
                {
                    break;
                }
            }

            if(gameInstallDirPath == null)
            {
                throw new Exception("ゲームがインストールされていません");
            }

            GameInstallDirectory = gameInstallDirPath;
        }

        private string GetGameInstallDir(string dirPath, int targetAppId)
        {
            // インストールディレクトリにあるsteamapps/appmanifest_[APPID].acfを探す
            var targetAppAcfFile = Path.Combine(dirPath,$"appmanifest_{targetAppId}.acf");

            // なければ終了
            if (!File.Exists(targetAppAcfFile))
            {
                return null;
            }

            // acfファイルにある"installdir" "xxxx"をさがす
            string installDirPattern = "\\s*\"installdir\"\\s+\"(.*)";
            string gameInstallDirName = null;

            using (var fr = new StreamReader(targetAppAcfFile))
            {
                while (fr.EndOfStream == false)
                {
                    foreach (Match m in Regex.Matches(fr.ReadLine(), installDirPattern))
                    {
                        if (m != null)
                        {
                            gameInstallDirName = m.Groups[1].Value.Trim('\"');
                        }
                    }
                }
            }

            if(gameInstallDirName == null)
            {
                throw new Exception("acfファイルが壊れています");
            }

            var gameInstallDirPath = Path.Combine(dirPath, "common", gameInstallDirName);

            if(gameInstallDirPath == null)
            {
                throw new Exception("ゲームディレクトリが存在しません");
            }

            return gameInstallDirPath;
        }

        private List<string> GetLibDirectoriesFromVdf(string steamAppsPath)
        {
            // vdfファイルを探す
            var libraryFoldersVdfFile = Path.Combine(steamAppsPath, "libraryfolders.vdf");

            if (!File.Exists(libraryFoldersVdfFile))
            {
                throw new Exception("vdfファイルがありません");
            }

            // vdfファイルにある"[数字]" "xxxx"をさがす
            var installDirPattern = "\\s*\"[0-9]+\"\\s+\"(.*)";
            var gameLibsPaths = new List<string>();

            using(var fr = new StreamReader(libraryFoldersVdfFile))
            {
                while(fr.EndOfStream == false)
                {
                    foreach (Match m in Regex.Matches(fr.ReadLine(), installDirPattern))
                    {
                        if (m != null)
                        {
                            gameLibsPaths.Add(m.Groups[1].Value.Trim('\"').Replace("\\\\","\\"));
                        }
                    }
                }
            }

            return gameLibsPaths;
        }
    }
}
