using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mark2
{
    class Square
    {
        public int x;
        public int y;
        public int w;
        public int h;
        public int cx;
        public int cy;

        public Square(int x, int y, int w, int h)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;

            this.cx = x + (w / 2);
            this.cy = y + (h / 2);
        }
    }
}
