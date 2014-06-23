//#define BLUR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace ImageStitching
{
    public partial class PanoramaCreator
    {
        private List<Pair<string, MyImage>> images;
        private MyImage panorama;

        public PanoramaCreator()
        {
            images = new List<Pair<string, MyImage>>();
            panorama = null;
        }

        private bool checkIfImageExist(string path)
        {
            foreach (Pair<string, MyImage> image in images)
            {
                if (image.First == path)
                {
                    return true;
                }
            }

            return false;
        }

        public void ClearImages()
        {
            images.Clear();
        }

        public void RemoveImage(string path)
        {
            for (int i = 0; i < images.Count; ++i)
            {
                if (images[i].First == path)
                {
                    images.RemoveAt(i);
                }
            }
        }
        
        public void RemoveImage(int index)
        {
            images.RemoveAt(index);
        }

        public bool AddImage(string path)
        {
            if (checkIfImageExist(path) == false)
            {
                MyImage image = new MyImage(path);
                images.Add(new Pair<string, MyImage>(path, image));

                return true;
            }
            else
            {
                return false;
            }
        }

        public int GetImagesCount()
        {
            return images.Count;
        }

        public MyImage GetImage(int index)
        {
            return images[index].Second;
        }

        public string GetImagePath(int index)
        {
            return images[index].First;
        }

        /// <summary>
        /// Zwraca informację o błędzie podczas tworzenia panoramy
        /// </summary>
        public string StitchImages(bool ifDrawPointsMatch, out MyImage outPanorama, out MyImage outPointsMatch)
        {
            List<Pair<Point, Point>> pointsMatch = null;
            if (GetImagesCount() >= 2)
            {
                PointsMatcher.MatchPoints(GetImagePath(0), GetImagePath(1), out pointsMatch);
            }

            outPointsMatch = null;
            outPanorama = null;

            Pair<Point, Point>[] matchedPointsFirstImages = null;
            string result = GetImagesCount() >= 1 ? createPanorama(out outPanorama, pointsMatch, out matchedPointsFirstImages) : "Brak wczytanych obrazów";

            if (ifDrawPointsMatch && GetImagesCount() >= 2)
            {
                outPointsMatch = drawPointsMatch(0, 1, pointsMatch, matchedPointsFirstImages);
            }

            return result;
        }

        private MyImage drawPointsMatch(int image1Index, int image2Index, List<Pair<Point, Point>> pointsMatch, Pair<Point, Point>[] selectedPoints)
        {
            const int interspacePixels = 10;

            MyImage image1 = images[image1Index].Second;
            MyImage image2 = images[image2Index].Second;

            MyImage outImage = image1.Clone();
            outImage.SetPixels(outImage.GetWidth(), 0, interspacePixels, outImage.GetHeight(), new PixelColor(0, 255, 255, 255));
            outImage.InsertImage(outImage.GetWidth(), 0, image2);

            List<Pair<Point, Point>> notMatchedPoints = new List<Pair<Point, Point>>();
            List<Pair<Point, Point>> matchedPoints = new List<Pair<Point, Point>>();
            int matchFoundCount = selectedPoints != null ? 0 : 3;
            foreach (Pair<Point, Point> pair in pointsMatch)
            {
                bool foundMatch = false;
                for (int i = 0; matchFoundCount < 3 && i < selectedPoints.Length && foundMatch == false; ++i)
                {
                    if (pair.Equals(selectedPoints[i]))
                    {
                        foundMatch = true;
                    }
                }

                pair.Second = new Point(image1.GetWidth() + interspacePixels + pair.Second.X, pair.Second.Y);

                if (foundMatch == true)
                {
                    matchedPoints.Add(pair);
                }
                else
                {
                    notMatchedPoints.Add(pair);         
                }
            }

            outImage.DrawLines(matchedPoints, System.Drawing.Color.FromArgb(39, 255, 36));
            outImage.DrawLines(notMatchedPoints, System.Drawing.Color.Red);

            return outImage;
        }

        /// <summary>
        /// Zwraca informację o błędzie podczas tworzenia panoramy
        /// </summary>
        private string createPanorama(out MyImage outImage, List<Pair<Point, Point>> firstPointsMatch, out Pair<Point, Point>[] outMatchedPointsFirstImages)
        {
            outMatchedPointsFirstImages = null;
            
            string result = "";
            Transformation [] transformationsArray = new Transformation [images.Count];
            Pair<int, int> minusShift = new Pair<int, int>(0, 0);
            /////////int [] imagesBrightness = new int[images.Count];

            /// Petla 1:
            /// Sprawdzamy jasnosc kazdego obrazu, pozniej liczymy srednia
            /// System.Console.WriteLine("petla 1");
            ////////for (int i = 0; i < images.Count - 1; ++i)
            ////////{
            ////////    int image1Index = i;
            ////////    int image2Index = i + 1;

            ////////    BrightnessEqualizer imageBrightness = new BrightnessEqualizer(images[image2Index].Second);
            ////////    if (i == 0)
            ////////    {
            ////////        BrightnessEqualizer image0Brightness = new BrightnessEqualizer(images[image1Index].Second);
            ////////        imagesBrightness[i] = image0Brightness.getAvaregeBrightness();
            ////////        imagesBrightness[i + 1] = imageBrightness.getAvaregeBrightness();
            ////////    }
            ////////    else
            ////////    {
            ////////        imagesBrightness[i + 1] = imageBrightness.getAvaregeBrightness();
            ////////    }
            ////////}

            ////////int imagesBrightnessSum = 0;
            ////////int imagesAvaregeBrightness = 0;
            ////////for (int i = 0; i < imagesBrightness.Length; i++)
            ////////{
            ////////    imagesBrightnessSum += imagesBrightness[i];
            ////////}
            ////////imagesAvaregeBrightness = (int)(imagesBrightnessSum / imagesBrightness.Length);
            //System.Console.WriteLine("srednia 1 "+imagesAvaregeBrightness);
            
            /// Petla 2:
            /// Wybrane obrazy zastepujemy nowymi z nowymi, usrednionymi jasnosciami
            /// System.Console.WriteLine("petla 2");
            ////////for (int i = 0; i < images.Count; i++)
            ////////{
            ////////    //MyImage newBrightnessImage = new MyImage();
            ////////    BrightnessEqualizer bE = new BrightnessEqualizer(imagesAvaregeBrightness, imagesBrightness[i], images[i].Second);//.Clone());
            ////////    //newBrightnessImage = bE.newBrightnessImage.Clone();
            ////////    //images[i].Second = (bE.newBrightnessImage.Clone()); //newBrightnessImage;
            ////////}
            //////////////////////////////////////////
            //for (int i = 0; i < images.Count - 1; ++i)
            //{
            //    int image1Index = i;
            //    int image2Index = i + 1;

            //    BrightnessEqualizer imageBrightness = new BrightnessEqualizer(images[image2Index].Second);
            //    if (i == 0)
            //    {
            //        BrightnessEqualizer image0Brightness = new BrightnessEqualizer(images[image1Index].Second);
            //        imagesBrightness[i] = image0Brightness.getAvaregeBrightness();
            //        imagesBrightness[i + 1] = imageBrightness.getAvaregeBrightness();
            //    }
            //    else
            //    {
            //        imagesBrightness[i + 1] = imageBrightness.getAvaregeBrightness();
            //    }
            //}

            //imagesBrightnessSum = 0;
            //imagesAvaregeBrightness = 0;
            //for (int i = 0; i < imagesBrightness.Length; i++)
            //{
            //    imagesBrightnessSum += imagesBrightness[i];
            //    System.Console.WriteLine("brightness 2 " + i + ": " + imagesBrightness[i]);
            //}
            //imagesAvaregeBrightness = (int)(imagesBrightnessSum / imagesBrightness.Length);
            //System.Console.WriteLine("srednia 2 " + imagesAvaregeBrightness);
            outImage = images[0].Second.Clone();

            /// Petla 3:
            /// Laczymy obrazy w panorame
            /// System.Console.WriteLine("petla 3");
            for (int i = 0; i < images.Count - 1; ++i)
            {
                int image1Index = i;
                int image2Index = i + 1;
                //float[,] image1PixelWeights;
                int blendParam = 20; // 2*blendParam - obszar rozmycia przy złaczeniach
                int[] partitionLinePixIdx;  //indeksy pikseli gdzie obrazy na siebie nachodza [linia podzialu]
                //BrightnessEqualizer pixelWeights = new BrightnessEqualizer(images[image1Index].Second);
                BrightnessEqualizer bEqualizer = new BrightnessEqualizer();

                List<Pair<Point, Point>> pointsMatch;
                if (i == 0)
                {
                    pointsMatch = firstPointsMatch;
                }
                else
                {
                    PointsMatcher.MatchPoints(GetImagePath(image1Index), GetImagePath(image2Index), out pointsMatch);
                }

                Pair<Point, Point>[] selectedPoints = selectPointsToTrasform(image1Index, image2Index, pointsMatch);
                if (i == 0)
                {
                    outMatchedPointsFirstImages = selectedPoints;
                }
                try
                {
                    selectedPoints[2].First.Equals(selectedPoints[2].Second);
                }
                catch (Exception)
                {
                    result = "Nie wykryto pokrywających się obszarów.";
                    break;
                }

                Pair<Point, Point>[] selectedPointsCopy = new Pair<Point, Point>[3];
                for (int j = 0; j < selectedPoints.Length; ++j)
                {
                    selectedPointsCopy[j] = new Pair<Point, Point>(new Point(selectedPoints[j].Second.X, selectedPoints[j].Second.Y),
                        new Point(selectedPoints[j].First.X, selectedPoints[j].First.Y));
                }

                Transformation transformation = new Transformation(selectedPointsCopy);
                transformationsArray[i] = transformation;
                               
                MyImage imageSpace = images[image2Index].Second;

                Point[] corners = new Point[4];
                corners[0] = new Point(0, 0);
                corners[1] = new Point(0, imageSpace.GetHeight());
                corners[2] = new Point(imageSpace.GetWidth(), 0);
                corners[3] = new Point(imageSpace.GetWidth(), imageSpace.GetHeight());
                for (int j = i; j >= 0; j--)
                {
                    corners = transformationsArray[j].TransformPoints(corners);
                }

                int minX = Math.Min((int)Math.Round(corners[0].X), Math.Min((int)Math.Round(corners[1].X),
                    Math.Min((int)Math.Round(corners[2].X), (int)Math.Round(corners[3].X)))) - minusShift.First;
                int maxX = Math.Max((int)Math.Round(corners[0].X), Math.Max((int)Math.Round(corners[1].X),
                    Math.Max((int)Math.Round(corners[2].X), (int)Math.Round(corners[3].X)))) - minusShift.First;
                int minY = Math.Min((int)Math.Round(corners[0].Y), Math.Min((int)Math.Round(corners[1].Y),
                    Math.Min((int)Math.Round(corners[2].Y), (int)Math.Round(corners[3].Y)))) - minusShift.Second;
                int maxY = Math.Max((int)Math.Round(corners[0].Y), Math.Max((int)Math.Round(corners[1].Y),
                    Math.Max((int)Math.Round(corners[2].Y), (int)Math.Round(corners[3].Y)))) - minusShift.Second;

                ///nowy obraz po transformacji
                PixelColor[,] newPictureColors = new PixelColor[maxX - minX + 1, maxY - minY + 1];
                
                for (int j = 0; j < maxX - minX + 1; j++)
                {
                    for (int k = 0; k < maxY - minY + 1; k++)
                    {
                        int outImageX = j + minX;
                        int outImageY = k + minY;

                        if (outImageX >= 0 && outImageX < outImage.GetWidth() && outImageY >= 0 && outImageY < outImage.GetHeight())
                        {
                            newPictureColors[j, k] = outImage.GetPixel(outImageX, outImageY);
                            //image1PixelWeights[j, k] = image1PixelWeights[outImageX, outImageY];
                        }
                        else
                        {
                            newPictureColors[j, k] = new PixelColor(0, 255, 255, 255);
                            //image1PixelWeights[j, k] = 0;
                        }
                    }
                }

                PointInt[,] coordsTable = new PointInt[images[image1Index].Second.GetWidth(), images[image1Index].Second.GetHeight()];
                for (int j = 0; j < coordsTable.GetLength(0); ++j)
                {
                    for (int k = 0; k < coordsTable.GetLength(1); ++k)
                    {
                        Point secondImagePixel = transformationsArray[i].InverseTransformPoint(new Point(j, k));
                        PointInt secondImagePixelInt = new PointInt((int)Math.Round(secondImagePixel.X), (int)Math.Round(secondImagePixel.Y));

                        if ((secondImagePixelInt.x >= 0 && secondImagePixelInt.x < images[image2Index].Second.GetWidth() &&
                             secondImagePixelInt.y >= 0 && secondImagePixelInt.y < images[image2Index].Second.GetHeight()) == false)
                        {
                            secondImagePixelInt.x = secondImagePixelInt.y = -1;
                        }

                        coordsTable[j, k] = secondImagePixelInt;
                    }
                }

#if !BLUR
                int[,] pixelsSource;
                findPartitionLine(images[image1Index].Second, images[image2Index].Second, coordsTable, out pixelsSource);
                
                //image1PixelWeights = new float[pixelsSource.GetLength(0) + blendParam, pixelsSource.GetLength(1)];
                partitionLinePixIdx = new int[pixelsSource.GetLength(1)];
                
                //for (int j = 0; j < image1PixelWeights.GetLength(0); j++)
                //{
                //    for (int k = 0; k < image1PixelWeights.GetLength(1); k++)
                //    {
                //        image1PixelWeights[j, k] = 0;
                //    }
                //}
                
#endif

                int sourceImageWidth = images[image2Index].Second.GetWidth();
                int sourceImageHeight = images[image2Index].Second.GetHeight();

                for (int j = 0; j < pixelsSource.GetLength(1); ++j) // tworzymy tablice wag 
                {
                    int image2Start = -1;
                    for (int k = 0; k < pixelsSource.GetLength(0); ++k)
                    {
                        if (pixelsSource[k, j] == 2 && image2Start == -1)
                        {
                            image2Start = k;
                            partitionLinePixIdx[j] = k;
                            //float brightnessChangeValue = (float)(1.0 / (blendParam));
                            //int h = 0;
                            //for (int l = image2Start; l < image2Start + blendParam; l++)
                            //{
                            //    image1PixelWeights[l, j] = (blendParam - (l - image2Start)) * brightnessChangeValue;
                            //    image1PixelWeights[l - 1 - h, j] = (blendParam - (l - image2Start)) * brightnessChangeValue;
                            //    h += 2;
                            //}
                            break;
                        }
                        //else if (pixelsSource[k, j] != 2 && image2Start == -1)
                        //{
                        //    //partitionLinePixIdx[j] = -1;
                        //}
                    }
                }
                for (int j = 0; j < newPictureColors.GetLength(1); ++j)
                {

                    for (int k = 0; k < newPictureColors.GetLength(0); ++k)
                    {
#if !BLUR
                        Point sourceFirst = new Point(minX + k + minusShift.First, minY + j + minusShift.Second);
                        for (int l = 0; l < i; ++l)
                        {
                            sourceFirst = transformationsArray[l].InverseTransformPoint(sourceFirst);
                        }

                        if (((int)sourceFirst.X >= 0 && (int)sourceFirst.X < pixelsSource.GetLength(0) &&
                            (int)sourceFirst.Y >= 0 && (int)sourceFirst.Y < pixelsSource.GetLength(1)) == false ||
                            pixelsSource[(int)sourceFirst.X, (int)sourceFirst.Y] == 2)
                        {
                            Point source = transformationsArray[i].InverseTransformPoint(sourceFirst);

                            //double xCenterDist = source.X - ((int)source.X + 0.5f);
                            //double yCenterDist = source.Y - ((int)source.Y + 0.5f);

                            if ((int)source.X >= 0 && (int)source.X < sourceImageWidth && (int)source.Y >= 0 && (int)source.Y < sourceImageHeight)
                            {
                                newPictureColors[k, j] = imageSpace.GetPixel((int)source.X, (int)source.Y);
                            }
                        }
#else
                        Point source = new Point(minX + j + minusShift.First, minY + k + minusShift.Second);
                        for (int l = 0; l <= i; ++l)
                        {
                            source = transformationsArray[l].InverseTransformPoint(source);
                        }

                        //double xCenterDist = source.X - ((int)source.X + 0.5f);
                        //double yCenterDist = source.Y - ((int)source.Y + 0.5f);

                        if ((int)source.X >= 0 && (int)source.X < sourceImageWidth && (int)source.Y >= 0 && (int)source.Y < sourceImageHeight)
                        {
                            newPictureColors[j, k] = imageSpace.GetPixel((int)source.X, (int)source.Y);
                        }

                        int xIndexOutImage = minX + j;
                        int yIndexOutImage = minY + k;

                        if (xIndexOutImage >= 0 && xIndexOutImage < outImage.GetWidth() && yIndexOutImage >= 0 && yIndexOutImage < outImage.GetHeight() &&
                            outImage.GetAlpha(xIndexOutImage, yIndexOutImage) > 0)
                        {
                            newPictureColors[j, k] =
                            new PixelColor(255, (byte)((newPictureColors[j, k].red + outImage.GetRed(xIndexOutImage, yIndexOutImage)) / 2),
                                                (byte)((newPictureColors[j, k].green + outImage.GetGreen(xIndexOutImage, yIndexOutImage)) / 2),
                                                (byte)((newPictureColors[j, k].blue + outImage.GetBlue(xIndexOutImage, yIndexOutImage)) / 2));
                        }

#endif
                    }

                }
                
                if (minX < 0)
                {
                    minusShift.First += minX;
                }
                if (minY < 0)
                {
                    minusShift.Second += minY;
                }

                outImage.SetPixels(minX, minY, newPictureColors);
                
                for (int j = 0; j < partitionLinePixIdx.Length; j++)
                {
                    //int bright1Left = -1, bright1Right = -1, bright2Left = -1, bright2Right = -1;
                    int moveX = 0, moveY = 0;

                    if (minX >= 0 && partitionLinePixIdx[j]!=0 && minY < 0)
                    {
                        moveX = 0;
                        moveY = -minY;
                    }
                    else if (minX>= 0 && minY>=0 && partitionLinePixIdx[j] != 0)
                    {
                        moveX = 0;
                        moveY = 0;
                    }
                    else if (minX < 0 && minY < 0 && partitionLinePixIdx[j] != 0)
                    {
                        moveX = -minX;
                        moveY = -minY;
                    }
                    else if (minX >= 0 && minY >= 0 && partitionLinePixIdx[j] != 0)
                    {
                        moveX = -minX;
                        moveY = 0;
                    }

                    for (int k = 0; k <  blendParam/2; k++)
                    {
                        Point sourceFirst = new Point(k, j);//(minX + k + minusShift.First, minY + j + minusShift.Second);

                        if(partitionLinePixIdx[j]>0)
                        {
                            PixelColor newPix = new PixelColor(255, 123, 1, 255);
                            newPix = outImage.GetPixel((partitionLinePixIdx[j]+moveX-k), j+moveY);
                            PixelColor newPix1 = outImage.GetPixel((partitionLinePixIdx[j]+moveX -k-1), j+moveY);
                            newPix = bEqualizer.BrightnessChange(newPix, newPix1);
                            //newPix = new PixelColor(255, 255, 0, 0);
                            outImage.SetPixel((partitionLinePixIdx[j]+moveX -k), j+moveY, newPix); //na lewo od polaczenia
                            
                            PixelColor newPix3 = new PixelColor(255, 1, 255, 255);
                            newPix3 = outImage.GetPixel((partitionLinePixIdx[j] + moveX +k - 1), j + moveY);
                            PixelColor newPix4 = outImage.GetPixel((partitionLinePixIdx[j] + moveX + k), j + moveY);
                            newPix3 = bEqualizer.BrightnessChange(newPix3, newPix4);
                            outImage.SetPixel((partitionLinePixIdx[j] + moveX + k-1), j + moveY, newPix3); // na prawo od polaczenia

                        }
                    }
                }
            }

            //int imagesBrightnessSum = 0;
            //int imagesAvaregeBrightness = 0;
            //for (int i = 0; i < imagesBrightness.Length; i++)
            //{
            //    imagesBrightnessSum += imagesBrightness[i];
            //}
            //imagesAvaregeBrightness = (int)(imagesBrightnessSum / imagesBrightness.Length);
            panorama = outImage;
            return result;
        }

        public MyImage GetPanorama()
        {
            return panorama;
        }
    }
}
