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
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO.Compression;
using System.Reflection;

namespace claes
{
    internal static class Entry
    {
        [STAThread]
        private static void Main()
        {
            Form modSetChanger = new ModSetChangerForm();

            Application.Run(modSetChanger);
        }
    }
}