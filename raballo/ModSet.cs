using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace claes
{
    public class ModSet
    {        
        public string name { get; set; }
        public string s_id { get; set; }
        public string file { get; set; }
        public List<RequiredMod> required { get; set; }
    }
    public class RequiredMod
    {
        public string name { get; set; }
        public string type { get; set; }
        public int s_id { get; set; }
        public int t_id { get; set; }
        public string file { get; set; }
    }
}
