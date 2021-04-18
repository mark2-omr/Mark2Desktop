using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

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
        //private ImagePng.ImagePng pngImage = null;
        private Bitmap image = null;



        public int Width
        {
            get { return image.Width; }
        }

        public int Height { 
            get { return image.Height; }
        }

        public T this[int x, int y]
        {
            get
            {
                //ColorRGBA color = pngImage.GetPixel(x, y);
                var color = image.GetPixel(x, y);

                T pixel = new T();
                pixel.SetPixel(color.R, color.G, color.B, color.A);


                return pixel;
            }

            set
            {

            }
        }

        private void SetImage(Bitmap bitmap)
        {
            this.image = (Bitmap)bitmap.Clone();
        }

        public Image()
        {
            //pngImage = new ImagePng.ImagePng();
            image = new Bitmap(512, 512);
        }

        public void Load(Stream stream)
        {
            //pngImage.Load(stream);
            image = new Bitmap(stream);
            Console.WriteLine("{0}, {1}", image.Width, image.Height);
        }

        public Image<T> Clone()
        {
            var i = new Image<T>();
            i.SetImage(this.image);
             
            return i;
        }

        public void Resize(int width, int height)
        {
            Bitmap modifiedImage = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(modifiedImage);

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            g.DrawImage(this.image, 0, 0, width, height);

            this.image = modifiedImage;
        }

        public void Crop(int x, int y, int width, int height)
        {
            Rectangle rect = new Rectangle(x, y, width, height);
            this.image = this.image.Clone(rect, this.image.PixelFormat);
        }

        public void Save(string fileName)
        {
            this.image.Save(fileName);
        }

        public static Image<T> Load(byte[] imageBytes)
        {
            Image<T> image = new Image<T>();
            image.Load(new MemoryStream(imageBytes));

            return image;
        }
    }
}
