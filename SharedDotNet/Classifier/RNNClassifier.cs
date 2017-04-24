using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using Python.Runtime;
using SharedDotNet.Compute;

namespace SharedDotNet.Classifier
{
    public class RNNClassifier : ClassifierBase
    {
        public override int BufferSize { get; } = 25;
        private readonly dynamic _model;
        public RNNClassifier(string modelPath)
        {
            PythonEngine.Initialize();
            _model = PythonEngine.ImportModule("run_model_seq");
            _model.load_model(new PyString(modelPath));
        }
        public override void Dispose()
        {
            PythonEngine.Shutdown();
        }

        public override void Recognize(List<ComputeResult> jobs)
        {
            char[] result = Classify(jobs.Select(t => t.SegmentedCode).ToList());

            for (var i = 0; i < jobs.Count; i++)
            {
                var computeResult = jobs[i];
                computeResult.PredictedCode = new string(result.Skip(i * 29).Take(29).ToArray());
            }
        }
        protected new unsafe void GetBitmapData(Bitmap bitmap, float* dst)
        {
            int i = 0;
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            byte* ptr = (byte*)bitmapData.Scan0;
            for (int y = 0; y < 32; ++y)
                for (int x = 0; x < 475; ++x)
                    dst[i++] = y >= bitmap.Height || x >= bitmap.Width ? 255 : ptr[y * bitmapData.Stride + x];


            bitmap.UnlockBits(bitmapData);
        }
        //public static Bitmap ResizeImage(Image image, int width = 475, int height = 32)
        //{
        //    var destRect = new Rectangle(0, 0, width, height);
        //    var destImage = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

        //    //destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        //    using (var graphics = Graphics.FromImage(destImage))
        //    {
        //        graphics.CompositingMode = CompositingMode.SourceCopy;
        //        graphics.CompositingQuality = CompositingQuality.HighQuality;
        //        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        //        graphics.SmoothingMode = SmoothingMode.HighQuality;
        //        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        //        using (var wrapMode = new ImageAttributes())
        //        {
        //            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
        //            graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
        //        }
        //    }

        //    destImage.Save(@"D:\test.bmp", ImageFormat.Bmp);
        //    return destImage;
        //}

        protected override unsafe char[] Classify(List<Bitmap> list)
        {
            int imgSize = 475 * 32;
            int size = list.Count * imgSize;
            fixed (float* ptr = new float[size])
            {
                int index = 0;
                foreach (var bitmap in list)
                {
                    GetBitmapData(bitmap, ptr + index);
                    index += imgSize;
                    bitmap.Dispose();
                }

                return (char[])_model.predict(new PyLong(new IntPtr(ptr).ToInt64()), new PyInt(size));
            }
        }
    }
}
