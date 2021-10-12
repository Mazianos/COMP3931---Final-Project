using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WaveAnalyzer
{
    class WaveSelector
    {
        private double startX;
        private double currentX;
        private SolidColorBrush selectionBrush;

        public WaveSelector(double startX, Color selectionColor)
        {
            this.startX = startX;
            selectionBrush = new SolidColorBrush
            {
                Color = selectionColor
            };
        }

        public void UpdateSelection(double currentX, ref Canvas canvas)
        {
            this.currentX = currentX;
            RedrawSelection(ref canvas);
        }

        public void RedrawSelection(ref Canvas canvas)
        {
            double selectionDifference = currentX - startX;

            Thickness rectangleMargin = new Thickness()
            {
                Left = selectionDifference >= 0 ? startX : currentX,
            };

            Rectangle selection = new Rectangle()
            {
                Height = 175,
                Width = Math.Abs(selectionDifference),
                Fill = selectionBrush,
                Margin = rectangleMargin
            };

            canvas.Children.Add(selection);
        }

        public double GetStartX()
        {
            return startX;
        }

        public double GetCurrentX()
        {
            return currentX;
        }
    }
}
