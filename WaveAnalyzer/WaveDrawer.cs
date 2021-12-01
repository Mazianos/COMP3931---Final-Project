using System.Diagnostics;
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
    }
}
