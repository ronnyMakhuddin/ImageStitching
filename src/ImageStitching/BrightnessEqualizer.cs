using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageStitching
{
    public partial class BrightnessEqualizer
    {
        public int avarageBrightness;
        public MyImage newBrightnessImage = new MyImage();

        public BrightnessEqualizer() { }

        public BrightnessEqualizer(MyImage image)
        {
            avarageBrightness = ImageAvarageBrightness(image);
        }

        public BrightnessEqualizer(int avarageImagesBrightness, int imageBrightness, MyImage image)
        {
            BrightnessChange(avarageImagesBrightness,imageBrightness, image);
        }

        public int getAvaregeBrightness()
        {
            return avarageBrightness;
        }

        public MyImage getNewBrightnessImage()
        {
            return newBrightnessImage;
        }

        public int getBrightness(PixelColor c)
        {
            return (int)(Math.Sqrt(
                c.red * c.red * .241 +
                c.green * c.green * .691 +
                c.blue * c.blue * .068) * 1000 / 255);

            //return (int)((c.red * 299 + c.green * 587 + c.blue * 114) / 255); /// W3C Algorithm
        }

        private static int PixelBrightness(int red, int green, int blue)
        {
            return (int)(Math.Sqrt(
               red * red * .241 +
               green * green * .691 +
               blue * blue * .068) * 1000 / 255);
            //return (int)((red * 299 + green * 587 + blue * 114) / 255); /// W3C Algorithm
        }

        private static int ImageAvarageBrightness(MyImage image)
        {
            int red, green, blue;
            int imageBrightnessSum = 0;

            for (int i = 0; i < image.GetWidth(); i++)
            {
                for (int j = 0; j < image.GetHeight(); j++)
                {
                    red = image.GetRed(i, j);
                    green = image.GetGreen(i, j);
                    blue = image.GetBlue(i, j);
                    imageBrightnessSum += PixelBrightness(red, green, blue);
                }
            }
            return (int)(Math.Round((float)imageBrightnessSum / (float)(image.GetHeight() * image.GetWidth())));
        }

        public void BrightnessChange(int avarageBrightness, int pictureBrightness, MyImage image)
        {
            PixelColor [,] newColor = new PixelColor [image.GetWidth(), image.GetHeight()];
            byte r, g, b;
            float changeValue = (float)(avarageBrightness) / (float)(pictureBrightness);
            for (int i = 0; i < image.GetWidth(); i++)
            {
                for (int j = 0; j < image.GetHeight(); j++)
                {
                    ///Rowna srednia jasnosc obrazkow
                    r = (byte)Math.Round(image.GetRed(i, j) * (changeValue));
                    g = (byte)Math.Round(image.GetGreen(i, j) * (changeValue));
                    b = (byte)Math.Round(image.GetBlue(i, j) * (changeValue));
                    newColor[i, j] = new PixelColor(image.GetAlpha(i, j), r, g, b);
                                        
                    if (r > 255) r = 255;
                    if (r < 0) r = 0;
                    if (g > 255) g = 255;
                    if (g < 0) g = 0;
                    if (b > 255) b = 255;
                    if (b < 0) b = 0;
                }
            }
        }

        //public PixelColor BrightnessChange(int brightnessValueChange, float brightnessWeightChange, PixelColor pixel)
        //{
        //    PixelColor newColor;// = new PixelColor(0, 0, 0, 0);
        //    byte r, g, b;
        //    float changeValue = (float)(brightnessValueChange) * (float)(brightnessWeightChange);
        //    r = pixel.red;
        //    g = pixel.green;
        //    b = pixel.blue;
        //    //System.Console.WriteLine("rgb " + r + " " + g + " " + b + "zmiana "+changeValue);
        //    r = (byte)Math.Round(r + changeValue);
        //    g = (byte)Math.Round(g + changeValue);
        //    b = (byte)Math.Round(b + changeValue);
        //    //System.Console.WriteLine("n rgb " + r + " " + g + " " + b);
            
        //    if (r > 255) r = 255;
        //    if (r < 0) r = 0;
        //    if (g > 255) g = 255;
        //    if (g < 0) g = 0;
        //    if (b > 255) b = 255;
        //    if (b < 0) b = 0;

        //    newColor = new PixelColor(pixel.alpha, r, g, b);

        //    return newColor;
        //}
        public PixelColor BrightnessChange(PixelColor pixel1, PixelColor pixel2)
        {
            int br1 = getBrightness(pixel1);
            int br2 = getBrightness(pixel2);
            PixelColor newColor;// = new PixelColor(0, 0, 0, 0);
            byte r, g, b;
            float changeValue = (float)((br1 + br2) / 2.0) / (float)(br1);
            r = pixel1.red;
            g = pixel1.green;
            b = pixel1.blue;
            r = (byte)Math.Round(r * (changeValue));
            g = (byte)Math.Round(g * (changeValue));
            b = (byte)Math.Round(b * (changeValue));

            if (r > 255) r = 255;
            if (r < 0) r = 0;
            if (g > 255) g = 255;
            if (g < 0) g = 0;
            if (b > 255) b = 255;
            if (b < 0) b = 0;

            newColor = new PixelColor(pixel1.alpha, r, g, b);

            return newColor;
        }
    }
}
