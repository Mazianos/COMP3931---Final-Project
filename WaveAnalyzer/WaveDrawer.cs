using System.Diagnostics;
using System.Windows.Forms.DataVisualization.Charting;

namespace WaveAnalyzer
{
    class WaveDrawer
    {
        /// <summary>
        /// Draw the wave on the appropriate charts
        /// </summary>
        /// <param name="samples">Samples to draw</param>
        /// <param name="chart">Chart we are drawing on</param>
        /// <param name="offset">Starting index of the wave</param>
        /// <param name="windowWidth">How wide the window is</param>
        /// <param name="incrementer">How much are we incrementing by</param>
        public void DrawWave(short[] samples, ref Chart chart, int offset, double windowWidth, int incrementer)
        {
            for (int i = 0; i + offset < samples.Length && i < windowWidth; i += incrementer)
            {
                chart.Series[0].Points.AddXY(i, samples[i + offset]);
            }
        }
    }
}
