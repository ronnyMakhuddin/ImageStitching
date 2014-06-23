//#define CROSS_CORRELATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;

namespace ImageStitching
{
    public partial class PanoramaCreator
    {
        private static int similarityPixelsCount = 2;
        private static int maxLinePixelXDist = 3;

        private PointInt findNearestPixel(PointInt point, PointInt[,] coordsTable, int direction)
        {
            if (point.y > 0)
            {
                if (direction < 0)
                {
                    for (int i = point.x - 1; i >= 0; --i)
                    {
                        if (coordsTable[i, point.y - 1].x >= 0 && coordsTable[i, point.y - 1].y >= 0)
                        {
                            return new PointInt(i, point.y - 1);
                        }
                    }

                    return point;
                }
                else // direction > 0
                {
                    for (int i = point.x + 1; i < coordsTable.GetLength(0); ++i)
                    {
                        if (coordsTable[i, point.y - 1].x >= 0 && coordsTable[i, point.y - 1].y >= 0)
                        {
                            return new PointInt(i, point.y - 1);
                        }
                    }

                    return point;
                }
            }
            else
            {
                return point;
            }
        }

#if CROSS_CORRELATION
        private double calcImagesSimilarityCrossCorrelation(double[,] image1GrayScale, double[,] image2GrayScale,
                                            PointInt[,] coordsTable, PointInt image1Coords)
        {
            List<Pair<int, int>> points = new List<Pair<int, int>>();

            for (int i = image1Coords.x - similarityPixelsCount; i <= image1Coords.x + similarityPixelsCount; ++i)
            {
                for (int j = image1Coords.y - similarityPixelsCount; j <= image1Coords.y + similarityPixelsCount; ++j)
                {
                    if (i >= 0 && i < coordsTable.GetLength(0) && j >= 0 && j < coordsTable.GetLength(1) &&
                        coordsTable[i, j].x >= 0 && coordsTable[i, j].y >= 0)
                    {
                        points.Add(new Pair<int, int>(i, j));
                    }
                }
            }

            double mean1 = 0, mean2 = 0;
            for (int i = 0; i < points.Count; ++i)
            {
                mean1 += image1GrayScale[points[i].First, points[i].Second];
                mean2 += image2GrayScale[coordsTable[points[i].First, points[i].Second].x, coordsTable[points[i].First, points[i].Second].y];
            }
            mean1 /= points.Count;
            mean2 /= points.Count;

            double std1 = 0, std2 = 0;
            for (int i = 0; i < points.Count; ++i)
            {
                std1 += (image1GrayScale[points[i].First, points[i].Second] - mean1) *
                        (image1GrayScale[points[i].First, points[i].Second] - mean1);
                std2 += (image2GrayScale[coordsTable[points[i].First, points[i].Second].x, coordsTable[points[i].First, points[i].Second].y] - mean2) *
                        (image2GrayScale[coordsTable[points[i].First, points[i].Second].x, coordsTable[points[i].First, points[i].Second].y] - mean2);
            }
            std1 /= points.Count;
            std2 /= points.Count;

            double stdMult = std1 * std2;

            double correlation = 0;
            for (int i = 0; i < points.Count; ++i)
            {
                if (stdMult == 0)
                {
                    if (std1 + std2 != 0)
                    {
                        correlation += -1;
                    }
                    else
                    {
                        correlation += 1;
                    }
                }
                else
                {
                    correlation += stdMult == 0 ? -1 : ((image1GrayScale[points[i].First, points[i].Second] - mean1) *
                                    (image2GrayScale[coordsTable[points[i].First, points[i].Second].x, coordsTable[points[i].First, points[i].Second].y] - mean2)) /
                                    stdMult;
                }
            }
            correlation /= points.Count;

            return correlation;
        }
#else

