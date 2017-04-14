using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using SharedDotNet.Compute;

namespace SharedDotNet.Classifier
{
    public abstract class ClassifierBase : IClassifier
    {
        protected abstract char[] Classify(List<Bitmap> input);
        public abstract void Dispose();
        public void Recognize(List<ComputeResult> jobs)
        {
            char[] result = Classify(jobs.SelectMany(t => t.Letters).ToList());

            var builder = new StringBuilder(25);
            for (var i = 0; i < jobs.Count; i++)
            {
                var computeResult = jobs[i];
                builder.Clear();

                for (int j = 0; j < 25; ++j)
                {
                    builder.Append(result[i * 25 + j]);
                    if ((j + 1) % 5 == 0)
                    {
                        builder.Append('-');
                    }
                }

                --builder.Length;
                computeResult.PredictedCode = builder.ToString();
            }
        }
        protected unsafe float[] GetBitmapData(Bitmap bitmap)
        {
            var data = new float[bitmap.Width * bitmap.Height];
            int i = 0;
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            byte* ptr = (byte*)bitmapData.Scan0;
            for (int y = 0; y < bitmap.Height; ++y)
            for (int x = 0; x < bitmap.Width; ++x)
                data[i++] = ptr[y * bitmapData.Stride + x];


            bitmap.UnlockBits(bitmapData);
            return data;
        }

        protected unsafe void GetBitmapData(Bitmap bitmap, float* dst)
        {
            int i = 0;
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            byte* ptr = (byte*)bitmapData.Scan0;
            for (int y = 0; y < bitmap.Height; ++y)
            for (int x = 0; x < bitmap.Width; ++x)
                dst[i++] = ptr[y * bitmapData.Stride + x];


            bitmap.UnlockBits(bitmapData);
        }
    }
}
