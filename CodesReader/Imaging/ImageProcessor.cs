using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Accord.Imaging;
using Accord.Imaging.Filters;
using MoreLinq;

namespace CodesReader.Imaging
{
    public class ImageProcessor : IImageProcessor
    {
        private static readonly Grayscale GrayScaleFilter = new Grayscale(0.2989, 0.5870, 0.1140);
        private static readonly RotateBilinear RotateFilter = new RotateBilinear(90, false);
        private static readonly Threshold ThresholdFilter204 = new Threshold(204);
        private static readonly Invert InvertFilter = new Invert();
        
        private static readonly Opening OpeningHorizontalLines;
        private static readonly Opening OpeningVerticalLines;

        static ImageProcessor()
        {
            const int strelSize = 45;
            var strel = new short[strelSize, strelSize];
            Enumerable.Range(0, 45).ToList().ForEach(t => strel[22, t] = 1);
            OpeningHorizontalLines = new Opening(strel);

            const int strelVertSize = 5;
            var strelVertical = new short[strelVertSize, strelVertSize];
            Enumerable.Range(0, 5).ToList().ForEach(t => strelVertical[t, 2] = 1);
            OpeningVerticalLines = new Opening(strelVertical);
        }

        public Bitmap SegmentCode(Bitmap originalImage)
        {
            UnmanagedImage img = UnmanagedImage.FromManagedImage(originalImage);
            img = PreProcess(img);
            img = Process(img);
            return img.ToManagedImage(false);
        }

        private UnmanagedImage PreProcess(UnmanagedImage img)
        {
            int width = Math.Max(img.Width, img.Height) - 99;
            int height = Math.Min(img.Width, img.Height);
            
            if (img.PixelFormat == PixelFormat.Format24bppRgb)
            {
                ApplyFilter(GrayScaleFilter, ref img);
            }
            if (img.Height > img.Width)
            {
                ApplyFilter(RotateFilter, ref img);
            }

            var cropFilter = new Crop(new Rectangle(49, 0, width, height));
            ApplyFilter(cropFilter, ref img);
            return img;
        }

        private UnmanagedImage Process(UnmanagedImage originalImage)
        {
            UnmanagedImage img = UnmanagedImage.Create(originalImage.Width, originalImage.Height, originalImage.PixelFormat);
            ThresholdFilter204.Apply(originalImage, img);

            InvertFilter.ApplyInPlace(img);
            BlobsFiltering removeSmallObjects = new BlobsFiltering(new BlobFilter(8));
            removeSmallObjects.ApplyInPlace(img);

            InvertFilter.ApplyInPlace(img);
            OpeningHorizontalLines.ApplyInPlace(img);

            BlobsFiltering clearBorderFilter = new BlobsFiltering(new BlobFilterBorder(img.Width));
            InvertFilter.ApplyInPlace(img);
            clearBorderFilter.ApplyInPlace(img);

            InvertFilter.ApplyInPlace(img);
            OpeningVerticalLines.ApplyInPlace(img);

            InvertFilter.ApplyInPlace(img);
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.ProcessImage(img);

            // find biggest object and extract it
            Blob blob = blobCounter.GetObjectsInformation().MaxBy(t => t.Area);
            UnmanagedImage result = new Crop(new Rectangle(blob.Rectangle.Location.X, blob.Rectangle.Location.Y, blob.Rectangle.Width + 1, blob.Rectangle.Height + 1)).Apply(originalImage);

            img.Dispose();
            originalImage.Dispose();
            return result;
        }

        private void ApplyFilter(IFilter filter, ref UnmanagedImage source)
        {
            UnmanagedImage result = filter.Apply(source);
            UnmanagedImage tmp = source;
            source = result;
            tmp.Dispose();
        }
    }

    internal class RemoveConnectedComponents : BaseInPlacePartialFilter
    {
        private readonly IBlobsFilter _filter;

        public RemoveConnectedComponents(IBlobsFilter filter)
        {
            _filter = filter;
        }

        protected override void ProcessFilter(UnmanagedImage image, Rectangle rect)
        {
            new Invert().ApplyInPlace(image);
            BlobsFiltering filter = new BlobsFiltering(_filter);
            filter.ApplyInPlace(image);
            new Invert().ApplyInPlace(image);
        }

        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations { get; } = new Dictionary<PixelFormat, PixelFormat>
        {
            [PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed
        };
    }

    internal class BlobFilter : IBlobsFilter
    {
        private readonly int _pixels;

        public BlobFilter(int pixels)
        {
            _pixels = pixels;
        }

        public bool Check(Blob blob)
        {
            return blob.Area >= _pixels;
        }
    }

    internal class BlobFilterBorder : IBlobsFilter
    {
        private readonly int _width;

        public BlobFilterBorder(int width)
        {
            _width = width;
        }

        public bool Check(Blob blob)
        {
            return !(blob.Rectangle.Left == 0 || blob.Rectangle.Right == _width ||
                   blob.Rectangle.Top == 0);
        }
    }
}