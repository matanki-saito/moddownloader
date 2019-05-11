using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace claes
{
    public class Cache
    {
        public Cache()
        {
            this.pack_md5 = "";
            this.file_list = new List<FileInfo>();
        }

        public string pack_md5 { get; set; }
        public List<FileInfo> file_list { get; set; }


    }

    public class FileInfo
    {
        public string path { get; set; }
        public string md5 { get; set; }
        public DateTime last_update { get; set; }
    }
}
