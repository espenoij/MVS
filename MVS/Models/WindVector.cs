using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVS.Services
{
    internal class WindVector
    {
        public double x;
        public double y;
        public double spd;
        public double dir;

        public WindVector()
        {
            x = 0;
            y = 0;
            dir = 0;
            spd = 0;
        }
    }
}
