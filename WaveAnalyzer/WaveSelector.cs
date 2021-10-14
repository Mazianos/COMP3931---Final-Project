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
        public int StartX { get; private set; }

        public int CurrentX { get; private set; }

        private SolidColorBrush selectionBrush;
        private SolidColorBrush selectorBrush;

        public WaveSelector(int startX, Color selectionColor, Color selectorColor)
        {
            StartX = startX;
            selectionBrush = new SolidColorBrush
            {
                Color = selectionColor
            };
            selectorBrush = new SolidColorBrush
            {
                Color = selectorColor
            };
        }

        public void UpdateSelection(int currentX, ref Canvas canvas)
        {
            CurrentX = currentX;
            RedrawSelection(ref canvas);
        }

        public void RedrawSelection(ref Canvas canvas)
        {
            double selectionDifference = CurrentX - StartX;

            Thickness rectangleMargin = new Thickness()
            {
                Left = selectionDifference >= 0 ? StartX : CurrentX,
            };

            Rectangle selection = new Rectangle()
            {
                Height = 150,
                Width = Math.Abs(selectionDifference),
                Fill = selectionBrush,
                Margin = rectangleMargin
            };

            canvas.Children.Add(selection);
        }

        public void DrawSelector(ref Canvas canvas)
        {
            Line selector = new Line()
            {
                X1 = CurrentX,
                X2 = CurrentX,
                Y1 = -25,
                Y2 = 175,
                StrokeThickness = 2,
                Stroke = selectorBrush
            };

            canvas.Children.Add(selector);
        }
    }
}
