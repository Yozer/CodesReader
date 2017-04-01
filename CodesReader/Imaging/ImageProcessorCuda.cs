//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Drawing2D;
//using System.Drawing.Imaging;
//using Emgu.CV;
//using Emgu.CV.Cuda;
//using Emgu.CV.CvEnum;
//using Emgu.CV.Structure;
//using Emgu.CV.Util;

//namespace CodesReader.Imaging
//{
//    class ImageProcessorCuda : IImageProcessor
//    {
//        //private static readonly CudaMorphologyFilter HorizontalOpening;
//        //private static readonly CudaMorphologyFilter VerticalOpening;

//        static ImageProcessorCuda()
//        {
//            //var strel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(45, 1), new Point(0, 0));
//            //HorizontalOpening = new CudaMorphologyFilter(MorphOp.Open, DepthType.Cv8U, 1, strel, new Point(-1, -1), 1);

//            //strel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(1, 5), new Point(0, 0));
//            //VerticalOpening = new CudaMorphologyFilter(MorphOp.Open, DepthType.Cv8U, 1, strel, new Point(-1, -1), 1);
//        }

//        public List<Bitmap> SegmentCode(string path)
//        {
//            var result = new List<Bitmap>(5*5 + 1);
//            using (Mat img = CvInvoke.Imread(path, ImreadModes.Grayscale))
//            {
//                GpuMat tmp, src = new GpuMat();
//                src.Upload(img);

//                PreProcess(ref src, out tmp);
//                Mat preprocessedImage = src.ToMat();

//                Rectangle rectangle = Process(src, tmp);

//                var myImage = new Matrix<byte>(preprocessedImage.Rows, preprocessedImage.Cols, preprocessedImage.DataPointer);
//                var codeImage = myImage.GetSubRect(rectangle);
//                result.Add((Bitmap) codeImage.Mat.Bitmap.Clone());

//                Mat threshold = new Mat();
//                CvInvoke.Threshold(codeImage.Mat, threshold, 204.0, 255.0, ThresholdType.Binary);
//                CutImage(result, threshold.Bitmap);

//                threshold.Dispose();
//                myImage.Dispose();
//                codeImage.Dispose();
//                preprocessedImage.Dispose();
//            }

//            return result;
//        }

//        private void CutImage(List<Bitmap> result, Bitmap code)
//        {
//            bool isBlackPixel = false;
//            Point prevCut = new Point(0, 0);

//            for (int x = 0; x < code.Width; x++)
//            {
//                if (!isBlackPixel)
//                {
//                    for (int y = 0; y < code.Height; y++)
//                    {
//                        var color = code.GetPixel(x, y);

//                        if (ColorEquals(color, Color.Black))
//                        {
//                            isBlackPixel = true;
//                            if (Math.Abs(0 - x + 1) > 3)
//                            {
//                                result.Add((Bitmap) CropImage(result[0], new Rectangle(prevCut, new Size(x - 1 - prevCut.X, code.Height))));
//                                prevCut = new Point(x - 1, 0);
//                            }
//                            break;
//                        }
//                    }
//                }

//                if (isBlackPixel)
//                {
//                    bool allWhite = true;
//                    for (int y = 0; y < code.Height; y++)
//                    {
//                        var color = code.GetPixel(x, y);
//                        if (ColorEquals(color, Color.Black))
//                        {
//                            allWhite = false;
//                            break;
//                        }
//                    }

//                    if (allWhite)
//                    {
//                        isBlackPixel = false;
//                    }
//                }
//            }

//            result.Add((Bitmap)CropImage(result[0], new Rectangle(prevCut, new Size(code.Width - prevCut.X, code.Height))));
//        }

//        private static Image CropImage(Image img, Rectangle cropArea)
//        {
//            Bitmap bmpImage = new Bitmap(img);
//            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
//        }
//        static bool ColorEquals(Color colorA, Color colorB)
//        {
//            return colorA.R == colorB.R && colorA.G == colorB.G && colorA.B == colorB.B;
//        }
//        private void PreProcess(ref GpuMat src, out GpuMat tmp)
//        {
//            var size = src.Size;
//            int width = Math.Max(size.Width, size.Height) - 99;
//            int height = Math.Min(size.Width, size.Height);
//            tmp = new GpuMat(height, width, src.Depth, src.NumberOfChannels, src.IsContinuous);

//            double rotate = 0;
//            double yShift = 0;
//            if (size.Height > size.Width)
//            {
//                rotate = 90;
//                yShift = height;
//            }

//            CudaInvoke.Rotate(src, tmp, tmp.Size, rotate, -49, yShift);
//            Swap(ref src, ref tmp);
//        }

//        private Rectangle Process(GpuMat src, GpuMat tmp)
//        {
//            CudaInvoke.Threshold(src, tmp, 204.0d, 255.0d, ThresholdType.BinaryInv);
//            Swap(ref src, ref tmp);

//            RemoveSmallObjects(src);
//            Inverse(ref src, ref tmp);

//            var strel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(45, 1), new Point(0, 0));
//            var HorizontalOpening = new CudaMorphologyFilter(MorphOp.Open, DepthType.Cv8U, 1, strel, new Point(-1, -1), 1);

//            var strel2 = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(1, 5), new Point(0, 0));
//            var VerticalOpening = new CudaMorphologyFilter(MorphOp.Open, DepthType.Cv8U, 1, strel2, new Point(-1, -1), 1);

//            HorizontalOpening.Apply(src, tmp);
//            Swap(ref src, ref tmp);

//            ClearBorder(ref src, ref tmp);

//            VerticalOpening.Apply(src, tmp);
//            Swap(ref src, ref tmp);

