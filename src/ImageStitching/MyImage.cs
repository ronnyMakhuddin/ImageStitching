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
    public class PixelColor
    {
        public byte alpha;
        public byte red;
        public byte green;
        public byte blue;

        public PixelColor(byte alpha, byte red, byte green, byte blue)
        {
            this.alpha = alpha;
            this.red = red;
            this.green = green;
            this.blue = blue;
        }
    }

    public class MyImage
    {
        private WriteableBitmap bitmap;
        private byte[] pixels;
        private int stride;

        private static int calcPixelIndex(int x, int y, int width)
        {
            return 4 * (y * width + x);
        }

        public void changePixels(PixelColor[,] colors)
        {
            int width = colors.GetLength(0);
            int height = colors.GetLength(1);
            byte[] pColors = new byte[width * 4 * height];

            for (int i = 0; i < height; ++i)
            {
                for (int j = 0; j < width; ++j)
                {
                    int startIndex = calcPixelIndex(j, i, colors.GetLength(0));
                    pColors[startIndex] = colors[j, i].blue;
                    pColors[startIndex + 1] = colors[j, i].green;
                    pColors[startIndex + 2] = colors[j, i].red;
                    pColors[startIndex + 3] = colors[j, i].alpha;
                }
            }
            pixels = pColors;
        }

        private static int calcStride(int width)
        {
            return width * 4;
        }

        private void setStride()
        {
            stride = bitmap.PixelWidth * 4;
        }

        private void getPixelsData()
        {
            setStride();
            int size = bitmap.PixelHeight * stride;
            pixels = new byte[size];
            bitmap.CopyPixels(pixels, stride, 0);
        }

        private byte[] createPixelArray(PixelColor[,] colors)
        {
            int width = colors.GetLength(0);
            int height = colors.GetLength(1);
            byte[] pColors = new byte[width * 4 * height];

            for (int i = 0; i < height; ++i)
            {
                for (int j = 0; j < width; ++j)
                {
                    int startIndex = calcPixelIndex(j, i, colors.GetLength(0));
                    pColors[startIndex]     = colors[j, i].blue;
                    pColors[startIndex + 1] = colors[j, i].green;
                    pColors[startIndex + 2] = colors[j, i].red;
                    pColors[startIndex + 3] = colors[j, i].alpha;
                }
            }

            return pColors;
        }

        public MyImage() { }

        public MyImage(int width, int height)
        {
            bitmap = new WriteableBitmap(width, height, 100, 100, PixelFormats.Bgra32, null);
            getPixelsData();
        }

        public MyImage(string path) : this(new BitmapImage(new Uri(path))) { }

        public MyImage(WriteableBitmap newbitmap)
        {
            bitmap = new WriteableBitmap(newbitmap);
            getPixelsData();
        }

        public MyImage(BitmapSource bitmap)
        {
            this.bitmap = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight, 100, 100, PixelFormats.Bgra32, null);

            int tempStride = bitmap.PixelWidth * 4;
            int size = bitmap.PixelHeight * tempStride;
            byte[] tempPixels = new byte[size];
            bitmap.CopyPixels(tempPixels, tempStride, 0);

            this.bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), tempPixels, tempStride, 0);

            getPixelsData();
        }

        private void enlargeImage(int newX, int newY)
        {
            int oldWidth = bitmap.PixelWidth;
            int oldHeight = bitmap.PixelHeight;
            bitmap = new WriteableBitmap(newX, newY, bitmap.DpiX, bitmap.DpiY, bitmap.Format, bitmap.Palette);
            bitmap.WritePixels(new Int32Rect(0, 0, oldWidth, oldHeight), pixels, stride, 0);
            setStride();
        }

        private void enlargeImageWithShift(int newX, int newY, int startX, int startY)
        {
            int oldWidth = bitmap.PixelWidth;
            int oldHeight = bitmap.PixelHeight;
            bitmap = new WriteableBitmap(newX, newY, bitmap.DpiX, bitmap.DpiY, bitmap.Format, bitmap.Palette);
            bitmap.WritePixels(new Int32Rect(startX, startY, oldWidth, oldHeight), pixels, stride, 0);
            setStride();
        }

        public MyImage Clone()
        {
            MyImage newImage = new MyImage();
            newImage.bitmap = bitmap.Clone();
            newImage.pixels = new byte[pixels.Length];
            pixels.CopyTo(newImage.pixels, 0);
            newImage.stride = stride;

            return newImage;
        }

        public void SaveImage(string path)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (FileStream filestream = new FileStream(path, FileMode.Create))
            {
                encoder.Save(filestream);
            }
        }

        public int GetWidth()
        {
            return bitmap.PixelWidth;
        }

        public int GetHeight()
        {
            return bitmap.PixelHeight;
        }

        public PixelColor GetPixel(int x, int y)
        {
            int startIndex = calcPixelIndex(x, y, GetWidth());
            return new PixelColor(pixels[startIndex + 3], pixels[startIndex + 2], pixels[startIndex + 1], pixels[startIndex]);
        }

        public byte GetAlpha(int x, int y)
        {
            return pixels[calcPixelIndex(x, y, GetWidth()) + 3];
        }

        public byte GetRed(int x, int y)
        {
            return pixels[calcPixelIndex(x, y, GetWidth()) + 2];
        }

        public byte GetGreen(int x, int y)
        {
            return pixels[calcPixelIndex(x, y, GetWidth()) + 1];
        }

        public byte GetBlue(int x, int y)
        {
            return pixels[calcPixelIndex(x, y, GetWidth())];
        }

        private void setPixels(int startX, int startY, MyImage secondImage)
        {
            bitmap.WritePixels(new Int32Rect(startX, startY, secondImage.GetWidth(), secondImage.GetHeight()), secondImage.pixels, secondImage.stride, 0);

            getPixelsData();
        }

        public void SetPixels(int startX, int startY, PixelColor[,] pixelsColors)
        {
            byte[] pixelArray = createPixelArray(pixelsColors);

            if (startX < 0 || startY < 0)
            {
                enlargeImageWithShift(
                    Math.Max(GetWidth(), Math.Max(Math.Abs(startX) + pixelsColors.GetLength(0), (startX < 0 ? Math.Abs(startX) : 0) + GetWidth())),
                    Math.Max(GetHeight(), Math.Max(Math.Abs(startY) + pixelsColors.GetLength(1), (startY < 0 ? Math.Abs(startY) : 0) + GetHeight())),
                    (startX < 0 ? Math.Abs(startX) : 0), startY < 0 ? Math.Abs(startY) : 0);

                if (startX < 0)
                {
                    startX = 0;
                }
                if (startY < 0)
                {
                    startY = 0;
                }
            }
            else if (startX + pixelsColors.GetLength(0) >= bitmap.PixelWidth || startY + pixelsColors.GetLength(1) >= bitmap.PixelHeight)
            {
                enlargeImage(Math.Max(GetWidth(), startX + pixelsColors.GetLength(0)), Math.Max(GetHeight(), startY + pixelsColors.GetLength(1)));
            }

            bitmap.WritePixels(new Int32Rect(startX, startY, pixelsColors.GetLength(0), pixelsColors.GetLength(1)), pixelArray, calcStride(pixelsColors.GetLength(0)), 0);

            getPixelsData();
        }

        public void SetPixels(int startX, int startY, int sizeX, int sizeY, PixelColor pixelColor)
        {
            PixelColor[,] pixelsArray = new PixelColor[sizeX, sizeY];

            for (int i = 0; i < sizeX; ++i)
            {
                for (int j = 0; j < sizeY; ++j)
                {
                    pixelsArray[i, j] = pixelColor;
                }
            }

            SetPixels(startX, startY, pixelsArray);
        }

        public void SetPixel(int x, int y, PixelColor pixelColor)
        {
            PixelColor[,] pixelsArray = new PixelColor[1, 1];
            pixelsArray[0, 0] = pixelColor;
            SetPixels(x, y, pixelsArray);
        }

        public void InsertImage(int x, int y, MyImage image)
        {

            if (x < 0 || y < 0)
            {
                enlargeImageWithShift(
                    Math.Max(GetWidth(), Math.Max(Math.Abs(x) + image.GetWidth(), (x < 0 ? Math.Abs(x) : 0) + GetWidth())),
                    Math.Max(GetHeight(), Math.Max(Math.Abs(y) + image.GetHeight(), (y < 0 ? Math.Abs(y) : 0) + GetHeight())),
                    (x < 0 ? Math.Abs(x) : 0), y < 0 ? Math.Abs(y) : 0);

                if (x < 0)
                {
                   x = 0;
                }
                if (y < 0)
                {
                    y = 0;
                }
            }
            else if (x + image.GetWidth() >= GetWidth() || y + image.GetHeight() >= GetHeight())
            {
                enlargeImage(Math.Max(GetWidth(), x + image.GetWidth()), Math.Max(GetHeight(), y + image.GetHeight()));
            }

            setPixels(x, y, image);
        }

        public void DrawLines(List<Pair<Point, Point>> lines, System.Drawing.Color color)
        {
            bitmap.Lock();

            foreach (Pair<Point, Point> line in lines)
            {
                var tempBitmap = new System.Drawing.Bitmap(GetWidth(), GetHeight(), bitmap.BackBufferStride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, bitmap.BackBuffer);
                System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(tempBitmap);
                graphics.DrawLine(new System.Drawing.Pen(color), new System.Drawing.Point((int)line.First.X, (int)line.First.Y),
                    new System.Drawing.Point((int)line.Second.X, (int)line.Second.Y));
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, GetWidth(), GetHeight()));
            bitmap.Unlock();
        }

        public BitmapSource GetSource()
        {
            return bitmap;
        }
    }
}