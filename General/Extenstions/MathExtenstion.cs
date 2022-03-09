using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Extenstions
{
    public static class MathExtenstion
    {
        public static int Clamp(int value,int min,int max)
        {
            if (value < min) return min;
            else if (value > max) return max;
            return value;
        }
    }
}
