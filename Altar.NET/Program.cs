using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Altar
{
    // http://pastebin.com/9t783UNE

    using static SR;

    static class Program
    {
        readonly static string[] dirs =
        {
            "texture",
            "texpage",
            "sprite" ,
            "audio"  ,
            "sound"  ,
            "room"   ,
            "object" ,
            "bg"     ,
            "script" ,
            "code"   ,
            "font"   ,
            "path"
        };

        [STAThread]
        unsafe static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture =
                CultureInfo.InvariantCulture;

            var file = Path.GetFullPath(args.Length == 0 ? DATA_WIN : args[0]);

            if (Directory.Exists(file) && !File.Exists(file))
                file += Path.DirectorySeparatorChar + DATA_WIN;

            if (!File.Exists(file))
            {
                Console.WriteLine(ERR_FILE_NF_1 + file + ERR_FILE_NF_2);
                return;
            }

            var cd = Path.GetFullPath(Environment.CurrentDirectory);
            Environment.CurrentDirectory = Path.GetDirectoryName(file);

            foreach (var s in dirs)
                if (!Directory.Exists(s))
                    Directory.CreateDirectory(s);

            using (var f = GMFile.GetFile(File.ReadAllBytes(file)))
            {

            }

            Environment.CurrentDirectory = cd;
        }
    }
}
