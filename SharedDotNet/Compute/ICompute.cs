using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedDotNet.Classifier;
using SharedDotNet.Imaging;

namespace SharedDotNet.Compute
{
    public interface ICompute : IDisposable
    {
        IImageProcessor ImageProcessor { get; }
        IClassifier Classifier { get; }
        IEnumerable<ComputeResult> Compute(IEnumerable<string> imagesPath);
    }

    public class ComputeResult : IDisposable
    {
        public string ImagePath { get; }
        public Bitmap SegmentedCode { get; internal set; }
        public List<Bitmap> Letters { get; internal set; }
        public string PredictedCode { get; internal set; }
        public string PredictedCodeLetters => PredictedCode?.Replace("-", string.Empty);
        internal ComputeResult(string imagePath)
        {
            ImagePath = imagePath;
        }

        public void Dispose()
        {
            SegmentedCode?.Dispose();
            Letters?.ForEach(t => t.Dispose());
        }
    }
}
