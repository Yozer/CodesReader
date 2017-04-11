using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedDotNet.Compute;

namespace SharedDotNet.Classifier
{
    public interface IClassifier : IDisposable
    {
        void Recognize(List<ComputeResult> imagePath);
    }
}
