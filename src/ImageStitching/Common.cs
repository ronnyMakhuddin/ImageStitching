using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageStitching
{
    public class Pair<T, U> : IEquatable<Object>
    {
        public Pair()
        {

        }

        public Pair(T first, U second)
        {
            this.First = first;
            this.Second = second;
        }

        public T First { get; set; }
        public U Second { get; set; }

        public override bool Equals(Object pair)
        {
            return pair is Pair<T, U> && First.Equals(((Pair<T, U>)pair).First) && Second.Equals(((Pair<T, U>)pair).Second);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public struct PointInt
    {
        public int x, y;

        public PointInt(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public PointInt(PointInt point)
        {
            this.x = point.x;
            this.y = point.y;
        }

        public static bool operator ==(PointInt point1, PointInt point2)
        {
            return point1.x == point2.x && point1.y == point2.y;
        }

        public static bool operator !=(PointInt point1, PointInt point2)
        {
            return (point1 == point2) == false;
        }

        public override bool Equals(Object o)
        {
            return o is PointInt && (PointInt)o == this;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class TripleIndex
    {
        public int a, b, c;

        public TripleIndex(int a, int b, int c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }

    public static class Utilities
    {
        public static double CalcDistanceBetweenPoints(Point point1, Point point2)
        {
            return Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) + (point1.Y - point2.Y) * (point1.Y - point2.Y));
        }

        public static double RadToDeg(double radian)
        {
            return radian * 180 / Math.PI;
        }

        public static double NormalizeAngleDegree(double degree)
        {
            return degree - Math.Floor(degree / 360) * degree;
        }

        public static double CalcSegmentAngle(Pair<Point, Point> segment)
        {
            return Math.Atan2(segment.First.Y - segment.Second.Y, segment.First.X - segment.Second.X);
        }

        public static int calcBitmapImageIndex(int x, int y, int width)
        {
            return 4 * (y * width + x);
        }

        public static byte[] getBitmapImagePixelArray(BitmapImage image)
        {
            int stride = image.PixelWidth * 4;
            int size = image.PixelHeight * stride;
            byte[] imagePixels = new byte[size];
            image.CopyPixels(imagePixels, stride, 0);

            return imagePixels;
        }

        public static long Factorial(int number)
        {
            long factorial = 1;
            int iteration = 1;
            while (iteration <= number)
            {
                factorial *= iteration;
                ++iteration;
            }

            return factorial;
        }

        public static List<TripleIndex> GetTrheeCombinationsWithFirstSet(int set, int end)
        {
            int intervalLength = end - set;
            if (intervalLength > 2)
            {
                List<TripleIndex> tripleIndices = new List<TripleIndex>();

                for (int i = set + 1; i <= end; ++i)
                {
                    for (int j = i + 1; j <= end; ++j)
                    {
                        tripleIndices.Add(new TripleIndex(set, i, j));
                    }
                }

                return tripleIndices;
            }
            else
            {
                return new List<TripleIndex>();
            }
        }

        public static List<TripleIndex> GetThreeCombinations(int start, int end, long max = long.MaxValue)
        {
            int intervalLength = end - start;
            if (intervalLength >= 2)
            {
                List<TripleIndex> tripleIndices = new List<TripleIndex>();

                int count = 0;

                for (int i = start; i <= end && count < max; ++i)
                {
                    for (int j = i + 1; j <= end && count < max; ++j)
                    {
                        for (int k = j + 1; k <= end && count < max; ++k)
                        {
                            tripleIndices.Add(new TripleIndex(i, j, k));
                            ++count;
                        }
                    }
                }

                return tripleIndices;
            }
            else
            {
                return new List<TripleIndex>();
            }

        }

        public static double Det3x3Matrix(Matrix matrix)
        {
            double plus = matrix[0, 0] * matrix[1, 1] * matrix[2, 2] + matrix[0, 1] * matrix[1, 2] * matrix[2, 0] + matrix[0, 2] * matrix[1, 0] * matrix[2, 1];
            double minus = matrix[0, 2] * matrix[1, 1] * matrix[2, 0] + matrix[0, 0] * matrix[1, 2] * matrix[2, 1] + matrix[0, 1] * matrix[1, 0] * matrix[2, 2];

            return plus - minus;
        }

        public static double GetGrayScale(PixelColor color)
        {
            return (0.3 * color.red + 0.59 * color.green + 0.11 * color.blue) / 255;
        }

        public static bool ArePointsColinear(PointInt point1, PointInt point2, PointInt point3)
        {
            Matrix pointsMatrix = new Matrix(3, 3);

            pointsMatrix[0, 0] = point1.x;
            pointsMatrix[0, 1] = point1.y;
            pointsMatrix[0, 2] = 1;
            pointsMatrix[1, 0] = point2.x;
            pointsMatrix[1, 1] = point2.y;
            pointsMatrix[1, 2] = 1;
            pointsMatrix[2, 0] = point3.x;
            pointsMatrix[2, 1] = point3.y;
            pointsMatrix[2, 2] = 1;

            return Det3x3Matrix(pointsMatrix) < 0.00000001;
        }

        public static int GetPointsOrientation(PointInt point1, PointInt point2, PointInt point3)
        {
            Matrix pointsMatrix = new Matrix(3, 3);

            pointsMatrix[0, 0] = point1.x;
            pointsMatrix[0, 1] = point1.y;
            pointsMatrix[0, 2] = 1;
            pointsMatrix[1, 0] = point2.x;
            pointsMatrix[1, 1] = point2.y;
            pointsMatrix[1, 2] = 1;
            pointsMatrix[2, 0] = point3.x;
            pointsMatrix[2, 1] = point3.y;
            pointsMatrix[2, 2] = 1;

            return Math.Sign(Det3x3Matrix(pointsMatrix));
        }

        public static bool AreSegmentsIntersection(Pair<PointInt, PointInt> segment1, Pair<PointInt, PointInt> segment2)
        {
            bool colinear = ArePointsColinear(segment1.First, segment1.Second, segment2.First);
            if (colinear)
            {
                bool belonging = segment2.First.x >= Math.Min(segment1.First.x, segment1.Second.x) &&
                                 segment2.First.y >= Math.Min(segment1.First.y, segment1.Second.y);

                if (belonging)
                {
                    return true;
                }
            }

            colinear = ArePointsColinear(segment1.First, segment1.Second, segment2.Second);
            if (colinear)
            {
                bool belonging = segment2.Second.x >= Math.Min(segment1.First.x, segment1.Second.x) &&
                                 segment2.Second.y >= Math.Min(segment1.First.y, segment1.Second.y);

                if (belonging)
                {
                    return true;
                }
            }

            colinear = ArePointsColinear(segment2.First, segment2.Second, segment1.First);
            if (colinear)
            {
                bool belonging = segment1.First.x >= Math.Min(segment2.First.x, segment2.Second.x) &&
                                 segment1.First.y >= Math.Min(segment2.First.y, segment2.Second.y);

                if (belonging)
                {
                    return true;
                }
            }

            colinear = ArePointsColinear(segment2.First, segment2.Second, segment1.Second);
            if (colinear)
            {
                bool belonging = segment1.Second.x >= Math.Min(segment2.First.x, segment2.Second.x) &&
                                 segment1.Second.y >= Math.Min(segment2.First.y, segment2.Second.y);

                if (belonging)
                {
                    return true;
                }
            }

            return GetPointsOrientation(segment1.First, segment1.Second, segment2.First) != GetPointsOrientation(segment1.First, segment1.Second, segment2.Second);
        }

        public static bool IsPointInsideQuadrilateral(PointInt point, PointInt quadrilateral1, PointInt quadrilateral2, PointInt quadrilateral3,
                                               PointInt quadrilateral4)
        {
            Pair<PointInt, PointInt> segment = new Pair<PointInt, PointInt>(point, new PointInt(1000000, point.y + 1));

            int intersectionsCount = 0;

            if (AreSegmentsIntersection(segment, new Pair<PointInt,PointInt>(quadrilateral1, quadrilateral2)))
            {
                ++intersectionsCount;
            }
            if (AreSegmentsIntersection(segment, new Pair<PointInt,PointInt>(quadrilateral2, quadrilateral3)))
            {
                ++intersectionsCount;
            }
            if (AreSegmentsIntersection(segment, new Pair<PointInt,PointInt>(quadrilateral3, quadrilateral4)))
            {
                ++intersectionsCount;
            }
            if (AreSegmentsIntersection(segment, new Pair<PointInt,PointInt>(quadrilateral4, quadrilateral1)))
            {
                ++intersectionsCount;
            }

            return intersectionsCount == 1;
        }
    }
}
