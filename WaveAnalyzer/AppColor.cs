using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace WaveAnalyzer
{
    public static class AppColor
    {
        public static System.Drawing.Color WaveColor { get => waveColor; private set => waveColor = value; }
        public static Color SelectionColor { get => selectionColor; private set => selectionColor = value; }
        public static Color SelectorColor { get => selectorColor; private set => selectorColor = value; }
        public static System.Drawing.Color ChartColor { get => chartColor; private set => chartColor = value; }

        private static System.Drawing.Color waveColor = System.Drawing.Color.FromArgb(248, 175, 96);

        private static Color selectionColor = new Color()
        {
            R = 96,
            G = 175,
            B = 248,
            A = 255
        };

        private static Color selectorColor = new Color()
        {
            R = 255,
            G = 100,
            B = 100,
            A = 255
        };

        private static System.Drawing.Color chartColor = System.Drawing.Color.FromArgb(88, 99, 103);
    }
}
