using System.Windows.Forms.DataVisualization.Charting;

namespace WaveAnalyzer
{
    class WaveDrawer
    {
        public void DrawWave(short[] samples, ref Chart chart, int offset, double windowWidth, short minSample, short maxSample)
        {
            // Used for clamping the value of each sample to between 0 and 1.
            float denom = maxSample - minSample;
            if (denom == 0)
            {
                ++denom;
            }

            for (int i = 0; i + offset < samples.Length && i < windowWidth; ++i)
            {
                chart.Series[0].Points.AddXY(i, (samples[i + offset] - minSample) / denom * 1.8 - 0.9);
            }
        }

        public short GetMinSample(short[] samples)
        {
            short minSample = 0;

            foreach (short sample in samples)
            {
                if (sample < minSample)
                {
                    minSample = sample;
                }
            }

            return minSample;
        }

        public short GetMaxSample(short[] samples)
        {
            short maxSample = 0;

            foreach (short sample in samples)
            {
                if (sample > maxSample)
                {
                    maxSample = sample;
                }
            }

            return maxSample;
        }
    }
}
