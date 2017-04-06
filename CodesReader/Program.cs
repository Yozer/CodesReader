using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            Random rnd = new Random();
            while (n > 1)
            {
                int k = (rnd.Next(0, n) % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        private static void Test()
        {
            IImageProcessor processor = new ImageProcessorOpenCv();

            //string path = @"D:\B2MMQ-3K3VW-PCCDM-99DBQ-WQRCB.jpg";
            //processor.SegmentCode(path);
            //using (var result = processor.SegmentCode(path))
            //{
            //    result.Mat.Bitmap.Save(@"D:\tmp.bmp", ImageFormat.Bmp);
            //}


            //foreach (var file in Directory.EnumerateFiles(@"D:\dataset\easy\read"))
            //Directory.EnumerateFiles(@"D:\dataset\easy\second_try_c_sharp\not_splited").ToList().ForEach(File.Delete);
            var dic = new Dictionary<char, int>();

            Parallel.ForEach(Directory.EnumerateFiles(@"D:\dataset\easy\SVM\input"), new ParallelOptions { MaxDegreeOfParallelism = 1 }, file =>
            {
                var list = processor.SegmentCode(file);
                string code = Path.GetFileNameWithoutExtension(file).Replace("-", "");

                if (list == null)
                {
                    //File.Copy(file, @"D:\dataset\easy\second_try_c_sharp\not_segmented\" + Path.GetFileName(file), true);
                }
                else
                {
                    //list[0].Save(@"D:\dataset\easy\second_try_c_sharp\read_and_segmented\" + Path.GetFileName(file), ImageFormat.Jpeg);

                    //if (list.Count != 26)
                    //{
                    //    File.Copy(file, @"D:\dataset\easy\second_try_c_sharp\not_splited\" + Path.GetFileName(file), true);
                    //}

                    if (list.Count == 26)
                    {
                        for (int i = 1; i < 26; ++i)
                        {
                            if(!dic.ContainsKey(code[i - 1]))
                                dic.Add(code[i-1], 0);

                            list[i].Save(@"D:\dataset\easy\SVM\input_letters\" + code[i - 1] + $"_{dic[code[i - 1]]}.bmp", ImageFormat.Bmp);
                            ++dic[code[i - 1]];
                        }
                    }

                    list.ForEach(t => t.Dispose());
                }
            });


            //foreach (var file in Directory.EnumerateFiles(@"D:\dataset\easy\read"))
            //Parallel.ForEach(Directory.EnumerateFiles(@"D:\dataset\easy\read"), new ParallelOptions {MaxDegreeOfParallelism = 8}, file =>
            //{
            //    string code = Path.GetFileName(file);
            //    using (var result = processor.SegmentCode(file))
            //    {
            //        result.Mat.Bitmap.Save(Path.Combine(@"D:\dataset\easy\second_try_c_sharp\read_and_segmented", code));
            //    }
            //});
        }
    }
}
