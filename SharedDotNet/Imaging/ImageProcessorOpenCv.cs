using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SharedDotNet.Compute;

namespace SharedDotNet.Imaging
{
    public class ImageProcessorOpenCv : IImageProcessor
    {
        [DllImport("CodesReaderNative.dll", EntryPoint = "segment_codes")]
        private static extern void SegmentCodes([MarshalAs(UnmanagedType.LPStr)] string path, [Out] out ArrayStruct result, [In, Out] ref IntPtr code);

        public ComputeResult SegmentCode(string path)
        {
            IntPtr codePtr = IntPtr.Zero;
            var computeResult = new ComputeResult(path);

            SegmentCodes(path, out ArrayStruct result, ref codePtr);

            if (codePtr == IntPtr.Zero)
            {
                return computeResult;
            }

            byte[] codeImageBytes = new byte[result.length];
            Marshal.Copy(codePtr, codeImageBytes, 0, codeImageBytes.Length);
            Marshal.FreeCoTaskMem(codePtr);

            var stream = new MemoryStream(codeImageBytes);
            computeResult.SegmentedCode = (Bitmap)Image.FromStream(stream);

            if (result.array.All(t => t.Width != 0))
            {
                computeResult.Letters = new List<Bitmap>(25);

                var bitmapData = computeResult.SegmentedCode.LockBits(new Rectangle(0, 0, computeResult.SegmentedCode.Width, computeResult.SegmentedCode.Height), 
                    ImageLockMode.ReadOnly, computeResult.SegmentedCode.PixelFormat);

                foreach (var codeRect in result.array)
                {
                    computeResult.Letters.Add(CropImage(bitmapData, computeResult.SegmentedCode.Palette, codeRect.Left, codeRect.Top, codeRect.Left + codeRect.Width, codeRect.Top + codeRect.Height));
                }

                computeResult.SegmentedCode.UnlockBits(bitmapData);
            }

            return computeResult;
        }
        static Bitmap CropImage(BitmapData sourceImage, ColorPalette palette, int xl, int yl, int xr, int yr)
        {
            const int sizeX = 30, sizeY = 35;
            if (xr - xl > sizeX || yr - yl > sizeY)
                return null;

            Bitmap dst = new Bitmap(sizeX, sizeY, PixelFormat.Format8bppIndexed);
            dst.Palette = palette;
            var bitmapData = dst.LockBits(new Rectangle(0, 0, dst.Width, dst.Height),
                ImageLockMode.ReadWrite, dst.PixelFormat);

            int totalWhiteHorizontal = sizeX - xr + xl - 1;
            int totalLeft = (int)(0.4 * totalWhiteHorizontal);

            int totalWhiteVertical = sizeY - (yr - yl + 1);
            int totalTop = (int)(0.2 * totalWhiteVertical);

            for (int y = 0; y < dst.Height; y++)
                for (int x = 0; x < dst.Width; x++)
                    SetIndexedPixel(bitmapData, x, y, 255);

            for (int x = xl; x < xr; x++)
                for (int y = yl; y < yr; y++)
                    SetIndexedPixel(bitmapData, x - xl + totalLeft, y - yl + totalTop, GetIndexedPixel(sourceImage, x, y));

            dst.UnlockBits(bitmapData);
            return dst;
        }
        public static unsafe byte GetIndexedPixel(BitmapData bitmap, int x, int y)
        {
            return *((byte*) bitmap.Scan0 + y * bitmap.Stride + x);
        }

        public static unsafe void SetIndexedPixel(BitmapData bitmap, int x, int y, byte color)
        {
            *((byte*) bitmap.Scan0 + y * bitmap.Stride + x) = color;
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

        public void Dispose()
        {
        }
    }
}
