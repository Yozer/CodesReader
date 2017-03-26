using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodesReader.Imaging
{
    public interface IImageProcessor
    {
        Bitmap SegmentCode(Bitmap image);
    }
}
