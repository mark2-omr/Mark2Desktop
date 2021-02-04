using System;
using System.Collections.Generic;
using System.Text;
using ImagePng;
using System.IO;

namespace Mark2CF
{
    public class Rgba32 : IColor
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Rgba32()
        {

        }

        public Rgba32(float r, float g, float b)
        {

        }

        static public Rgba32 ParseHex(string hex)
        {
            return new Rgba32();
        }

        public void SetPixel(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }
    public class Image<T> where T : IColor, new()
    {
        private ImagePng.ImagePng pngImage = null;

        public int Width { get; set; }
        public int Height { get; set; }

        public T this[int x, int y]
        {
            get
            {
                ColorRGBA color = pngImage.GetPixel(x, y);

                T pixel = new T();
                pixel.SetPixel(color.R, color.G, color.B, color.A);


                return pixel;
            }

            set
            {

            }
        }

        public Image()
        {
            pngImage = new ImagePng.ImagePng();
        }

        public void Load(Stream stream)
        {
            pngImage.Load(stream);
        }

        public Image<T> Clone()
        {
            // 
            return new Image<T>();
        }

        public static Image<T> Load(byte[] imageBytes)
        {
            Image<T> image = new Image<T>();
            image.Load(new MemoryStream(imageBytes));

            return image;
        }
    }
}
