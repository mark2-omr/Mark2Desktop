using System;
using System.Collections.Generic;
using System.Text;

namespace Mark2CF
{
    public interface IColor
    {
        public void SetPixel(byte r, byte g, byte b, byte a);
        public byte GetR();
        public byte GetG();
        public byte GetB();
    }
}
