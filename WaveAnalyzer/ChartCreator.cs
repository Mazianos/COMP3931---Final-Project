using System.Windows;
using System.Windows.Forms.DataVisualization.Charting;

namespace WaveAnalyzer
{
    /// <summary>
    /// Create charts
    /// </summary>
    public static class ChartCreator
    {
        /// <summary>
        /// Create a new chart that displays a time domain graph of a wave
        /// </summary>
        /// <returns>Time domain chart that the user can interact with</returns>
        public static Chart CreateChart()
        {
            var chart = new Chart
            {
                Height = (int)(SystemParameters.PrimaryScreenHeight / 4),
                BackColor = System.Drawing.Color.Transparent
            };

            var series = new Series()
            {
                Color = AppColor.WaveColor
            };

            chart.Series.Add(series);
            chart.Series[0].ChartType = SeriesChartType.FastLine;

            var area = new ChartArea
            {
                BackColor = System.Drawing.Color.Transparent
            };

            var axisX = area.AxisX;
            axisX.ScrollBar.Enabled = false;
            axisX.ScaleView.Zoomable = false;
            axisX.LabelStyle.Enabled = false;
            axisX.IsStartedFromZero = true;

            var axisY = area.AxisY;
            axisY.Minimum = -1;
            axisY.Maximum = 1;
            axisY.MajorGrid.Enabled = false;
            axisY.ScrollBar.Enabled = true;

            var cursorX = area.CursorX;
            cursorX.IsUserEnabled = true;
            cursorX.IsUserSelectionEnabled = true;

            chart.ChartAreas.Add(area);

            return chart;
        }

        /// <summary>
        /// Create a chart that displays bars for a frequency domain graph of a wave or selection
        /// </summary>
        /// <returns>Frequency domain chart that the user can interact with</returns>
        public static Chart CreateDFTChart()
        {
            var chart = new Chart
            {
                Height = (int)(SystemParameters.PrimaryScreenHeight / 4),
                BackColor = System.Drawing.Color.Transparent
            };

            var series = new Series()
            {
                Color = AppColor.WaveColor
            };

            chart.Series.Add(series);
            chart.Series[0].ChartType = SeriesChartType.Column;

            var area = new ChartArea
            {
                BackColor = System.Drawing.Color.Transparent
            };

            var axisX = area.AxisX;
            axisX.ScaleView.Zoomable = false;
            axisX.LabelStyle.Enabled = false;
            axisX.MajorGrid.Enabled = false;

            var axisY = area.AxisY;
            axisY.Minimum = 0;
            axisY.Maximum = 2;
            axisY.LabelStyle.Enabled = false;
            axisY.MajorGrid.Enabled = false;

            var cursorX = area.CursorX;
            cursorX.IsUserEnabled = true;
            cursorX.IsUserSelectionEnabled = true;

            chart.ChartAreas.Add(area);

            return chart;
        }
    }
}
