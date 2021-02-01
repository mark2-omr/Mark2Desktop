using System;
using System.Collections.Generic;
using System.Text;

namespace Mark2CF
{
    public class Rgba32
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
    }
    public class Image<T>
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public T this[int x, int y]
        {
            get
            {
                return default(T);
            }

            set
            {

            }
        }
    }
}
