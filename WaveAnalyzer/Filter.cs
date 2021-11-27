using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace WaveAnalyzer
{
    public static class Filter
    {
        public static void FilterRange(int binStart, int binEnd, short[][] samplesToFilter)
        {
            int samplesLength = samplesToFilter[0].Length;

            Trace.WriteLine(samplesToFilter[0][0]);

            double[] weights = Fourier.InverseDFT(CreateWeights(binStart, binEnd, samplesLength), samplesLength);

            Convolution(samplesToFilter[0], weights, samplesLength);

            Trace.WriteLine(samplesToFilter[0][0]);
        }

        public static Complex[] CreateWeights(int binStart, int binEnd, int samplesLength)
        {
            Complex[] weights = new Complex[samplesLength];

            for (int i = binStart; i < binEnd; ++i)
            {
                weights[i].real = 1;
            }

            for (int i = samplesLength - binStart; i > samplesLength - binEnd; --i)
            {
                weights[i].real = 1;
            }

            return weights;
        }

        public static void AddNMinusOneSamples(ref short[] s, int N)
        {
            Array.Resize(ref s, s.Length + N - 1);
        }

        public static void Convolution(short[] s, double[] weights, int N)
        {
            for (int i = 0; i < s.Length - N + 1; ++i)
            {
                double sum = 0;
                for (int j = 0; j < N; ++j)
                {
                    sum += s[i + j] * weights[j];
                }
                s[i] = (short)Math.Round(sum);
            }
        }
    }
}
