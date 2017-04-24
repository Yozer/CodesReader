using System;
using System.Collections.Generic;
using System.Drawing;
using Python.Runtime;

namespace SharedDotNet.Classifier
{
    public class NnLetterClassifier : ClassifierBase
    {
        public override int BufferSize { get; } = 50;
        private readonly dynamic _model;

        public NnLetterClassifier(string modelPath) 
        {
            PythonEngine.Initialize();
            _model = PythonEngine.ImportModule("run_model");
            _model.load_model(new PyString(modelPath));
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

                return (char[])_model.predict(new PyLong(new IntPtr(ptr).ToInt64()), new PyInt(size));
            }
        }

        public override void Dispose()
        {
            //_model.deinit();
            PythonEngine.Shutdown();
        }
    }
}
