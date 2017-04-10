using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using SharedDotNet.Imaging;

namespace SharedDotNet.Classifier
{
    public abstract class ClassifierBase : IClassifier
    {
        private readonly IImageProcessor _imageProcessor;
        private readonly object _locker = new object();

        protected ClassifierBase(IImageProcessor imageProcessor)
        {
            _imageProcessor = imageProcessor;
        }

        protected abstract char[] Classify(IEnumerable<Bitmap> input);
        public abstract void Dispose();
        public string Recognize(string imagePath)
        {
            List<Bitmap> data = _imageProcessor.SegmentCode(imagePath);
            if (data == null)
                return string.Empty;

            char[] result = Classify(data.Skip(1));
            return string.Join("-", result.Batch(5).Select(t => new string(t.ToArray())).ToArray());
        }
        protected unsafe float[] GetBitmapData(Bitmap bitmap)
        {
            var data = new float[30 * 35];
            int i = 0;
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

            byte* ptr = (byte*)bitmapData.Scan0;
            for (int y = 0; y < bitmap.Height; ++y)
            for (int x = 0; x < bitmap.Width; ++x)
                data[i++] = ptr[y * bitmapData.Stride + x] / 255.0f;


            bitmap.UnlockBits(bitmapData);
            return data;
        }
    }
}