        private double calcImagesSimilaritySAD(PixelColor[,] image1Pixels, PixelColor[,] image2Pixels, PointInt[,] coordsTable, PointInt image1Coords)
        {
            List<Pair<int, int>> points = new List<Pair<int, int>>();

            for (int i = image1Coords.x - similarityPixelsCount; i <= image1Coords.x + similarityPixelsCount; ++i)
            {
                for (int j = image1Coords.y - similarityPixelsCount; j <= image1Coords.y + similarityPixelsCount; ++j)
                {
                    if (i >= 0 && i < coordsTable.GetLength(0) && j >= 0 && j < coordsTable.GetLength(1) &&
                        coordsTable[i, j].x >= 0 && coordsTable[i, j].y >= 0)
                    {
                        points.Add(new Pair<int, int>(i, j));
                    }
                }
            }

            double absoluteDiff = 0;
            
            for (int i = 0; i < points.Count; ++i)
            {
                absoluteDiff += Math.Abs(image1Pixels[points[i].First, points[i].Second].red - image2Pixels[coordsTable[points[i].First, points[i].Second].x, coordsTable[points[i].First, points[i].Second].y].red) +
                                Math.Abs(image1Pixels[points[i].First, points[i].Second].green - image2Pixels[coordsTable[points[i].First, points[i].Second].x, coordsTable[points[i].First, points[i].Second].y].green) +
                                Math.Abs(image1Pixels[points[i].First, points[i].Second].blue - image2Pixels[coordsTable[points[i].First, points[i].Second].x, coordsTable[points[i].First, points[i].Second].y].blue);
            }

            return 765 - (absoluteDiff / points.Count);
        }
#endif

