using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Forms.DataVisualization.Charting;

namespace WaveAnalyzer
{
    class WaveDrawer
    {
        private short maxSample;
        private short minSample;
        private SolidColorBrush waveBrush;

        public WaveDrawer()
        {
            waveBrush = new SolidColorBrush
            {
                Color = AppColor.WaveColor
            };
        }

        public void SetMinMaxSample(short[] samples)
        {
            minSample = GetMinSample(samples);
            maxSample = GetMaxSample(samples);
        }

        public void DrawWave(short[] samples, ref Chart chart, int offset, double windowWidth)
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

        private short GetMinSample(short[] samples)
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

        public static short GetMaxSample(short[] samples)
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
