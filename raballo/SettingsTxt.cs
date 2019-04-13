using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace claes
{
    class SettingsTxt
    {
        private static readonly string rtncode = "\r\n";

        private string filePath;
        public List<string> ActiveMods { get; set; }
        private string prePart = null;
        private string postPart = null;

        public SettingsTxt(string path)
        {
            filePath = path;
            ActiveMods = new List<string>();
        }

        public void Update()
        {
            var strechMods = new List<string>();
            foreach(var fileName in ActiveMods)
            {
                strechMods.Add($"\t\"{fileName}\"");
            }

            var mainPart = string.Join(rtncode, strechMods);
            using(var fw = new StreamWriter(filePath))
            {
                fw.Write($"{prePart}{rtncode}{mainPart}{rtncode}{postPart}{rtncode}");
            }
        }

        public void Load()
        {
            var buf = new List<string>();
            string all;
            string mainPart = null;

            using (var fr = new StreamReader(filePath))
            {
                all = fr.ReadToEnd();
            }
            string[] del = { rtncode };
            foreach (var l in all.Split(del, StringSplitOptions.None))
            {
                buf.Add(l);
                if (l.Equals("last_mods={"))
                {
                    prePart = string.Join(rtncode, buf);
                    buf.Clear();
                    continue;
                }

                if (mainPart == null && prePart != null && l.Equals("}"))
                {
                    mainPart = string.Join(rtncode, buf);
                    buf.Clear();
                    postPart = l + rtncode; // }を入れる
                }
            }
            postPart += string.Join(rtncode, buf);

            foreach (Match m in Regex.Matches(mainPart, "\"([^\"]+)\""))
            {
                ActiveMods.Add(Path.GetFileName(m.Groups[1].Value));
            }
        }
    }
}
