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
        public Bitmap SegmentCode(Bitmap originalImage)
        {
            using (Bitmap image = Accord.Imaging.Image.Clone(originalImage))
            {
                UnmanagedImage img = UnmanagedImage.FromManagedImage(image);
                img = PreProcess(img);
                img = Process(img);
                return img.ToManagedImage(false);
            }
        }

        private UnmanagedImage PreProcess(UnmanagedImage img)
        {
            FiltersSequence sequence = new FiltersSequence();
            int width = Math.Max(img.Width, img.Height) - 99;
            int height = Math.Min(img.Width, img.Height);
            
            if (img.PixelFormat == PixelFormat.Format24bppRgb)
            {
                sequence.Add(new Grayscale(0.2989, 0.5870, 0.1140));
            }
            if (img.Height > img.Width)
            {
                sequence.Add(new RotateBilinear(90, false));
            }
            sequence.Add(new Crop(new Rectangle(49, 0, width, height)));
            UnmanagedImage result = sequence.Apply(img);
            img.Dispose();
            return result;
        }

        private UnmanagedImage Process(UnmanagedImage img)
        {
            FiltersSequence sequence = new FiltersSequence();
            sequence.Add(new Threshold(204));
            sequence.Add(new RemoveConnectedComponents(new BlobFilter(8)));

            var strel = new short[45, 45];
            Enumerable.Range(0, 45).ToList().ForEach(t => strel[22, t] = 1);
            sequence.Add(new Opening(strel));

            sequence.Add(new RemoveConnectedComponents(new BlobFilterBorder(img.Width)));

            var strelVertical = new short[5, 5];
            Enumerable.Range(0, 5).ToList().ForEach(t => strelVertical[t, 2] = 1);
            sequence.Add(new Opening(strelVertical));

            UnmanagedImage processedImage = sequence.Apply(img);

            new Invert().ApplyInPlace(processedImage);
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.ProcessImage(processedImage);

            // find biggest object and extract it
            Blob blob = blobCounter.GetObjectsInformation().MaxBy(t => t.Area);
            UnmanagedImage result = new Crop(new Rectangle(blob.Rectangle.Location.X, blob.Rectangle.Location.Y, blob.Rectangle.Width + 1, blob.Rectangle.Height + 1)).Apply(img);

            processedImage.Dispose();
            img.Dispose();
            return result;
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