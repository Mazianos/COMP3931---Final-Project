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
        public static extern unsafe void SetSaveBuffer(byte[] saveBuffer);

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
        private bool isPlaying;
        private WaveSelector currentSelection;
        private short[][] cutSamples;

        private Color waveColor = new Color()
        {
            R = 248,
            G = 175,
            B = 96,
            A = 255
        };
        private Color selectionColor = new Color()
        {
            R = 96,
            G = 175,
            B = 248,
            A = 255
        };
        private Color selectorColor = new Color()
        {
            R = 255,
            G = 100,
            B = 100,
            A = 255
        };

        public MainWindow()
        {
            InitializeComponent();

            waveDrawer = new WaveDrawer(waveColor);
            cutSamples = null;

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
            short[] channel = wave.GetChannels()[0];
            byte[] data = new byte[channel.Length];
            Buffer.BlockCopy(channel, 0, data, 0, channel.Length);
            SetSaveBuffer(data);


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
         * Start Recording
         */
        public void RecordHandler(object sender, RoutedEventArgs e)
        {

            currentSelection.DrawSelector(ref LeftChannelCanvas);
            if (!wave.IsMono())
            {
                currentSelection.DrawSelector(ref RightChannelCanvas);
            }
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
            currentSelection = new WaveSelector((int)(e.GetPosition(WaveScroll).X + WaveScroll.HorizontalOffset), selectionColor, selectorColor);
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
            if (wave == null || currentSelection == null)
            {
                return;
            }

            ClearCanvases();

            // Update the selection by giving it the current x position of the mouse and the relevant canvases.
            currentSelection.UpdateSelection(xPosition, ref LeftChannelCanvas);
            if (!wave.IsMono())
            {
                currentSelection.UpdateSelection(xPosition, ref RightChannelCanvas);
            }

            // Redraw the waves on top of the created selection rectangles.
            RedrawWaves();
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
        
        private void CutDeleteHandler(object sender, RoutedEventArgs e)
        {
            short[][] temp = wave.ExtractSamples(currentSelection.StartX, currentSelection.CurrentX);
            
            if (e.Source.Equals(CutButton))
            {
                cutSamples = temp;
            }

            int previousCurrentX = currentSelection.CurrentX;
            currentSelection = new WaveSelector(previousCurrentX, selectionColor, selectorColor);
            UpdateSelection(currentSelection.StartX);
            // WHY AREN'T YOU DRAWING THE DAMN LINE??
            /*foreach(var x in LeftChannelCanvas.Children)
            {
                Trace.WriteLine(x);
            }*/
        }

        private void PasteHandler(object sender, RoutedEventArgs e)
        {
            wave.InsertSamples(cutSamples, currentSelection.CurrentX);

            ClearCanvases();
            RedrawWaves();
        }
    }
}
