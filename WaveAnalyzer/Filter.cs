using System;
using System.Collections.Generic;
using System.Text;

namespace WaveAnalyzer
{
    public static class Filter
    {
        public static Complex[] CreateLowPassWeights(short totallyNotArbitraryHzValue, short[] A, int S, int N)
        {
            int cutoff = (int)Math.Ceiling((double)(totallyNotArbitraryHzValue * N / S));

            Complex[] lowPassWeights = new Complex[N];
            
            for (int i = 0; i < N; ++i)
            {
                lowPassWeights[i].real = i < cutoff || i > A.Length - cutoff ? 1 : 0;
            }

            return lowPassWeights;
        }

        public static void AddNMinusOneSamples(ref short[] s, int N)
        {
            Array.Resize(ref s, s.Length + N - 1);
        }

        public static void Convolution(short[] s, float[] weights, int N)
        {
            for (int i = 0; i < s.Length - N + 1; ++i)
            {
                float sum = 0;
                for (int j = 0; j < N; ++j)
                {
                    sum += s[i + j] * weights[j];
                }
                s[i] = (short)Math.Round(sum);
            }
        }
    }
}
