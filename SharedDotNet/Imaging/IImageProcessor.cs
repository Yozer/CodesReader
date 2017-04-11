using System;
using System.Collections.Generic;
using System.Drawing;
using SharedDotNet.Compute;

namespace SharedDotNet.Imaging
{
    public interface IImageProcessor : IDisposable
    {
        ComputeResult SegmentCode(string path);
    }
}
