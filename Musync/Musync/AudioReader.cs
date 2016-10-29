using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musync
{
    /// <summary>
    /// Helper class to read audio output from DirectSound
    /// </summary>
    ///
    public static class AudioReader
    {
        

        public static LyncColor FreqToColor(double freq)
        {
            int min = 500;
            int max = 16000;
            int step = (max - min) / 8;  // TODO: magic number

            if (freq < min)
            {
                return LyncColor.White;
            }

            if (freq > max)
            {
                return LyncColor.Magenta;
            }

            int colorIndex = (int) ((freq - min) / step);
            Console.WriteLine(colorIndex);
            var color = (LyncColor)(colorIndex);
            Console.WriteLine(color);
            return color;
        }
    }
}
