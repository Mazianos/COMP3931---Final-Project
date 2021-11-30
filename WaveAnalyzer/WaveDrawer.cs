using System.Windows.Forms.DataVisualization.Charting;

namespace WaveAnalyzer
{
    class WaveDrawer
    {
        public void DrawWave(short[] samples, ref Chart chart, int offset, double windowWidth, int incrementer)
        {
            for (int i = 0; i + offset < samples.Length && i < windowWidth; i += incrementer)
            {
                chart.Series[0].Points.AddXY(i, samples[i + offset]);
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
