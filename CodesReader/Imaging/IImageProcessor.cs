using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;

namespace CodesReader.Imaging
{
    public interface IImageProcessor
    {
        List<Bitmap> SegmentCode(string path);
    }
}
