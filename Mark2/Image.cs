using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
//using System.Drawing;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;

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
        private BitmapImage bitmapImage = null;
        private WriteableBitmap writableBitmap = null;
        private Rgba32[,] pixels = null;



        public int Width
        {
            get { return writableBitmap.PixelWidth; }
        }

        public int Height { 
            get { return writableBitmap.PixelHeight; }
        }

        public T this[int x, int y]
        {
            get
            {
                //ColorRGBA color = pngImage.GetPixel(x, y);
                var color = pixels[x, y];

                T pixel = new T();
                pixel.SetPixel(color.R, color.G, color.B, color.A);


                return pixel;
            }

            set
            {

            }
        }

        //private void SetImage(Bitmap bitmap)
        //{
        //    this.image = (Bitmap)bitmap.Clone();
        //}

        public Image()
        {
            //pngImage = new ImagePng.ImagePng();
            //image = new Bitmap(512, 512);
            writableBitmap = new WriteableBitmap(512, 512);
        }

        //public void Load(Stream stream)
        //{
        //    //pngImage.Load(stream);
        //    image = new Bitmap(stream);
        //    Console.WriteLine("{0}, {1}", image.Width, image.Height);
        //}

        public void LoadStream(Windows.Storage.Streams.IRandomAccessStream stream)
        {
            bitmapImage = new BitmapImage();
            bitmapImage.SetSource(stream);

            writableBitmap = new WriteableBitmap(bitmapImage.PixelWidth, bitmapImage.PixelHeight);
            writableBitmap.SetSource(stream);

            this.pixels = new Rgba32[Width, Height];

            BinaryReader binaryStream = new BinaryReader(writableBitmap.PixelBuffer.AsStream());
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    byte b = binaryStream.ReadByte();
                    byte g = binaryStream.ReadByte();
                    byte r = binaryStream.ReadByte();
                    byte a = binaryStream.ReadByte();

                    pixels[x, y] = new Rgba32(r, g, b);
                }
            }
        }

        public Image<T> Clone()
        {
            var i = new Image<T>();
            //i.SetImage(this.image);
             
            return i;
        }

        public void Resize(int width, int height)
        {
            // TODO: Resize
            //Bitmap modifiedImage = new Bitmap(width, height);
            //Graphics g = Graphics.FromImage(modifiedImage);

            //g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            //g.DrawImage(this.image, 0, 0, width, height);

            //this.image = modifiedImage;
        }

        public void Crop(int x, int y, int width, int height)
        {
            // TODO: Crop
            //Rectangle rect = new Rectangle(x, y, width, height);
            //this.image = this.image.Clone(rect, this.image.PixelFormat);
        }

        public void Save(string fileName)
        {
            //this.image.Save(fileName);
        }

        public static Image<T> Load(Windows.Storage.Streams.IRandomAccessStream stream)
        {
            Image<T> image = new Image<T>();
            //image.Load(new MemoryStream(imageBytes));
            image.LoadStream(stream);

            return image;
        }
    }
}
