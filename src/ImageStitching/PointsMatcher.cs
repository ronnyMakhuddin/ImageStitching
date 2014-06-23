using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace ImageStitching
{
    public static class PointsMatcher
    {
        /// <summary>
        ///  Rozmiar obrazu (w pikselach) poddany przetwarzaniu
        /// </summary>
        private static int imagePixelsCount = 1000000;

        private static Image<Gray, Byte> emguImageFromFile(string file)
        {
            return new Image<Gray, Byte>(file);
        }

        private static double scaleImage(ref Image<Gray, Byte> image)
        {
            int pixelsCount = image.Width * image.Height;
            double pixelSizeFactor = pixelsCount / (double)imagePixelsCount;
            double resizeFactor = Math.Floor(Math.Sqrt(pixelSizeFactor));

            if (resizeFactor >= 2)
            {
                double scaleFactor = 1 / resizeFactor;
                image = image.Resize(scaleFactor, INTER.CV_INTER_CUBIC);
            }

            return resizeFactor >= 2 ? resizeFactor : 1;
        }

        public static void MatchPoints(string image1Path, string image2Path, out List<Pair<Point, Point>> outPoints)
        {
            Image<Gray, Byte> modelImage = emguImageFromFile(image1Path);
            Image<Gray, Byte> observedImage = emguImageFromFile(image2Path);

            double modelImageScaleFactor = scaleImage(ref modelImage);
            double observedImageScaleFactor = scaleImage(ref observedImage);

            SURFDetector surfCPU = new SURFDetector(500, false);

            VectorOfKeyPoint modelKeyPoints;
            VectorOfKeyPoint observedKeyPoints;
            Matrix<int> indices;
            Matrix<float> dist;
            Matrix<byte> mask;

            //extract features from the object image
            modelKeyPoints = surfCPU.DetectKeyPointsRaw(modelImage, null);
            Matrix<float> modelDescriptors = surfCPU.ComputeDescriptorsRaw(modelImage, null, modelKeyPoints);

            // extract features from the observed image
            observedKeyPoints = surfCPU.DetectKeyPointsRaw(observedImage, null);
            Matrix<float> observedDescriptors = surfCPU.ComputeDescriptorsRaw(observedImage, null, observedKeyPoints);

            BruteForceMatcher matcher = new BruteForceMatcher(BruteForceMatcher.DistanceType.L2F32);
            matcher.Add(modelDescriptors);
            int k = 2;
            indices = new Matrix<int>(observedDescriptors.Rows, k);
            dist = new Matrix<float>(observedDescriptors.Rows, k);
            matcher.KnnMatch(observedDescriptors, indices, dist, k, null);

            mask = new Matrix<byte>(dist.Rows, 1);

            mask.SetValue(255);

            Features2DTracker.VoteForUniqueness(dist, 0.8, mask);

            int nonZeroCount = CvInvoke.cvCountNonZero(mask);
            if (nonZeroCount >= 4)
            {
                nonZeroCount = Features2DTracker.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20);
            }

            MKeyPoint[] modelArray = modelKeyPoints.ToArray();
            MKeyPoint[] observedArray = observedKeyPoints.ToArray();

            outPoints = new List<Pair<Point, Point>>();

            for (int i = 0; i < indices.Rows; ++i)
            {
                if (mask[i, 0] == 255)
                {
                    int j = dist[i, 0] < dist[i, 1] ? 0 : 1;
                    int index1 = indices[i, j];
                    int index2 = i;

                    outPoints.Add(new Pair<Point, Point>(
                        new Point(modelArray[index1].Point.X * modelImageScaleFactor, modelArray[index1].Point.Y * modelImageScaleFactor),
                        new Point(observedArray[index2].Point.X * observedImageScaleFactor, observedArray[index2].Point.Y * observedImageScaleFactor)));
                }
            }
        }
    }
}
