using System;

namespace WaveAnalyzer
{
    public static class Filter
    {
        /// <summary>
        /// Filter the user selection 
        /// </summary>
        /// <param name="start">Starting bin index</param>
        /// <param name="end">Ending bin index</param>
        /// <param name="bins">Number of bins used in the dft</param>
        /// <param name="filterSize">Length of filter weights used</param>
        /// <param name="sampleRate">Sample rate of the wave</param>
        /// <param name="samplesToFilter">Buffer of data that we are filtering</param>
        public static void FilterRange(int start, int end, int bins, int filterSize, int sampleRate, short[][] samplesToFilter)
        {
            int binStart = Math.Min(start, end);
            int binEnd = Math.Max(start, end);

            int hzStart = binStart * sampleRate / bins;
            int hzEnd = binEnd * sampleRate / bins;

            Complex[] weights = CreateWeights(hzStart, hzEnd, sampleRate, filterSize);
            Fourier.DivideByN(weights, filterSize);
            double[] weightsIDFT = Fourier.InverseDFT(weights, filterSize);

            for (int i = 0; i < samplesToFilter.Length; ++i)
            {
                int samplesLength = samplesToFilter[i].Length; 
                Array.Resize(ref samplesToFilter[i], samplesLength + filterSize);
                Convolution(samplesToFilter[i], weightsIDFT);
                Array.Resize(ref samplesToFilter[i], samplesLength);
            }
        }

        /// <summary>
        /// Create the filter weights used for convolution
        /// </summary>
        /// <param name="hzStart">Starting frequency</param>
        /// <param name="hzEnd">Ending frequency</param>
        /// <param name="sampleRate">Sample rate of the wave</param>
        /// <param name="filterSize">Length of the filter weight array</param>
        /// <returns>Filter Weights in frequency domain</returns>
        public static Complex[] CreateWeights(int hzStart, int hzEnd, int sampleRate, int filterSize)
        {
            int binStart = hzStart * filterSize / sampleRate;
            int binEnd = hzEnd * filterSize / sampleRate;

            Complex[] weights = new Complex[filterSize];

            weights[0].real = 1;

            // If the starting bin is 0, increment the starting bin because 0 is the DC component.
            if (binStart == 0)
            {
                ++binStart;
            }
            if (binEnd == 1)
            {
                ++binEnd;
            }

            for (int i = binStart; i < binEnd; ++i)
            {
                weights[i].real = 1;
            }

            for (int i = filterSize - binStart; i > filterSize - binEnd; --i)
            {
                weights[i].real = 1;
            }

            Fourier.PrintComplex(weights);

            return weights;
        }

        /// <summary>
        /// Convolve the data passed in using the weights passed in
        /// </summary>
        /// <param name="s">Data passed in</param>
        /// <param name="weights">Array of filter weights in time domain</param>
        public static void Convolution(short[] s, double[] weights)
        {
            for (int i = 0; i < s.Length - weights.Length; ++i)
            {
                double sum = 0;
                for (int j = 0; j < weights.Length; ++j)
                {
                    sum += s[i + j] * weights[j];
                }

                s[i] = (short)Math.Round(sum);
            }
        }
    }
}
