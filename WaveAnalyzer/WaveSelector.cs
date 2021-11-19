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
    public class WaveSelector
    {
        public int StartX { get; private set; }

        public int CurrentX { get; private set; }

        private SolidColorBrush selectionBrush;
        private SolidColorBrush selectorBrush;

        public WaveSelector(int startX)
        {
            StartX = startX;
            selectionBrush = new SolidColorBrush
            {
                Color = AppColor.SelectionColor
            };
            selectorBrush = new SolidColorBrush
            {
                Color = AppColor.SelectorColor
            };
        }

        public void UpdateSelection(int currentX, ref Canvas canvas)
        {
            CurrentX = currentX;
            RedrawSelection(ref canvas);
        }

        private void RedrawSelection(ref Canvas canvas)
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

            DrawSelector(ref canvas);
        }

        private void DrawSelector(ref Canvas canvas)
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
