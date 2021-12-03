using System;

namespace WaveAnalyzer
{
    public static class Windowing
    {
        private const short HannMultiplier = 1;

        /// <summary>
        /// Put the selected samples into a Hann window
        /// </summary>
        /// <param name="constSamples">Samples</param>
        /// <returns>Samples in window</returns>
        public static short[][] Hann(short[][] constSamples)
        {
            short[][] samples = new short[constSamples.Length][];

            for (int i = 0; i < samples.Length; ++i)
            {
                samples[i] = new short[constSamples[i].Length];
                for (int j = 0; j < samples[i].Length; ++j)
                {
                    samples[i][j] = (short)(HannMultiplier * constSamples[i][j] * (0.5 * (1 - Math.Cos(2 * Math.PI * j / samples[i].Length))));
                }
            }

            return samples;
        }

        /// <summary>
        /// Put the selected samples into a Triangle window
        /// </summary>
        /// <param name="constSamples">Samples</param>
        /// <returns>Samples in window</returns>
        public static short[][] Triangular(short[][] constSamples)
        {
            short[][] samples = new short[constSamples.Length][];

            for (int i = 0; i < samples.Length; ++i)
            {
                samples[i] = new short[constSamples[i].Length];
                for (int j = 0; j < samples[i].Length; ++j)
                {
                    samples[i][j] = (short)(constSamples[i][j] * (1 - (j - samples[i].Length / 2.0) / (samples[i].Length / 2)));
                }
            }

            return samples;
        }
    }
}
