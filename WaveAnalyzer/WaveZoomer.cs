using System;
using System.Collections.Generic;
using System.Windows.Forms.DataVisualization.Charting;

namespace WaveAnalyzer
{
    public class WaveZoomer
    {
        private Stack<(double, double)> zooms;

        public WaveZoomer()
        {
            zooms = new Stack<(double, double)>();
        }

        public void HandleZoom(ref Chart chart, double delta, double position)
        {
            var axisX = chart.ChartAreas[0].AxisX;
            axisX.ScaleView.Zoomable = true;

            if (delta < 0)
            {
                if (zooms.Count > 0)
                {
                    var zoom = zooms.Pop();
                    if (zooms.Count == 0)
                    {
                        axisX.ScaleView.ZoomReset();
                    }
                    else
                    {
                        axisX.ScaleView.Zoom(zoom.Item1, zoom.Item2);
                    }
                }
            }
            else if (delta > 0)
            {
                var min = axisX.ScaleView.ViewMinimum;
                var max = axisX.ScaleView.ViewMaximum;
                zooms.Push((min, max));

                axisX.ScaleView.Zoom(axisX.PixelPositionToValue(position) - (max - min) / 4, axisX.PixelPositionToValue(position) + (max - min) / 4);
            }
            
            axisX.ScaleView.Zoomable = false;
        }
    }
}
