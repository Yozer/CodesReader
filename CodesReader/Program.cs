using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodesReader.Imaging;

namespace CodesReader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Test();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static void Test()
        {
            IImageProcessor processor = new ImageProcessor();

            foreach (var file in Directory.EnumerateFiles(@"D:\dataset\easy\read"))
            {
                string code = Path.GetFileName(file);
                
                Bitmap bitmap = (Bitmap)Image.FromFile(file);
                Bitmap result = processor.SegmentCode(bitmap);
                result.Save(Path.Combine(@"D:\dataset\easy\second_try_c_sharp\read_and_segmented", code));

            }
        }
    }
}
