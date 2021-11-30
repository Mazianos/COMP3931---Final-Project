using System;
using System.Windows.Media;

namespace WaveAnalyzer
{
    public static class AppImage
    {
        private static ImageSourceConverter converter = new ImageSourceConverter();

        public static ImageSource OpenIcon { get => (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\open.png")); }
        public static ImageSource SaveIcon { get => (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\save.png")); }
        public static ImageSource PlayIcon { get => (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\play.png")); }
        public static ImageSource PauseIcon { get => (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\pause.png")); }
        public static ImageSource StopIcon { get => (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\stop.png")); }
        public static ImageSource RecordIcon { get => (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\record.png")); }
    }
}
