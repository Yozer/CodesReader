using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedDotNet.Classifier;
using SharedDotNet.Compute;
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
            //Test();
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
            //SegmentAndSplitOneCode(@"D:\dataset\easy\read\T6GWD-TW9FQ-QQH3T-Q7MVJ-CF2V8.jpg");
            TryNeuralNetwork();
            //PrepareDataForGrzego();
        }

        private static void PrepareDataForGrzego()
        {
            // validation set
            IImageProcessor processor = new ImageProcessorOpenCv();
            var skip = new HashSet<string>(Directory.EnumerateFiles(@"C:\grzego\input").Select(Path.GetFileNameWithoutExtension));
            var enu = new List<string>(Directory.EnumerateFiles(@"C:\grzego\read")).Where(t => !skip.Contains(Path.GetFileNameWithoutExtension(t))).ToList();
            var dic = new Dictionary<char, int>();
            Shuffle(enu);

            Parallel.ForEach(enu.Take(500), new ParallelOptions { MaxDegreeOfParallelism = 1 }, file =>
            {
                using (var result = processor.SegmentCode(file))
                {
                    string code = Path.GetFileNameWithoutExtension(file).Replace("-", "");

                    if (result.SegmentedCode != null)
                    {
                        if (result.Letters != null)
                        {
                            for (int i = 0; i < 25; ++i)
                            {
                                if (!dic.ContainsKey(code[i]))
                                    dic.Add(code[i], 0);

                                result.Letters[i].Save(@"C:\grzego\validation_set\" + code[i] + $"_{dic[code[i]]}.bmp", ImageFormat.Bmp);
                                ++dic[code[i]];
                            }
                        }
                    }
                }
            });

            dic = new Dictionary<char, int>();
            Parallel.ForEach(Directory.EnumerateFiles(@"C:\grzego\input"), new ParallelOptions { MaxDegreeOfParallelism = 1 }, file =>
            {
                using (var result = processor.SegmentCode(file))
                {
                    string code = Path.GetFileNameWithoutExtension(file).Replace("-", "");

                    if (result.SegmentedCode != null)
                    {
                        if (result.Letters != null)
                        {
                            for (int i = 0; i < 25; ++i)
                            {
                                if (!dic.ContainsKey(code[i]))
                                    dic.Add(code[i], 0);

                                result.Letters[i].Save(@"C:\grzego\training_set\" + code[i] + $"_{dic[code[i]]}.bmp", ImageFormat.Bmp);
                                ++dic[code[i]];
                            }
                        }
                    }
                }
            });
        }
        private static void TryNeuralNetwork()
        {
            Directory.EnumerateFiles(@"D:\dataset\easy\wrong_segmentation").ToList().ForEach(File.Delete);
            Directory.EnumerateFiles(@"D:\dataset\easy\wrong_segmentation_whole").ToList().ForEach(File.Delete);
            var dictionary = new Dictionary<char, int>();
            int counter = 0, failed = 0, failed_letters = 0;
            IClassifier classifier = new NnLetterClassifier("summary/experiment-16/models/model");
            //IClassifier classifier = new RNNClassifier("summary/seq/model");
            //IClassifier classifier = new SVMClassifier(@"C:\Users\domin\Documents\Visual Studio 2017\Projects\CodesReader\OpenCvSVM\best.yaml");
            //classifier.Recognize(new List<ComputeResult> { new ComputeResult("") { Letters = new List<Bitmap> { (Bitmap)Image.FromFile("D:\\test.bmp") } } });

            using (var compute = new ParallelCompute(new ImageProcessorOpenCv(), classifier))
            {
                Console.Clear();

                foreach (var computeResult in compute.Compute(Directory.EnumerateFiles(@"C:\grzego\read")))
                {
                    Console.SetCursorPosition(0, 0);
                    ++counter;
                    string fileName = Path.GetFileNameWithoutExtension(computeResult.ImagePath);
                    string correctCode = Path.GetFileNameWithoutExtension(computeResult.ImagePath).Replace("-", string.Empty);
                    if (computeResult.PredictedCodeLetters == null || correctCode != computeResult.PredictedCodeLetters)
                        ++failed;

                    if (computeResult.PredictedCodeLetters != null && correctCode != computeResult.PredictedCodeLetters)
                    {
                        File.Copy(computeResult.ImagePath, @"D:\dataset\easy\wrong_segmentation_whole\" + fileName + ".jpg", true);
                        for (int i = 0; i < 25; ++i)
                        {
                            if (correctCode[i] != computeResult.PredictedCodeLetters[i])
                            {
                                if (!dictionary.ContainsKey(correctCode[i]))
                                    dictionary.Add(correctCode[i], 0);

                                computeResult.Letters[i].Save(@"D:\dataset\easy\wrong_segmentation\" +
                                    $"{correctCode[i]}_predicted={computeResult.PredictedCodeLetters[i]}_{fileName}_{dictionary[correctCode[i]]}.bmp", ImageFormat.Bmp);

                                ++failed_letters;
                                ++dictionary[correctCode[i]];
                            }
                        }
                    }

                    Console.WriteLine($"Total: {counter} Bad: {failed} Failed letters: {failed_letters}");
                    computeResult.Dispose();
                }
            }

            //using (var nn = new NnLetterClassifier(new ImageProcessorOpenCv(), "summary/experiment-12/models/model-7805"))
            //{
            //    Parallel.ForEach(Directory.EnumerateFiles(@"D:\dataset\easy\read"), new ParallelOptions { MaxDegreeOfParallelism = 50 }, file =>
            //     {
            //         string code = Path.GetFileNameWithoutExtension(file);
            //         string predicted = nn.Recognize(file);
            //         if (predicted != code)
            //         {
            //             //File.Copy(file, @"D:\dataset\easy\wrong_segmentation\" + Path.GetFileName(file), true);
            //             Interlocked.Increment(ref failed);
            //         }

            //         if (counter % 100 == 0)
            //         {
            //             lock (nn)
            //             {
            //                 Console.WriteLine($"{failed}/{counter++}");
            //             }
            //         }
            //         Interlocked.Increment(ref counter);
            //     });
            //}
        }

        private static void TryTestSet()
        {
            //int all = 0, c = 0;
            //foreach (var file in Directory.EnumerateFiles(@"D:\grzego"))
            //{
            //    Bitmap bmp = (Bitmap)Image.FromFile(file);
            //    char correct = Path.GetFileNameWithoutExtension(file)[0];
            //    fixed (float* ptr = GetBitmapData(bmp))
            //    {
            //        char answer = Predict(ptr);
            //        if (correct == answer)
            //            ++c;
            //    }

            //    ++all;
            //}

            //float succ = (float)c / all;
        }

        private static void SegmentAndSplitOneCode(string path, string target = @"D:\tmp")
        {
            IImageProcessor processor = new ImageProcessorOpenCv();

            var result = processor.SegmentCode(path);
            string x = Path.GetFileNameWithoutExtension(path).Replace("-", string.Empty);
            result.SegmentedCode.Save(Path.Combine(target, "test.bmp"), ImageFormat.Bmp);

            for (int i = 0; i < x.Length; ++i)
            {
                result.Letters[i].Save(Path.Combine(target, x[i] + ".bmp"), ImageFormat.Bmp);
            }
        }

        private static void SegmentCodes()
        {
            IImageProcessor processor = new ImageProcessorOpenCv();

            Directory.EnumerateFiles(@"D:\dataset\easy\second_try_c_sharp\not_splited").ToList().ForEach(File.Delete);
            Directory.EnumerateFiles(@"D:\dataset\easy\second_try_c_sharp\not_segmented").ToList().ForEach(File.Delete);
            Directory.EnumerateFiles(@"D:\dataset\easy\second_try_c_sharp\read_and_segmented").ToList().ForEach(File.Delete);


            Parallel.ForEach(Directory.EnumerateFiles(@"D:\dataset\easy\read"), new ParallelOptions { MaxDegreeOfParallelism = 8 }, file =>
              {
                  using (var result = processor.SegmentCode(file))
                  {
                      if (result.SegmentedCode == null)
                      {
                          File.Copy(file, @"D:\dataset\easy\second_try_c_sharp\not_segmented\" + Path.GetFileName(file), true);
                      }
                      else
                      {
                          result.SegmentedCode.Save(@"D:\dataset\easy\second_try_c_sharp\read_and_segmented\" + Path.GetFileName(file), ImageFormat.Jpeg);

                          if (result.Letters == null)
                          {
                              File.Copy(file, @"D:\dataset\easy\second_try_c_sharp\not_splited\" + Path.GetFileName(file), true);
                          }
                      }
                  }
              });
        }
    }
}
