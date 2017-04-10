using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MoreLinq;
using SharedDotNet.Classifier;
using SharedDotNet.Imaging;

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
            //int counter = 0, failed = 0;
            //using (var nn = new NnLetterClassifier(new ImageProcessorOpenCv(), "summary/experiment-11/models/model-7805"))
            //{
            //    Parallel.ForEach(Directory.EnumerateFiles(@"D:\dataset\easy\read"), new ParallelOptions { MaxDegreeOfParallelism = 8}, file =>
            //    {
            //        string code = Path.GetFileNameWithoutExtension(file);
            //        string predicted = nn.Recognize(file);
            //        if (predicted != code)
            //        {
            //            File.Copy(file, @"D:\dataset\easy\wrong_segmentation\" + Path.GetFileName(file), true);
            //            Interlocked.Increment(ref failed);
            //        }

            //        if (counter % 100 == 0)
            //        {
            //            lock (nn)
            //            {
            //                Console.WriteLine($"{failed}/{counter++}");
            //            }
            //        }
            //        Interlocked.Increment(ref counter);
            //    });

            //}
            //return;
            IImageProcessor processor = new ImageProcessorOpenCv();

            string path = @"D:\dataset\easy\wrong_segmentation\B4JR9-B7WYC-2BPKY-JFVQB-C87B6.jpg";
            var images = processor.SegmentCode(path);
            string x = Path.GetFileNameWithoutExtension(path).Replace("-", string.Empty);
            images[0].Save("D:\\tmp\\test.bmp", ImageFormat.Bmp);

            for (int i = 0; i < x.Length; ++i)
            {
                images[i + 1].Save(@"D:\tmp\" + x[i] + ".bmp", ImageFormat.Bmp);
            }
            return;

            //int all = 0, c = 0;
            //foreach (var file in Directory.EnumerateFiles(@"D:\grzego"))
            //{
            //    Bitmap bmp = (Bitmap) Image.FromFile(file);
            //    char correct = Path.GetFileNameWithoutExtension(file)[0];
            //    fixed (float* ptr = GetBitmapData(bmp))
            //    {
            //        char answer = Predict(ptr);
            //        if (correct == answer)
            //            ++c;
            //    }

            //    ++all;
            //}

            //float succ = (float) c / all;
            //File.WriteAllText("res.txt", succ.ToString());



            //foreach (var file in Directory.EnumerateFiles(@"D:\dataset\easy\read"))
            //Directory.EnumerateFiles(@"D:\dataset\easy\second_try_c_sharp\not_splited").ToList().ForEach(File.Delete);
            //var skip = new HashSet<string>(Directory.EnumerateFiles(@"D:\dataset\easy\SVM\input").Select(Path.GetFileNameWithoutExtension));
            //var enu = new List<string>(Directory.EnumerateFiles(@"D:\dataset\easy\read")).Where(t => !skip.Contains(Path.GetFileNameWithoutExtension(t))).ToList();
            //Shuffle(enu);
            //var dic = new Dictionary<char, int>();

            //Parallel.ForEach(enu.Take(500), new ParallelOptions { MaxDegreeOfParallelism = 1 }, file =>
            //{
            //    var list = processor.SegmentCode(file);
            //    string code = Path.GetFileNameWithoutExtension(file).Replace("-", "");

            //    if (list == null)
            //    {
            //        //File.Copy(file, @"D:\dataset\easy\second_try_c_sharp\not_segmented\" + Path.GetFileName(file), true);
            //    }
            //    else
            //    {
            //        //list[0].Save(@"D:\dataset\easy\second_try_c_sharp\read_and_segmented\" + Path.GetFileName(file), ImageFormat.Jpeg);

            //        //if (list.Count != 26)
            //        //{
            //        //    File.Copy(file, @"D:\dataset\easy\second_try_c_sharp\not_splited\" + Path.GetFileName(file), true);
            //        //}

            //        if (list.Count == 26)
            //        {
            //            for (int i = 1; i < 26; ++i)
            //            {
            //                if(!dic.ContainsKey(code[i - 1]))
            //                    dic.Add(code[i-1], 0);

            //                list[i].Save(@"D:\grzego\" + code[i - 1] + $"_{dic[code[i - 1]]}.bmp", ImageFormat.Bmp);
            //                ++dic[code[i - 1]];
            //            }
            //        }

            //        list.ForEach(t => t.Dispose());
            //    }
            //});


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
