using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SharedDotNet.Imaging;

namespace SharedDotNet.Classifier
{
    public class OpenCvSvmInterop : ClassifierBase
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

        protected override unsafe char[] Classify(List<Bitmap> input)
        {
            return input
                    .Select(t =>
                    {
                        fixed (float* ptr = GetBitmapData(t))
                        {
                            return Predict(ptr);
                        }
                    })
                    .ToArray();

        }

        public override void Dispose()
        {
            Release();
        }
    }
}