//            var result = FindBiggestBlob(ref src, ref tmp);
//            src.Dispose();
//            tmp.Dispose();
//            return result;
//        }

//        private void Inverse(ref GpuMat src, ref GpuMat tmp)
//        {
//            CudaInvoke.BitwiseNot(src, tmp);
//            Swap(ref src, ref tmp);
//        }

//        private Rectangle FindBiggestBlob(ref GpuMat src, ref GpuMat tmp)
//        {
//            Inverse(ref src, ref tmp);
//            Rectangle result;

//            using (var mat = new Mat())
//            {
//                src.Download(mat);

//                var labelRaw = new Mat();
//                var statsRaw = new Mat();
//                var centroids = new Mat();
//                int number = CvInvoke.ConnectedComponentsWithStats(mat, labelRaw, statsRaw, centroids);

//                var stats = new Matrix<int>(statsRaw.Rows, statsRaw.Cols, statsRaw.DataPointer);
//                var myImage = new Matrix<byte>(mat.Rows, mat.Cols, mat.DataPointer);

//                int maxArea = 0;
//                int labelIndex = 0;

//                for (int i = 1; i < number; i++)
//                {
//                    int area = stats[i, (int)ConnectecComponentsTypes.Area];

//                    if (area > maxArea)
//                    {
//                        labelIndex = i;
//                        maxArea = area;
//                    }
//                }

//                int left = stats[labelIndex, (int)ConnectecComponentsTypes.Left];
//                int top = stats[labelIndex, (int)ConnectecComponentsTypes.Top];
//                int width = stats[labelIndex, (int) ConnectecComponentsTypes.Width];
//                int height = stats[labelIndex, (int) ConnectecComponentsTypes.Height];
//                result = new Rectangle(left, top, width + left + 1 > mat.Cols ? width : (width + 1), height + top + 1 > mat.Rows ? height : (height + 1));

//                stats.Dispose();
//                myImage.Dispose();
//                labelRaw.Dispose();
//                statsRaw.Dispose();
//                centroids.Dispose();
//            }

//            return result;
//        }

//        private void ClearBorder(ref GpuMat src, ref GpuMat tmp)
//        {
//            Inverse(ref src, ref tmp);
//            using (var mat = new Mat())
//            {
//                src.Download(mat);

//                var labelRaw = new Mat();
//                var statsRaw = new Mat();
//                var centroids = new Mat();
//                int number = CvInvoke.ConnectedComponentsWithStats(mat, labelRaw, statsRaw, centroids);

//                var label = new Matrix<int>(labelRaw.Rows, labelRaw.Cols, labelRaw.DataPointer);
//                var stats = new Matrix<int>(statsRaw.Rows, statsRaw.Cols, statsRaw.DataPointer);
//                var myImage = new Matrix<byte>(mat.Rows, mat.Cols, mat.DataPointer);

//                for (int i = 1; i < number; i++)
//                {
//                    int left = stats[i, (int)ConnectecComponentsTypes.Left];
//                    int width = stats[i, (int)ConnectecComponentsTypes.Width] + left;
//                    int top = stats[i, (int)ConnectecComponentsTypes.Top];
//                    int height = stats[i, (int)ConnectecComponentsTypes.Height] + top;

//                    if (left == 0 || width == mat.Width || top == 0)
//                    {
//                        for (int x = left; x < width; x++)
//                        {
//                            for (int y = top; y < height; y++)
//                            {
//                                if (label[y, x] == i)
//                                {
//                                    myImage[y, x] = 0;
//                                }
//                            }
//                        }
//                    }
//                }

//                src.Upload(myImage.Mat);

//                label.Dispose();
//                stats.Dispose();
//                myImage.Dispose();
//                labelRaw.Dispose();
//                statsRaw.Dispose();
//                centroids.Dispose();
//            }

//            Inverse(ref src, ref tmp);
//        }

//        private static void RemoveSmallObjects(GpuMat src)
//        {
//            using (var mat = new Mat())
//            {
//                src.Download(mat);

//                var labelRaw = new Mat();
//                var statsRaw = new Mat();
//                var centroids = new Mat();
//                int number = CvInvoke.ConnectedComponentsWithStats(mat, labelRaw, statsRaw, centroids);

//                var label = new Matrix<int>(labelRaw.Rows, labelRaw.Cols, labelRaw.DataPointer);
//                var stats = new Matrix<int>(statsRaw.Rows, statsRaw.Cols, statsRaw.DataPointer);
//                var myImage = new Matrix<byte>(mat.Rows, mat.Cols, mat.DataPointer);

//                for (int i = 1; i < number; i++)
//                {
//                    int area = stats[i, (int)ConnectecComponentsTypes.Area];
//                    if (area < 8)
//                    {
//                        int left = stats[i, (int)ConnectecComponentsTypes.Left];
//                        int width = stats[i, (int)ConnectecComponentsTypes.Width] + left;
//                        int top = stats[i, (int)ConnectecComponentsTypes.Top];
//                        int height = stats[i, (int)ConnectecComponentsTypes.Height] + top;

//                        for (int x = left; x < width; x++)
//                        {
//                            for (int y = top; y < height; y++)
//                            {
//                                if (label[y, x] == i)
//                                {
//                                    myImage[y, x] = 0;
//                                }
//                            }
//                        }
//                    }
//                }

//                src.Upload(myImage.Mat);

//                label.Dispose();
//                stats.Dispose();
//                myImage.Dispose();
//                labelRaw.Dispose();
//                statsRaw.Dispose();
//                centroids.Dispose();
//            }
//        }

//        static void Swap<T>(ref T x, ref T y)
//        {
//            T t = y;
//            y = x;
//            x = t;
//        }
//    }

//}
