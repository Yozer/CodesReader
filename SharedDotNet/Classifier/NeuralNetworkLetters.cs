using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Python.Runtime;
using SharedDotNet.Imaging;

namespace SharedDotNet.Classifier
{
    public class NnLetterClassifier : ClassifierBase
    {
        //private readonly Py.GILState _pyGIL;
        private readonly dynamic _model;

        private readonly IntPtr _threadPtr;

        public NnLetterClassifier(IImageProcessor imageProcessor, string modelPath) 
            : base(imageProcessor)
        {
            //_pyGIL = Py.GIL();
            PythonEngine.Initialize();
            _model = PythonEngine.ImportModule("run_model");
            _model.load_model(new PyString(modelPath));

            _threadPtr = PythonEngine.BeginAllowThreads();
        }

        protected override char[] Classify(IEnumerable<Bitmap> input)
        {
            using (Py.GIL())
            {
                using (PyObject data = new PyList(input.Select(t => ConvertToPyList(GetBitmapData(t))).ToArray()))
                {
                    return (char[])_model.predict(data);
                }
            }
        }

        private PyObject ConvertToPyList(float[] data)
        {
            return new PyList(data.Select(t => (PyObject)new PyFloat(t)).ToArray());
        }

        public override void Dispose()
        {
            PythonEngine.EndAllowThreads(_threadPtr);
            _model.deinit();
            PythonEngine.Shutdown();
            //_pyGIL?.Dispose();
        }
    }
}
