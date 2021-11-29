using System;

namespace WaveAnalyzer
{
    public static class Windowing
    {
        public static short[][] Hann(short[][] constSamples)
        {
            short[][] samples = new short[constSamples.Length][];

            for (int i = 0; i < samples.Length; ++i)
            {
                samples[i] = new short[constSamples[i].Length];
                for (int j = 0; j < samples[i].Length; ++j)
                {
                    samples[i][j] = (short)(constSamples[i][j] * (0.5 * (1 - Math.Cos(2 * Math.PI * j / samples[i].Length))));
                }
            }

            return samples;
        }

        public static short[][] Triangular(short[][] constSamples)
        {
            short[][] samples = new short[constSamples.Length][];

            for (int i = 0; i < samples.Length; ++i)
            {
                samples[i] = new short[constSamples[i].Length];
                for (int j = 0; j < samples[i].Length; ++j)
                {
                    samples[i][j] = (short)(constSamples[i][j] * (1 - (j - samples[i].Length / 2) / samples[i].Length / 2));
                }
            }

            return samples;
        }
    }
}