        /// <param name="coordsTable">Tablica o wymiarach pierwszego łączonego obrazu zawierające przyporządkowania puntów drugiego obrazu
        /// po dokonaniu przekształcenia; <-1;-1> gdy w danym punkcie nie ma przyporządkowania drugiego obrazu</param>
        /// <param name="outPixelsSource">Tablica o wymiarach pierwszego łączonego obrazu zawierające informację, z którego obrazu trzeba w tym
        /// punkcie wstawić piksel: 1, gdy z pierwszego, 2 z drugiego</param>
        private void findPartitionLine(MyImage image1, MyImage image2, PointInt[,] coordsTable, out int[,] outPixelsSource)
        {
#if CROSS_CORRELATION
            double[,] image1GrayScale = new double[image1.GetWidth(), image1.GetHeight()];
            double[,] image2GrayScale = new double[image2.GetWidth(), image2.GetHeight()];

            for (int i = 0; i < image1.GetWidth(); ++i)
            {
                for (int j = 0; j < image1.GetHeight(); ++j)
                {
                    image1GrayScale[i, j] = Utilities.GetGrayScale(image1.GetPixel(i, j));
                }
            }
            for (int i = 0; i < image2.GetWidth(); ++i)
            {
                for (int j = 0; j < image2.GetHeight(); ++j)
                {
                    image2GrayScale[i, j] = Utilities.GetGrayScale(image2.GetPixel(i, j));
                }
            }

#else
            PixelColor[,] image1Pixels = new PixelColor[image1.GetWidth(), image1.GetHeight()];
            PixelColor[,] image2Pixels = new PixelColor[image2.GetWidth(), image2.GetHeight()];
            for (int i = 0; i < image1.GetWidth(); ++i)
            {
                for (int j = 0; j < image1.GetHeight(); ++j)
                {
                    image1Pixels[i, j] = image1.GetPixel(i, j);
                }
            }
            for (int i = 0; i < image2.GetWidth(); ++i)
            {
                for (int j = 0; j < image2.GetHeight(); ++j)
                {
                    image2Pixels[i, j] = image2.GetPixel(i, j);
                }
            }
#endif

            Pair<PointInt, double>[,] coordsDyn = new Pair<PointInt, double>[coordsTable.GetLength(0), coordsTable.GetLength(1)];
            int width = coordsDyn.GetLength(0);
            int height = coordsDyn.GetLength(1);

            for (int i = 0; i < width; ++i)
            {
                if (coordsTable[i, 0].x >= 0 && coordsTable[i, 0].y >= 0)
                {
                    coordsDyn[i, 0] = new Pair<PointInt, double>(new PointInt(i, 0), 0);
                }
                else
                {
                    coordsDyn[i, 0] = new Pair<PointInt, double>(new PointInt(-1, -1), 0);
                }
            }

            for (int i = 1; i < height; ++i)
            {
                Parallel.For(0, width, j =>
                {
                    PointInt point = new PointInt(j, i);

                    if (coordsTable[point.x, point.y].x >= 0 && coordsTable[point.x, point.y].y >= 0)
                    {
                        List<PointInt> candidates = new List<PointInt>();

                        if (coordsTable[j, i - 1].x >= 0 && coordsTable[j, i - 1].y >= 0)
                        {
                            candidates.Add(new PointInt(j, i - 1));
                        }

                        PointInt nearestLeft = findNearestPixel(point, coordsTable, -1);
                        PointInt nearestRight = findNearestPixel(point, coordsTable, 1);

                        if (nearestLeft != point)
                        {
                            for (int k = nearestLeft.x; k >= 0 && nearestLeft.x - k < maxLinePixelXDist; --k)
                            {
                                if (coordsTable[k, nearestLeft.y].x >= 0 && coordsTable[k, nearestLeft.y].y >= 0)
                                {
                                    candidates.Add(new PointInt(k, nearestLeft.y));
                                }
                            }
                        }

                        if (nearestRight != point)
                        {
                            for (int k = nearestRight.x; k < coordsTable.GetLength(0) && k - nearestRight.x < maxLinePixelXDist; ++k)
                            {
                                if (coordsTable[k, nearestRight.y].x >= 0 && coordsTable[k, nearestRight.y].y >= 0)
                                {
                                    candidates.Add(new PointInt(k, nearestRight.y));
                                }
                            }
                        }

                        if (candidates.Count > 0)
                        {
                            double maxCorrelation = double.MinValue;
                            PointInt minCorrelationPoint = point;

                            for (int k = 0; k < candidates.Count; ++k)
                            {
#if CROSS_CORRELATION
                                double correlation = calcImagesSimilarityCrossCorrelation(image1GrayScale, image2GrayScale, coordsTable, candidates[k]);
#else
                                double correlation = calcImagesSimilaritySAD(image1Pixels, image2Pixels, coordsTable, candidates[k]);
#endif
                                if (correlation > maxCorrelation)
                                {
                                    maxCorrelation = correlation;
                                    minCorrelationPoint = candidates[k];
                                }
                            }

                            coordsDyn[j, i] = new Pair<PointInt, double>(minCorrelationPoint,
                                coordsDyn[minCorrelationPoint.x, minCorrelationPoint.y].Second + maxCorrelation);
                        }
                        else // candidates.Count == 0
                        {
                            coordsDyn[j, i] = new Pair<PointInt, double>(point, 0);
                        }
                    }
                });
            }

            outPixelsSource = new int[width, height];

            int endHeight = height - 1;
            bool foundPixel = false;
            for (; endHeight >= 0 && foundPixel == false; --endHeight)
            {
                for (int j = 0; j < width && foundPixel == false; ++j)
                {
                    if (coordsDyn[j, endHeight] != null)
                    {
                        foundPixel = true;
                    }
                }
            }
            ++endHeight;

            for (int i = endHeight + 1; i < height; ++i)
            {
                for (int j = 0; j < width; ++j)
                {
                    outPixelsSource[j, i] = 1;
                }
            }

            int downMaxCorrelationIndex = 0;
            for (int i = 0; i < width; ++i)
            {
                if (coordsTable[i, endHeight].x >= 0 && coordsTable[i, endHeight].y >= 0 && (coordsDyn[downMaxCorrelationIndex, endHeight] == null ||
                    coordsDyn[i, endHeight].Second > coordsDyn[downMaxCorrelationIndex, endHeight].Second))
                {
                    downMaxCorrelationIndex = i;
                }
            }

            int partitionIndex = downMaxCorrelationIndex;
            for (int i = endHeight; i >= 0; --i)
            {
                bool crossedLine = false;

                for (int j = 0; j < width; ++j)
                {
                    if (coordsTable[j, i].x >= 0 && coordsTable[j, i].y >= 0)
                    {
                        if (j == partitionIndex)
                        {
                            outPixelsSource[j, i] = 1;
                            crossedLine = true;
                        }
                        else
                        {
                            if (crossedLine)
                            {
                                outPixelsSource[j, i] = 2;
                            }
                            else
                            {
                                outPixelsSource[j, i] = 1;
                            }
                        }
                    }
                    else
                    {
                        if (crossedLine)
                        {
                            outPixelsSource[j, i] = 2;
                        }
                        else
                        {
                            outPixelsSource[j, i] = 1;
                        }
                    }
                }

                if (coordsDyn[partitionIndex, i] != null)
                {
                    partitionIndex = coordsDyn[partitionIndex, i].First.x;
                }
            }
        }
    }
}
