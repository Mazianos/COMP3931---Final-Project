using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace WaveAnalyzer
{
    public static class AppColor
    {
        public static Color WaveColor { get => waveColor; private set => waveColor = value; }
        public static Color SelectionColor { get => selectionColor; private set => selectionColor = value; }
        public static Color SelectorColor { get => selectorColor; private set => selectorColor = value; }

        private static Color waveColor = new Color()
        {
            R = 248,
            G = 175,
            B = 96,
            A = 255
        };

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
    }
}
