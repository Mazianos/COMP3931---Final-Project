using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WaveAnalyzer
{
    class WaveDrawer
    {
        private SolidColorBrush waveBrush;

        public WaveDrawer(Color waveColor)
        {
            waveBrush = new SolidColorBrush
            {
                Color = waveColor
            };
        }

        public void DrawWave(short[] samples, ref Canvas canvas, double offset, double windowWidth)
        {
            PointCollection wavePoints = new PointCollection();

            Polyline wavePolyline = new Polyline()
            {
                Points = wavePoints,
                Stroke = waveBrush,
                StrokeThickness = 1
            };
            
            // Used for clamping the value of each sample to between 0 and 1.
            int min = GetMinSample(samples);
            float denom = GetMaxSample(samples) - min;
            if (denom == 0)
            {
                ++denom;
            }

            for (int i = (int)offset; i < samples.Length && i < windowWidth + offset; ++i)
            {
                Point point = new Point()
                {
                    X = i,
                    Y = (samples[i] - min) / denom * 150
                };

                wavePoints.Add(point);
            }

            canvas.Children.Add(wavePolyline);

            canvas.Width = samples.Length;
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
