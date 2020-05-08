using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mark2
{
    public class Question
    {
        public string text;
        public int type;
        public List<Area> areas;

        public Question()
        {
            areas = new List<Area>();
        }
    }
}
