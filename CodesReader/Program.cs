using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodesReader.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Cuda;

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

        private static void Test()
        {
            //Mat img = CvInvoke.Imread("D:\\B2MMQ-3K3VW-PCCDM-99DBQ-WQRCB.jpg", ImreadModes.Grayscale);
            //GpuMat dst = new GpuMat(), src = new GpuMat();
            //src.Upload(img);

            //CudaInvoke.Threshold(src, dst, 128.0, 255.0, ThresholdType.Binary);

            //Mat resultHost = new Mat();
            //dst.Download(resultHost);

            //resultHost.Save("D:\\out.png");


            //dst.Dispose();
            //src.Dispose();
            //img.Dispose();
            //resultHost.Dispose();

            IImageProcessor processor = new ImageProcessorCuda();

            //string path = @"D:\B2MMQ-3K3VW-PCCDM-99DBQ-WQRCB.jpg";
            //using (var result = processor.SegmentCode(path))
            //{
            //    result.Mat.Bitmap.Save(@"D:\tmp.bmp", ImageFormat.Bmp);
            //}


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
