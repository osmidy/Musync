using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musync
{
    /// <summary>
    /// This helper class analyzes frequencies and magnitudes of an FFT
    /// to make decisions about operations on the Blynclight.
    /// </summary>
    ///
    public static class FFTHelper
    {
        public const int FftLength = 16384;

        public const int MinBassFreq = 20;

        public const int MaxBassFreq = 200;

        private static double SampleRate = 48000;

        public static LyncColor FreqToColor(double ratio)
        {
            var val = ratio * ratio;

            if (val == 0 || Double.IsNaN(val))
            {
                return LyncColor.White;
            }
            else if (val > 4)
            {
                return LyncColor.Yellow;
            }
            else if (val > 3.75)
            {
                return LyncColor.Green;
            }
            else if (val > 3.5)
            {
                return LyncColor.Cyan;
            }
            else if (val > 2.25)
            {
                return LyncColor.Blue;
            }
            else if (val > 2.0) {
                return LyncColor.Magenta;
            }
            else
            {
                return LyncColor.Red;
            }
        }

        public static bool ShouldPulse(double psd)
        {
            return psd > 0.7;
        }

        public static int FreqToIndex(double freq)
        {
            return (int)(freq * FftLength / FFTHelper.SampleRate);
        }

        public static double IndexToFreq(double index)
        {
            return index * FFTHelper.SampleRate / FftLength;
        }

        public static void SetSampleRate(double fs)
        {
            FFTHelper.SampleRate = fs;
        }
    }
}
