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
        private ImageSourceConverter converter;
        private bool bRecording;
        private bool bPlaying;
        private IntPtr hwnd;

        [DllImport("SoundProcessing.dll")] public static extern bool WinProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);
        [DllImport("SoundProcessing.dll")] public static extern IntPtr GetSaveBuffer();
        [DllImport("SoundProcessing.dll")] public static extern uint GetDWDataLength();
        [DllImport("SoundProcessing.dll")] public static extern unsafe void SetSaveBuffer(byte* saveBuffer);
        [DllImport("SoundProcessing.dll")] public static extern void SetDWDataLength(uint dataWord);
        [DllImport("SoundProcessing.dll")] public static extern void InitWave();
        [DllImport("SoundProcessing.dll")] public static extern void BeginRecord();
        [DllImport("SoundProcessing.dll")] public static extern void EndRecord();
        [DllImport("SoundProcessing.dll")] public static extern void BeginPlay();
        [DllImport("SoundProcessing.dll")] public static extern void PausePlay();
        [DllImport("SoundProcessing.dll")] public static extern void EndPlay();
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        private WaveSelector currentSelection;
        private short[][] cutSamples;

        public MainWindow()
        {
            InitializeComponent();

            waveDrawer = new WaveDrawer();
            cutSamples = null;

            SetIconImages();

            InitWave();
        }

        private void SetIconImages()
        {
            converter = new ImageSourceConverter();
            OpenIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\open.png"));
            SaveIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\save.png"));
            PlayPauseIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\play.png"));
            StopIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\stop.png"));
            RecordIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\record.png"));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (WinProc(hwnd, msg, wParam, lParam))
            {
                return IntPtr.Zero;
            }
            return (IntPtr)1;
        }

        protected void OnInitialized(EventArgs e)
        {
            Window window = Window.GetWindow(this);
            var wih = new WindowInteropHelper(window);
            hwnd = wih.Handle;
        }

        public unsafe void OpenHandler(object sender, RoutedEventArgs e)
        {
            // Opens the open file dialog box.
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*"
            };

            // Returns true when a file is opened. Return if not opened.
            if (openFileDialog.ShowDialog() != true) return;

            // Read the wave file in bytes.
            wave = new Wave(openFileDialog.FileName);

            // God this is ugly af.
            short[] channel = wave.GetChannels()[0];
            byte[] dataArr = new byte[channel.Length];
            Buffer.BlockCopy(channel, 0, dataArr, 0, channel.Length);
            byte* Pdata;
            fixed (byte* data = dataArr)
            {
                Pdata = data;
            }
            SetSaveBuffer(Pdata);
            SetDWDataLength((uint)wave.GetDataLength());

            Trace.WriteLine("Done!");

            // Drawing.
            ClearCanvases();
            RedrawWaves();
        }

        private void SaveHandler(object sender, RoutedEventArgs e)
        {

        }

        /**
         * Play/Pause Wave
         */
        public void PlayPauseHandler(object sender, RoutedEventArgs e)
        {
            if (!bPlaying)
            {
                PlayPauseIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\pause.png"));
                BeginPlay();
            }
            else
            {
                PlayPauseIcon.Source = (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\play.png"));
                PausePlay();
            }

            bPlaying = !bPlaying;
        }

        /**
         * Stops Playing Wave
         */
        public void StopHandler(object sender, RoutedEventArgs e)
        {
            EndPlay();
        }

        /**
         * Start Recording
         */
        public void RecordHandler(object sender, RoutedEventArgs e)
        {
            if (!bRecording)
            {
                BeginRecord();
            }
            else
            {
                EndRecord();
            }

            bRecording = !bRecording;
        }

        private void WaveScrollHandler(object sender, ScrollChangedEventArgs e)
        {
            ClearCanvases();
            RedrawWaves();
        }

        private void ClearCanvases()
        {
            LeftChannelCanvas.Children.Clear();
            RightChannelCanvas.Children.Clear();
        }

        private void RedrawWaves()
        {
            if (wave != null)
            {
                waveDrawer.DrawWave(wave.GetChannels()[0], ref LeftChannelCanvas, WaveScroll.HorizontalOffset, SystemParameters.PrimaryScreenWidth);
                if (!wave.IsMono())
                {
                    waveDrawer.DrawWave(wave.GetChannels()[1], ref RightChannelCanvas, WaveScroll.HorizontalOffset, SystemParameters.PrimaryScreenWidth);
                }
            }
        }

        private void WaveMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            // Select a portion of the wave from the current mouse position.
            currentSelection = new WaveSelector((int)(e.GetPosition(WaveScroll).X + WaveScroll.HorizontalOffset));
            UpdateSelection(currentSelection.StartX);
        }

        private void WaveMouseMoveHandler(object sender, MouseEventArgs e)
        {
            // Update the selection if the mouse is held down.
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSelection((int)(e.GetPosition(WaveScroll).X + WaveScroll.HorizontalOffset));
            }
        }

        private void UpdateSelection(int xPosition)
        {
            // Return if no wave is found or if there is no current selection.
            if (wave == null || currentSelection == null) return;

            ClearCanvases();

            // Update the selection by giving it the current x position of the mouse and the relevant canvases.
            currentSelection.UpdateSelection(xPosition, ref LeftChannelCanvas);
            if (!wave.IsMono())
            {
                currentSelection.UpdateSelection(xPosition, ref RightChannelCanvas);
            }

            // Redraw the waves on top of the created selection rectangles.
            RedrawWaves();
        }

        private void CutDeleteHandler(object sender, RoutedEventArgs e)
        {
            short[][] temp = wave.ExtractSamples(currentSelection.StartX, currentSelection.CurrentX);

            if (e.Source.Equals(CutButton))
            {
                cutSamples = temp;
            }

            int previousCurrentX = currentSelection.CurrentX;
            currentSelection = new WaveSelector(previousCurrentX);

            UpdateSelection(currentSelection.StartX);

            // BUGGEGGEEDDDD
            foreach(var x in LeftChannelCanvas.Children)
            {
                Trace.WriteLine(x);
            }
        }

        private void PasteHandler(object sender, RoutedEventArgs e)
        {
            wave.InsertSamples(cutSamples, currentSelection.CurrentX);

            ClearCanvases();
            RedrawWaves();
        }
    }
}
