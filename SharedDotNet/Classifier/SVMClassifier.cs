using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SharedDotNet.Classifier
{
    public class SVMClassifier : ClassifierBase
    {
        [DllImport("CodesReaderNative.dll", EntryPoint = "init")]
        private static extern void Init([MarshalAs(UnmanagedType.LPStr)] string path);
        [DllImport("CodesReaderNative.dll", EntryPoint = "release")]
        private static extern void Release();
        [DllImport("CodesReaderNative.dll", EntryPoint = "predict")]
        private static extern unsafe void Predict(float* data, int count, char* result);

        public SVMClassifier(string modelPath)
        {
            Init(modelPath);
        }

        protected override unsafe char[] Classify(List<Bitmap> input)
        {
            int imgSize = input[0].Width * input[0].Height;
            int size = input.Count * imgSize;
            fixed (float* ptr = new float[size])
            {
                int index = 0;
                foreach (var bitmap in input)
                {
                    GetBitmapData(bitmap, ptr + index);
                    index += imgSize;
                }

                var result = new char[input.Count];
                fixed (char* resPtr = result)
                {
                    Predict(ptr, result.Length, resPtr);
                }

                return result;
            }
        }

        public override void Dispose()
        {
            Release();
        }
    }
}
