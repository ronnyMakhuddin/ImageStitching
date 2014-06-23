using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ImageStitching
{
    public class Transformation
    {
        private Matrix affineTransform;
        private Matrix inverseTransform;
        private Matrix inPoint;
        private Matrix outPoint;

        private void initPoints()
        {
            inPoint = new Matrix(3, 1);
            inPoint[2, 0] = 1;
            outPoint = new Matrix(3, 1);
            outPoint[2, 0] = 1;
        }

        public Transformation(Pair<Point, Point>[] pointsMatch)
        {
            initPoints();

            Matrix firstMatrix = Matrix.ZeroMatrix(6, 6);
            firstMatrix[0, 0] = pointsMatch[0].First.X;
            firstMatrix[0, 1] = pointsMatch[0].First.Y;
            firstMatrix[0, 2] = 1;
            firstMatrix[1, 0] = pointsMatch[1].First.X;
            firstMatrix[1, 1] = pointsMatch[1].First.Y;
            firstMatrix[1, 2] = 1;
            firstMatrix[2, 0] = pointsMatch[2].First.X;
            firstMatrix[2, 1] = pointsMatch[2].First.Y;
            firstMatrix[2, 2] = 1;
            firstMatrix[3, 3] = pointsMatch[0].First.X;
            firstMatrix[3, 4] = pointsMatch[0].First.Y;
            firstMatrix[3, 5] = 1;
            firstMatrix[4, 3] = pointsMatch[1].First.X;
            firstMatrix[4, 4] = pointsMatch[1].First.Y;
            firstMatrix[4, 5] = 1;
            firstMatrix[5, 3] = pointsMatch[2].First.X;
            firstMatrix[5, 4] = pointsMatch[2].First.Y;
            firstMatrix[5, 5] = 1;
            firstMatrix = firstMatrix.Invert();

            Matrix secondMatrix = new Matrix(6, 1);
            secondMatrix[0, 0] = pointsMatch[0].Second.X;
            secondMatrix[1, 0] = pointsMatch[1].Second.X;
            secondMatrix[2, 0] = pointsMatch[2].Second.X;
            secondMatrix[3, 0] = pointsMatch[0].Second.Y;
            secondMatrix[4, 0] = pointsMatch[1].Second.Y;
            secondMatrix[5, 0] = pointsMatch[2].Second.Y;

            Matrix parametersMatrix = firstMatrix * secondMatrix;

            affineTransform = new Matrix(3, 3);
            affineTransform[0, 0] = parametersMatrix[0, 0];
            affineTransform[0, 1] = parametersMatrix[1, 0];
            affineTransform[0, 2] = parametersMatrix[2, 0];
            affineTransform[1, 0] = parametersMatrix[3, 0];
            affineTransform[1, 1] = parametersMatrix[4, 0];
            affineTransform[1, 2] = parametersMatrix[5, 0];
            affineTransform[2, 0] = 0;
            affineTransform[2, 1] = 0;
            affineTransform[2, 2] = 1;

            inverseTransform = affineTransform.Invert();
        }

        public Transformation(Matrix affineTransform)
        {
            initPoints();
            this.affineTransform = affineTransform;
        }

        public Point TransformPoint(Point point)
        {
            inPoint[0, 0] = point.X;
            inPoint[1, 0] = point.Y;

            outPoint = affineTransform * inPoint;
            return new Point(outPoint[0, 0], outPoint[1, 0]);
        }

        public Point[] TransformPoints(Point[] points)
        {
            Point[] outPoints = new Point[points.Length];

            for (int i = 0; i < points.Length; ++i)
            {
                outPoints[i] = TransformPoint(points[i]);
            }

            return outPoints;
        }

        public Point InverseTransformPoint(Point point)
        {
            inPoint[0, 0] = point.X;
            inPoint[1, 0] = point.Y;

            outPoint = inverseTransform * inPoint;
            return new Point(outPoint[0, 0], outPoint[1, 0]);
        }

        public Point[] InverseTransformPoints(Point[] points)
        {
            Point[] outPoints = new Point[points.Length];

            for (int i = 0; i < points.Length; ++i)
            {
                outPoints[i] = InverseTransformPoint(points[i]);
            }

            return outPoints;
        }
    }
}
