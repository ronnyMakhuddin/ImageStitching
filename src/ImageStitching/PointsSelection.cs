using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ImageStitching
{
    public partial class PanoramaCreator
    {
        private static double maxSegmentsDiffTolerance = 0.15;
        private static double maxTransformationDiffTolerance = 1000;
        private static int firstMatchSearchCount = 10;
        private static int sameGroupSearchMatch = 20;

        private class SegmentParams
        {
            public double angle;
            public double length;
            private double maxLength;

            private readonly double maxDifference;

            private static double angleFactor = 1.4;
            private static double lengthFactor = 1;

            public SegmentParams(double angle, double length, double maxLength)
            {
                this.angle = angle;
                this.length = length;
                this.maxLength = maxLength;

                maxDifference = angleFactor + lengthFactor;
            }

            private double calcAngleDiff(double secondAngle)
            {
                double diff = Math.Abs(angle - secondAngle);
                if (diff > 180)
                {
                    diff = 360 - diff;
                }

                return diff / 180;
            }

            /// <summary>
            /// Wartość od 0 do 1
            /// </summary>
            public double CalcDifference(SegmentParams otherSegment)
            {
                return (angleFactor * calcAngleDiff(otherSegment.angle) +
                        lengthFactor * (Math.Abs(length - otherSegment.length) / maxLength)) / maxDifference;
            }
        }

        private void sortSegments(List<Pair<Pair<Point, Point>, SegmentParams>> segments)
        {
            Pair<Pair<Point, Point>, SegmentParams>[] segmentsCopy = new Pair<Pair<Point, Point>, SegmentParams>[segments.Count];

            bool[] free = new bool[segments.Count];
            for (int i = 0; i < free.Length; ++i)
            {
                free[i] = true;
            }

            segmentsCopy[0] = segments[0];
            free[0] = false;

            int best = segments[0].Second.CalcDifference(segments[1].Second) < segments[0].Second.CalcDifference(segments[2].Second) ? 1 : 2;
            int worse = best == 1 ? 2 : 1;

            for (int j = 3; j < segments.Count; ++j)
            {
                if (segments[0].Second.CalcDifference(segments[j].Second) < segments[0].Second.CalcDifference(segments[best].Second))
                {
                    best = j;
                }
                else if (segments[0].Second.CalcDifference(segments[j].Second) > segments[0].Second.CalcDifference(segments[worse].Second))
                {
                    worse = j;
                }
            }
            segmentsCopy[1] = segments[best];
            segmentsCopy[segmentsCopy.Length - 1] = segments[worse];
            free[best] = false;
            free[worse] = false;
            int first = 1;
            int last = segments.Count - 1;

            while (first + 1 < last - 1)
            {
                int bestFirst = -1;
                int bestLast = -1;

                for (int j = 1; j < segments.Count; ++j)
                {
                    if (free[j])
                    {
                        if (bestFirst == -1 || segmentsCopy[first].Second.CalcDifference(segments[j].Second) < segmentsCopy[first].Second.CalcDifference(segments[bestFirst].Second))
                        {
                            bestFirst = j;
                        }
                        if (bestLast == -1 || segmentsCopy[last].Second.CalcDifference(segments[j].Second) < segmentsCopy[last].Second.CalcDifference(segments[bestLast].Second))
                        {
                            bestLast = j;
                        }
                    }
                }

                segmentsCopy[first + 1] = segments[bestFirst];
                segmentsCopy[last - 1] = segments[bestLast];
                free[bestFirst] = false;
                free[bestLast] = false;
                ++first;
                --last;
            }

            if (first + 1 == last - 1)
            {
                for (int i = 0; i < free.Length; ++i)
                {
                    if (free[i] == true)
                    {
                        segmentsCopy[first + 1] = segments[i];
                        break;
                    }
                }
            }

            for (int i = 0; i < segments.Count; ++i)
            {
                segments[i] = segmentsCopy[i];
            }
        }

        int[] getMaxAcceptedSegment(List<Pair<Pair<Point, Point>, SegmentParams>> segments)
        {
            int[] maxAcceptedSegment = new int[segments.Count];

            for (int i = 0; i < segments.Count; ++i)
            {
                int max = segments.Count - 1;
                for (int j = i + 1; j < segments.Count; ++j)
                {
                    if (segments[i].Second.CalcDifference(segments[j].Second) > maxSegmentsDiffTolerance)
                    {
                        max = j - 1;
                        break;
                    }
                }

                maxAcceptedSegment[i] = max;
            }

            return maxAcceptedSegment;
        }

        private Pair<Point, Point>[] selectPointsToTrasform(int image1Index, int image2Index, List<Pair<Point, Point>> pointsMatch)
        {
            if (pointsMatch.Count >= 3)
            {
                List<Pair<Pair<Point, Point>, SegmentParams>> translatedPoints = getTranslatedPointsWithSegmentParams(pointsMatch, image1Index, image2Index);
                sortSegments(translatedPoints);
                int[] maxAcceptedSegment = getMaxAcceptedSegment(translatedPoints);
                List<List<Pair<TripleIndex, double>>> matches = new List<List<Pair<TripleIndex, double>>>();

                for (int i = 0; i < translatedPoints.Count - 2; ++i)
                {
                    int start = i;
                    int end = maxAcceptedSegment[i];
                    int sameAccepted = i + 1;
                    while (sameAccepted < maxAcceptedSegment.Length && maxAcceptedSegment[i] == maxAcceptedSegment[sameAccepted])
                    {
                        ++sameAccepted;
                    }

                    List<TripleIndex> combinations = new List<TripleIndex>();
                    if (end - start <= 20)
                    {
                        combinations = Utilities.GetThreeCombinations(start, end);
                    }
                    else
                    {
                        combinations = getBigSetCombinations(start, end, translatedPoints, image1Index, image2Index);
                    }

                    if (combinations.Count > 0)
                    {
                        matches.Add(new List<Pair<TripleIndex, double>>());
                        for (int j = 0; j < combinations.Count; ++j)
                        {
                            if (isAcceptedPointsCombination(combinations[j], translatedPoints))
                            {
                                Pair<Point, Point>[] triplePointsPairs = new Pair<Point, Point>[3];
                                triplePointsPairs[0] = translatedPoints[combinations[j].a].First;
                                triplePointsPairs[1] = translatedPoints[combinations[j].b].First;
                                triplePointsPairs[2] = translatedPoints[combinations[j].c].First;

                                matches[matches.Count - 1].Add(new Pair<TripleIndex, double>(combinations[j], calcYDistanceThreePointsPairs(triplePointsPairs)));
                            }
                        }
                        if (matches[matches.Count - 1].Count == 0)
                        {
                            matches.RemoveAt(matches.Count - 1);
                        }
                    }

                    i = sameAccepted - 1;
                }

                foreach (List<Pair<TripleIndex, double>> match in matches)
                {
                    match.Sort(
                        delegate(Pair<TripleIndex, double> match1, Pair<TripleIndex, double> match2)
                        {
                            return Math.Sign(match2.Second - match1.Second);
                        }
                    );
                }

                matches.Sort(
                        delegate(List<Pair<TripleIndex, double>> match1, List<Pair<TripleIndex, double>> match2)
                        {
                            return Math.Sign(match2[0].Second - match1[0].Second);
                        }
                    );

                TripleIndex result = null;

                int firstMatchesConfirmed = -1;
                int firstMatchIndex = -1;
                for (int i = 0; firstMatchesConfirmed == -1 && i < matches.Count; ++i)
                {
                    for (int j = 0; firstMatchesConfirmed == -1 && j < matches[i].Count && j < firstMatchSearchCount; ++j)
                    {
                        for (int k = 0; firstMatchesConfirmed == -1 && k < matches.Count; ++k)
                        {
                            if (i != k)
                            {
                                for (int l = 0; firstMatchesConfirmed == -1 && l < matches[k].Count && l < firstMatchSearchCount; ++l)
                                {
                                    if (arePointsDifferentiable(matches[i][j].First, matches[k][l].First, translatedPoints) &&
                                        compareTransforms(image1Index, image2Index, tripleIndexToPointsArray(matches[i][j].First, translatedPoints),
                                        tripleIndexToPointsArray(matches[k][l].First, translatedPoints)))
                                    {
                                        firstMatchesConfirmed = i;
                                        firstMatchIndex = j;
                                    }
                                }
                            }
                        }
                    }
                }

                if (firstMatchesConfirmed >= 0)
                {
                    result = matches[firstMatchesConfirmed][firstMatchIndex].First;
                }
                else // nie ma potwierdzenia najlepszego w innej grupie
                {
                    int[] confirmedIndexInGroup = new int[matches.Count];
                    for (int i = 0; i < confirmedIndexInGroup.Length; ++i)
                    {
                        confirmedIndexInGroup[i] = -1;
                    }

                    for (int i = 0; i < matches.Count; ++i)
                    {
                        for (int j = 0; confirmedIndexInGroup[i] == -1 && j < matches[i].Count; ++j)
                        {
                            for (int k = j + 1; confirmedIndexInGroup[i] == -1 && k <= j + sameGroupSearchMatch && k < matches[i].Count; ++k)
                            {
                                if (arePointsDifferentiable(matches[i][j].First, matches[i][k].First, translatedPoints) &&
                                    compareTransforms(image1Index, image2Index, tripleIndexToPointsArray(matches[i][j].First, translatedPoints),
                                    tripleIndexToPointsArray(matches[i][k].First, translatedPoints)))
                                {
                                    confirmedIndexInGroup[i] = j;
                                }
                            }
                        }
                    }

                    int bestGroupIndex = -1;
                    for (int i = 0; i < matches.Count; ++i)
                    {
                        if (confirmedIndexInGroup[i] >= 0)
                        {
                            if (bestGroupIndex == -1 || matches[i][confirmedIndexInGroup[i]].Second > matches[bestGroupIndex][confirmedIndexInGroup[bestGroupIndex]].Second)
                            {
                                bestGroupIndex = i;
                            }
                        }
                    }

                    if (bestGroupIndex >= 0)
                    {
                        result = matches[bestGroupIndex][confirmedIndexInGroup[bestGroupIndex]].First;
                    }
                }

                if (result != null)
                {
                    Pair<Point, Point>[] outPoints = new Pair<Point, Point>[3];
                    outPoints[0] = untranslatePair(translatedPoints[result.a].First, image1Index);
                    outPoints[1] = untranslatePair(translatedPoints[result.b].First, image1Index);
                    outPoints[2] = untranslatePair(translatedPoints[result.c].First, image1Index);

                    return outPoints;
                }
                else
                {
                    return null;
                }                
            }
            else
            {
                return null;
            }
        }

        private List<TripleIndex> getBigSetCombinations(int startPoint, int endPoint, List<Pair<Pair<Point, Point>, SegmentParams>> pointsPairs, 
            int image1Index, int image2Index)
        {
            List<TripleIndex> combinations = new List<TripleIndex>();

            List<Pair<Pair<Point, Point>, SegmentParams>> pointsCopy = new List<Pair<Pair<Point, Point>, SegmentParams>>();
            for (int i = startPoint; i <= endPoint; ++i)
            {
                pointsCopy.Add(pointsPairs[i]);
            }

            pointsCopy.Sort(
                delegate(Pair<Pair<Point, Point>, SegmentParams> segment1, Pair<Pair<Point, Point>, SegmentParams> segment2)
                {
                    Point upperLeft = new Point(0, 0);
                    Point upperRight = new Point(images[image1Index].Second.GetWidth() + images[image2Index].Second.GetWidth(), 0);

                    return Math.Sign(((Utilities.CalcDistanceBetweenPoints(upperLeft, segment1.First.First) + Utilities.CalcDistanceBetweenPoints(upperLeft, segment1.First.Second)) +
                                     (Utilities.CalcDistanceBetweenPoints(upperRight, segment1.First.First) + Utilities.CalcDistanceBetweenPoints(upperRight, segment1.First.Second))) -
                                     ((Utilities.CalcDistanceBetweenPoints(upperLeft, segment2.First.First) + Utilities.CalcDistanceBetweenPoints(upperLeft, segment2.First.Second)) +
                                     (Utilities.CalcDistanceBetweenPoints(upperRight, segment2.First.First) + Utilities.CalcDistanceBetweenPoints(upperRight, segment2.First.Second))));

                }
            );

            int[] newIndices = new int[endPoint - startPoint + 1];
            for (int i = 0; i < pointsCopy.Count; ++i)
            {
                int index = -1;
                for (int j = startPoint; index == -1 && j <= endPoint; ++j)
                {
                    if (pointsCopy[i].First.Equals(pointsPairs[j].First))
                    {
                        index = j;
                    }
                }

                newIndices[i] = index;
            }

            const int maxCombinationsPerIteration = 10;
            int iterationSetCount = Math.Min(pointsCopy.Count / 10, maxCombinationsPerIteration);

            for (int i = 0; i < iterationSetCount; ++i)
            {
                for (int j = pointsCopy.Count / 2 - iterationSetCount / 2; j < pointsCopy.Count / 2 + iterationSetCount / 2; ++j)
                {
                    for (int k = pointsCopy.Count - iterationSetCount; k < pointsCopy.Count; ++k)
                    {
                        combinations.Add(new TripleIndex(newIndices[i], newIndices[j], newIndices[k]));
                    }
                }
            }

            for (int i = 0; i < pointsCopy.Count; ++i)
            {
                int lIndex = Math.Max(0, i - pointsCopy.Count / 3);
                int gIndex = Math.Min(pointsCopy.Count - 1, i + pointsCopy.Count / 3);
                if (lIndex == i)
                {
                    ++lIndex;
                }
                if (gIndex == i)
                {
                    --gIndex;
                }

                combinations.Add(new TripleIndex(newIndices[i], newIndices[lIndex], newIndices[gIndex]));
            }

            const int surroundPointsCount = 5;
            int step = Math.Max(1, pointsCopy.Count / 10);

            for (int i = 0; i < pointsCopy.Count; i += step)
            {
                int startPoint1 = Math.Max(0, i - surroundPointsCount);
                int endPoint1 = Math.Min(i + surroundPointsCount, pointsCopy.Count - 1);

                for (int j = startPoint1; j <= endPoint1; ++j)
                {
                    int startPoint2 = Math.Max(0, j - surroundPointsCount);
                    int endPoint2 = Math.Min(j + surroundPointsCount, pointsCopy.Count - 1);

                    for (int k = startPoint2; k <= endPoint2; ++k)
                    {
                        combinations.Add(new TripleIndex(newIndices[i], newIndices[j], newIndices[k]));
                    }
                }
            }

            return combinations;
        }

        private bool isAcceptedPointsCombination(TripleIndex combination, List<Pair<Pair<Point, Point>, SegmentParams>> points)
        {
            Matrix firstPointsMatrix = new Matrix(3, 3);
            Matrix secondPointsMatrix = new Matrix(3, 3);

            firstPointsMatrix[0, 0] = points[combination.a].First.First.X;
            firstPointsMatrix[0, 1] = points[combination.a].First.First.Y;
            firstPointsMatrix[0, 2] = 1;
            firstPointsMatrix[1, 0] = points[combination.b].First.First.X;
            firstPointsMatrix[1, 1] = points[combination.b].First.First.Y;
            firstPointsMatrix[1, 2] = 1;
            firstPointsMatrix[2, 0] = points[combination.c].First.First.X;
            firstPointsMatrix[2, 1] = points[combination.c].First.First.Y;
            firstPointsMatrix[2, 2] = 1;

            secondPointsMatrix[0, 0] = points[combination.a].First.Second.X;
            secondPointsMatrix[0, 1] = points[combination.a].First.Second.Y;
            secondPointsMatrix[0, 2] = 1;
            secondPointsMatrix[1, 0] = points[combination.b].First.Second.X;
            secondPointsMatrix[1, 1] = points[combination.b].First.Second.Y;
            secondPointsMatrix[1, 2] = 1;
            secondPointsMatrix[2, 0] = points[combination.c].First.Second.X;
            secondPointsMatrix[2, 1] = points[combination.c].First.Second.Y;
            secondPointsMatrix[2, 2] = 1;

            return Math.Abs(Utilities.Det3x3Matrix(firstPointsMatrix)) > 0.00000001 &&
                  Math.Abs(Utilities.Det3x3Matrix(secondPointsMatrix)) > 0.00000001;
        }

        private bool arePointsDifferentiable(TripleIndex firstPoints, TripleIndex secondPoints, List<Pair<Pair<Point, Point>, SegmentParams>> points)
        {
            return points[firstPoints.a].First.First != points[secondPoints.a].First.First &&
                   points[firstPoints.a].First.First != points[secondPoints.b].First.First &&
                   points[firstPoints.a].First.First != points[secondPoints.c].First.First &&
                   points[firstPoints.b].First.First != points[secondPoints.a].First.First &&
                   points[firstPoints.b].First.First != points[secondPoints.b].First.First &&
                   points[firstPoints.b].First.First != points[secondPoints.c].First.First &&
                   points[firstPoints.c].First.First != points[secondPoints.a].First.First &&
                   points[firstPoints.c].First.First != points[secondPoints.b].First.First &&
                   points[firstPoints.c].First.First != points[secondPoints.c].First.First &&

                   points[firstPoints.a].First.Second != points[secondPoints.a].First.Second &&
                   points[firstPoints.a].First.Second != points[secondPoints.b].First.Second &&
                   points[firstPoints.a].First.Second != points[secondPoints.c].First.Second &&
                   points[firstPoints.b].First.Second != points[secondPoints.a].First.Second &&
                   points[firstPoints.b].First.Second != points[secondPoints.b].First.Second &&
                   points[firstPoints.b].First.Second != points[secondPoints.c].First.Second &&
                   points[firstPoints.c].First.Second != points[secondPoints.a].First.Second &&
                   points[firstPoints.c].First.Second != points[secondPoints.b].First.Second &&
                   points[firstPoints.c].First.Second != points[secondPoints.c].First.Second;
        }

        private Pair<Point, Point>[] tripleIndexToPointsArray(TripleIndex tripleIndex, List<Pair<Pair<Point, Point>, SegmentParams>> points)
        {
            Pair<Point, Point>[] outPoints = new Pair<Point, Point>[3];
            outPoints[0] = new Pair<Point, Point>(points[tripleIndex.a].First.First, points[tripleIndex.a].First.Second);
            outPoints[1] = new Pair<Point, Point>(points[tripleIndex.b].First.First, points[tripleIndex.b].First.Second);
            outPoints[2] = new Pair<Point, Point>(points[tripleIndex.c].First.First, points[tripleIndex.c].First.Second);

            return outPoints;
        }

        private bool compareTransforms(int image1Index, int image2Index, Pair<Point, Point>[] selectedPoints1, Pair<Point, Point>[] selectedPoints2)
        {
            const int pointsDimCount = 5;
            int xDiff = images[image2Index].Second.GetWidth() / 5;
            int yDiff = images[image2Index].Second.GetHeight() / 5;

            Point[] testPoints = new Point[pointsDimCount * pointsDimCount];

            for (int i = 0; i < pointsDimCount; ++i)
            {
                for (int j = 0; j < pointsDimCount; ++j)
                {
                    testPoints[i * pointsDimCount + j] = new Point(i * xDiff, j * yDiff);
                }
            }

            Transformation transform1 = new Transformation(selectedPoints1);
            Transformation transform2 = new Transformation(selectedPoints2);

            double transformationsDifference = calcDifferenceBetweenTransforms(transform1, transform2, testPoints);

            return transformationsDifference <= maxTransformationDiffTolerance;
        }

        private double calcDifferenceBetweenTransforms(Transformation transform1, Transformation transform2, Point[] testPoints)
        {
            Point[] transformedPoints1 = transform1.TransformPoints(testPoints);
            Point[] transformedPoints2 = transform2.TransformPoints(testPoints);

            double distance = 0;

            for (int i = 0; i < transformedPoints1.Length; ++i)
            {
                distance += Utilities.CalcDistanceBetweenPoints(transformedPoints1[i], transformedPoints2[i]);
            }

            return distance;
        }

        private List<Pair<Pair<Point, Point>, SegmentParams>> getTranslatedPointsWithSegmentParams(List<Pair<Point, Point>> inPoints, int referenceImageIndex, int secondImage)
        {
            int widthAdd = images[referenceImageIndex].Second.GetWidth();

            List<Pair<Pair<Point, Point>, SegmentParams>> translatedPoints = new List<Pair<Pair<Point, Point>, SegmentParams>>(inPoints.Count);
            for (int i = 0; i < inPoints.Count; ++i)
            {
                translatedPoints.Add(new Pair<Pair<Point, Point>, SegmentParams>());
                translatedPoints[i].First = new Pair<Point, Point>();
                translatedPoints[i].First.First = new Point(inPoints[i].First.X, inPoints[i].First.Y);
                translatedPoints[i].First.Second = new Point(inPoints[i].Second.X + widthAdd, inPoints[i].Second.Y);
                translatedPoints[i].Second = new SegmentParams(Utilities.RadToDeg(Utilities.CalcSegmentAngle(translatedPoints[i].First)),
                    Utilities.CalcDistanceBetweenPoints(translatedPoints[i].First.First, translatedPoints[i].First.Second),
                    Utilities.CalcDistanceBetweenPoints(new Point(0, 0), new Point(images[referenceImageIndex].Second.GetWidth() +
                        images[secondImage].Second.GetWidth(), Math.Max(images[referenceImageIndex].Second.GetHeight(),
                        images[secondImage].Second.GetHeight()))));
            }

            return translatedPoints;
        }

        private Pair<Point, Point> untranslatePair(Pair<Point, Point> pair, int referenceImageIndex)
        {
            int widthAdd = images[referenceImageIndex].Second.GetWidth();

            return new Pair<Point, Point>(pair.First, new Point(pair.Second.X - widthAdd, pair.Second.Y));
        }

        private double calcDistanceTwoPointsPairs(Pair<Point, Point> pair1, Pair<Point, Point> pair2)
        {
            return Utilities.CalcDistanceBetweenPoints(pair1.First, pair2.First) + Utilities.CalcDistanceBetweenPoints(pair1.Second, pair2.Second);
        }

        private double calcDistanceThreePointsPairs(Pair<Point, Point>[] points)
        {
            double distance = Math.Min(Math.Min(calcDistanceTwoPointsPairs(points[0], points[1]), calcDistanceTwoPointsPairs(points[0], points[2])),
                calcDistanceTwoPointsPairs(points[1], points[2]));

            return distance;
        }

        private double calcYDistanceTwoPointsPairs(Pair<Point, Point> pair1, Pair<Point, Point> pair2)
        {
            return Math.Abs(pair1.First.Y - pair2.First.Y) + Math.Abs(pair1.Second.Y - pair2.Second.Y);
        }

        private double calcYDistanceThreePointsPairs(Pair<Point, Point>[] points)
        {
            double distance = Math.Min(Math.Min(calcYDistanceTwoPointsPairs(points[0], points[1]), calcYDistanceTwoPointsPairs(points[0], points[2])),
                calcYDistanceTwoPointsPairs(points[1], points[2]));

            return distance;
        }
    }
}