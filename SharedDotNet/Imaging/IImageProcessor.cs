using System.Collections.Generic;
using System.Drawing;

namespace SharedDotNet.Imaging
{
    public interface IImageProcessor
    {
        List<Bitmap> SegmentCode(string path);
    }
}
