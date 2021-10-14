using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Interop;



namespace WaveAnalyzer
{
    public partial class MainWindow : Window
    {
        private Wave wave;
        private WaveDrawer waveDrawer;
        ImageSourceConverter converter;
        bool bRecording, bPlaying;
        IntPtr hwnd;

        [DllImport("RecordPlayLibrary.dll")]
        public static extern bool WinProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("RecordPlayLibrary.dll")]
        public static extern unsafe byte* GetSaveBuffer();

        [DllImport("RecordPlayLibrary.dll")]
        public static extern uint GetDWDataLength();

        [DllImport("RecordPlayLibrary.dll")]
        public static extern unsafe void SetSaveBuffer(byte* saveBuffer);

        [DllImport("RecordPlayLibrary.dll")]
        public static extern void SetDWDataLength(uint dataWord);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if(WinProc(hwnd, msg, wParam, lParam))
            {
                return IntPtr.Zero;
            }
            return (IntPtr) 1;
        }

        public MainWindow()
        {
            InitializeComponent();

            // Set icon images.
            converter = new ImageSourceConverter();
            OpenIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\open.png"));
            SaveIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\save.png"));
            PlayPauseIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\play.png"));
            StopIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\stop.png"));
            RecordIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\record.png"));
        }

        protected void OnInitialized(EventArgs e)
        {
            Window window = Application.Current.MainWindow;
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
            source.AddHook(WndProc);
            hwnd = source.Handle;
        }

        public void OpenHandler(object sender, RoutedEventArgs e)
        {
            // Opens the open file dialog box.
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "WAV files (*.wav)|*.wav" +
                "|All files (*.*)|*.*";

            // Returns true when a file is opened. Return if not opened.
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            // Read the wave file in bytes.
            wave = new Wave(openFileDialog.FileName);
            SetSaveBuffer(wave.data)


            Trace.WriteLine("Done!");

            // Drawing.
            Color waveColor = new Color()
            {
                R = 248,
                G = 175,
                B = 96,
                A = 255
            };

            waveDrawer = new WaveDrawer(waveColor);

            waveDrawer.DrawWave(wave.GetLeftChannel(), ref LeftChannelCanvas, 0, root.Width);
            if (wave.GetRightChannel() != null)
            {
                waveDrawer.DrawWave(wave.GetRightChannel(), ref RightChannelCanvas, 0, root.Width);
            }

            
        }

        private void SaveHandler(object sender, RoutedEventArgs e)
        {

        }

        private void LeftChannelScrollHandler(object sender, ScrollChangedEventArgs e)
        {
            if (wave != null)
            {
                waveDrawer.DrawWave(wave.GetLeftChannel(), ref LeftChannelCanvas, e.HorizontalOffset, root.Width);
            }
        }

        private void RightChannelScrollHandler(object sender, ScrollChangedEventArgs e)
        {
            if (wave != null && wave.GetRightChannel() != null)
            {
                waveDrawer.DrawWave(wave.GetRightChannel(), ref RightChannelCanvas, e.HorizontalOffset, root.Width);
            }
        }

        /**
         * Start Recording
         */
        public void RecordHandler(object sender, RoutedEventArgs e)
        {
            if (!bRecording)
            { 
                //0x0111 is the code for WM_COMMAND
                //1000 is the code for IDC_RECORD_BEG
                SendMessage(hwnd, 0x0111, (IntPtr)((ushort)(((ulong)(1000)) & 0xffff)), (IntPtr)null);
                bRecording = true;
            } else
            {
                //0x0111 is the code for WM_COMMAND
                //1001 is the code for IDC_RECORD_END
                SendMessage(hwnd, 0x0111, (IntPtr)((ushort)(((ulong)(1001)) & 0xffff)), (IntPtr)null);
                bRecording = false;
            }
            
        }

        /**
         * Stops Playing Wave
         */
        public void StopHandler(object sender, RoutedEventArgs e)
        {
            //0x0111 is the code for WM_COMMAND
            //1004 is the code for IDC_PLAY_END
            SendMessage(hwnd, 0x0111, (IntPtr)((ushort)(((ulong)(1001)) & 0xffff)), (IntPtr)null);
        }

        /**
         * Play/Pause Wave
         */
        public void PlayPauseHandler(object sender, RoutedEventArgs e)
        {
            if (!bPlaying)
            {
                PlayPauseIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\pause.png"));
                //0x0111 is the code for WM_COMMAND
                //1002 is the code for IDC_PLAY_BEG
                SendMessage(hwnd, 0x0111, (IntPtr)((ushort)(((ulong)(1002)) & 0xffff)), (IntPtr)null);
                bPlaying = true;
            }
            else
            {
                PlayPauseIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\play.png"));
                //0x0111 is the code for WM_COMMAND
                //1003 is the code for IDC_PLAY_PAUSE
                SendMessage(hwnd, 0x0111, (IntPtr)((ushort)(((ulong)(1003)) & 0xffff)), (IntPtr)null);
                bPlaying = false;
            }
        }
    }
}
