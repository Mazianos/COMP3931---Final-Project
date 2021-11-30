using System.Windows;
using System.Windows.Forms.DataVisualization.Charting;

namespace WaveAnalyzer
{
    public static class ChartCreator
    {
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
            axisX.MajorGrid.Enabled = false;

            var axisY = area.AxisY;
            axisY.Minimum = -1;
            axisY.Maximum = 1;
            axisY.LabelStyle.Enabled = false;
            axisY.MajorGrid.Enabled = false;
            axisY.ScrollBar.Enabled = true;

            var cursorX = area.CursorX;
            cursorX.IsUserEnabled = true;
            cursorX.IsUserSelectionEnabled = true;

            chart.ChartAreas.Add(area);

            return chart;
        }

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
            //axisX.LabelStyle.Enabled = false;
            axisX.MajorGrid.Enabled = false;

            var axisY = area.AxisY;
            axisY.Minimum = 0;
            axisY.Maximum = 2;
            //axisY.LabelStyle.Enabled = false;
            axisY.MajorGrid.Enabled = false;

            var cursorX = area.CursorX;
            cursorX.IsUserEnabled = true;
            cursorX.IsUserSelectionEnabled = true;

            chart.ChartAreas.Add(area);

            return chart;
        }
    }
}
