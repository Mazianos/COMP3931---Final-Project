using System;

namespace WaveAnalyzer
{
    public static class Filter
    {
        public static void FilterRange(int hzStart, int hzEnd, int sampleRate, short[][] samplesToFilter)
        {
            int samplesLength = samplesToFilter[0].Length;
            int binStart = samplesLength * hzStart / sampleRate;
            int binEnd = samplesLength * hzEnd / sampleRate;
            Complex[] A = CreateWeights(binStart, binEnd, sampleRate);
            double[] weights = Fourier.InverseDFT(A, samplesLength);

            for (int i = 0; i < samplesToFilter.Length; ++i)
            {
                Array.Resize(ref samplesToFilter[i], samplesLength + weights.Length);
                Convolution(samplesToFilter[i], weights);
                Array.Resize(ref samplesToFilter[i], samplesLength);
            }
            
        }

        public static Complex[] CreateWeights(int binStart, int binEnd, int sampleRate)
        {
            Complex[] weights = new Complex[sampleRate];

            weights[0].real = 1;

            // If the starting bin is 0, increment the starting bin because 0 is the DC component.
            if (binStart == 0)
            {
                ++binStart;
            }

            for (int i = binStart; i < binEnd; ++i)
            {
                weights[i].real = 1;
            }

            for (int i = sampleRate - binStart; i > sampleRate - binEnd; --i)
            {
                weights[i].real = 1;
            }

            return weights;
        }

        public static void AddNMinusOneSamples(ref short[] s, int N)
        {
            Array.Resize(ref s, s.Length + N - 1);
        }

        public static void Convolution(short[] s, double[] weights)
        {
            for (int i = 0; i < s.Length - weights.Length; ++i)
            {
                double sum = 0;
                for (int j = 0; j < weights.Length; ++j)
                {
                    sum += s[i + j] * weights[j];
                }
                sum /= weights.Length;
                s[i] = (short)Math.Round(sum);
            }
        }
    }
}
