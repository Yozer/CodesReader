using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace CodesReader.Imaging
{
    class ImageProcessorOpenCv : IImageProcessor
    {
        [DllImport("CodesReaderNative.dll", EntryPoint = "segment_codes")]
        private static extern void SegmentCodes([MarshalAs(UnmanagedType.LPStr)] string path, [Out] out ArrayStruct result, [In, Out] ref IntPtr code);

        public List<Bitmap> SegmentCode(string path)
        {
            IntPtr codePtr = IntPtr.Zero;
            SegmentCodes(path, out ArrayStruct result, ref codePtr);

            if (codePtr == IntPtr.Zero)
            {
                return null;
            }

            byte[] codeImageBytes = new byte[result.length];
            Marshal.Copy(codePtr, codeImageBytes, 0, codeImageBytes.Length);
            Marshal.FreeCoTaskMem(codePtr);

            MemoryStream stream = new MemoryStream(codeImageBytes);
            var segmentedResult = new List<Bitmap> {(Bitmap) Image.FromStream(stream)};

            if (result.array.All(t => t.Width != 0))
            {
                foreach (var codeRect in result.array)
                {
                    segmentedResult.Add(CropImage(segmentedResult[0], codeRect));
                }
            }

            return segmentedResult;
        }

        private static Bitmap CropImage(Bitmap img, CodeRect cropArea)
        {
            return img.Clone(new Rectangle(cropArea.Left, cropArea.Top, cropArea.Width, cropArea.Height), img.PixelFormat);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ArrayStruct
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            public CodeRect[] array;
            public int length;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CodeRect
        {
            public int Left, Top, Width, Height;
        }
    }
}
