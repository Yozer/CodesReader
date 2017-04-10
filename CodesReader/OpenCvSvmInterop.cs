using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharedDotNet.Imaging;

namespace CodesReader
{
    public class OpenCvSvmInterop : IDisposable
    {
        private readonly IImageProcessor _imageProcessor;

        [DllImport("CodesReaderNative.dll", EntryPoint = "init")]
        private static extern void Init([MarshalAs(UnmanagedType.LPStr)] string path);
        [DllImport("CodesReaderNative.dll", EntryPoint = "release")]
        private static extern void Release();
        [DllImport("CodesReaderNative.dll", EntryPoint = "predict")]
        private static extern unsafe char Predict(float* data);

        public OpenCvSvmInterop(IImageProcessor imageProcessor, string modelPath)
        {
            _imageProcessor = imageProcessor;
            Init(modelPath);
        }

        public unsafe string Recognize(string imagePath)
        {
            int all = 0, c = 0;
            foreach (var file in Directory.EnumerateFiles(@"D:\grzego"))
            {
                Bitmap bmp = (Bitmap) Image.FromFile(file);
                char correct = Path.GetFileNameWithoutExtension(file)[0];
                fixed (float* ptr = GetBitmapData(bmp))
                {
                    char answer = Predict(ptr);
                    if (correct == answer)
                        ++c;
                }

                ++all;
            }

            float succ = (float) c / all;
            File.WriteAllText("res.txt", succ.ToString());
            //Bitmap bmp = (Bitmap) Image.FromFile("C:\\input_letters\\2_0.bmp");
            //fixed (float* a = GetBitmapData(bmp))
            //{
            //    char answer = Predict(a);
            //}

            List<Bitmap> data = _imageProcessor.SegmentCode(imagePath);
            string result = string.Empty;

            foreach (Bitmap bitmap in data.Skip(1))
            {
                fixed (float* ptr = GetBitmapData(bitmap))
                {
                    char answer = Predict(ptr);
                    result += answer;
                }
            }

            return result;
        }

        private unsafe float[] GetBitmapData(Bitmap bitmap)
        {
            var data = new float[30 * 35];
            int i = 0;
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            byte* ptr = (byte*) bitmapData.Scan0;
            for (int y = 0; y < bitmap.Height; ++y)
            for (int x = 0; x < bitmap.Width; ++x)
                data[i++] = ptr[y * bitmapData.Stride + x];


            bitmap.UnlockBits(bitmapData);
            return data;
        }

        public void Dispose()
        {
            Release();
        }
    }
}
