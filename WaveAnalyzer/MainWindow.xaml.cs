using System.Diagnostics;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Forms.DataVisualization.Charting;

namespace WaveAnalyzer
{
    public partial class MainWindow : Window
    {
        private Chart leftChart;
        private Chart rightChart;
        private Chart dftChart;

        private Wave wave;
        private WaveDrawer waveDrawer;
        private WaveZoomer waveZoomer;
        private Commands commands;
        private Thread stopListener;
        private delegate void stopButtonDelegate();
        private bool bRecording;
        private bool bPlaying;
        private bool bPaused = false;
        private static bool die = false;
        private short[][] cutSamples;
        private const int WaveHeightPadding = 1000;
        private const float IncrementerMultiplier = 0.001f;
        private const float ScrollIntensityMultiplier = 0.0005f;
        private int scrollMultiplier = 1;

        public MainWindow()
        {
            InitializeComponent();

            waveDrawer = new WaveDrawer();
            waveZoomer = new WaveZoomer();
            commands = new Commands();
            cutSamples = null;
            wave = new Wave();

            SetIconImages();
            SetupCommands();
            SetupCharts();

            ModelessDialog.InitWave();
        }

        private void SetIconImages()
        {
            OpenIcon.Source = AppImage.OpenIcon;
            SaveIcon.Source = AppImage.SaveIcon;
            PlayPauseIcon.Source = AppImage.PlayIcon;
            StopIcon.Source = AppImage.StopIcon;
            RecordIcon.Source = AppImage.RecordIcon;
            ClearIcon.Source = AppImage.ClearIcon;
            DFTIcon.Source = AppImage.DFTIcon;
        }

        private void SetupCommands()
        {
            CommandBindings.Add(new CommandBinding(commands.Cut, CutCopyDeleteHandler));
            CommandBindings.Add(new CommandBinding(commands.Paste, PasteHandler));
            CommandBindings.Add(new CommandBinding(commands.Delete, CutCopyDeleteHandler));
            CommandBindings.Add(new CommandBinding(commands.Copy, CutCopyDeleteHandler));
        }

        private void SetupCharts()
        {
            leftChart = ChartCreator.CreateChart();
            rightChart = ChartCreator.CreateChart();

            LeftHost.Child = leftChart;
            RightHost.Child = rightChart;

            leftChart.SelectionRangeChanging += ChartSelectionHandler;
            rightChart.SelectionRangeChanging += ChartSelectionHandler;

            leftChart.MouseWheel += ScaleCharts;
            rightChart.MouseWheel += ScaleCharts;
        }

        private void ChartSelectionHandler(object sender, CursorEventArgs e)
        {
            if (sender == leftChart)
            {
                var cursorX = leftChart.ChartAreas[0].CursorX;
                SyncCursors(cursorX.SelectionStart, cursorX.SelectionEnd);
            }
            else
            {
                var cursorX = rightChart.ChartAreas[0].CursorX;
                SyncCursors(cursorX.SelectionStart, cursorX.SelectionEnd);
            }
        }

        private void SyncCursors(double start, double end)
        {
            var cursorX = leftChart.ChartAreas[0].CursorX;
            cursorX.SelectionStart = start;
            cursorX.SelectionEnd = end;
            cursorX.Position = end;

            cursorX = rightChart.ChartAreas[0].CursorX;
            cursorX.SelectionStart = start;
            cursorX.SelectionEnd = end;
            cursorX.Position = end;
        }

        private double GetCursorPosition()
        {
            return leftChart.ChartAreas[0].CursorX.SelectionEnd + WaveScroller.Value;
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

            if (stopListener != null)
            {
                die = true;
            }

            if (bPlaying)
            {
                StopButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }

            // Read the wave file in bytes.
            wave = new Wave(openFileDialog.FileName);

            Trace.WriteLine("Done!");

            WaveScroller.Value = WaveScroller.Minimum;
            ScalerBar.Value = ScalerBar.Minimum;

            UpdateScalerMax();
            UpdateScrollerMax();
            UpdateChartHeights();

            // Drawing.
            ClearCharts();
            RedrawWaves();
        }

        private void UpdateScalerMax()
        {
            if (wave == null) return;

            ScalerBar.Maximum = wave.Subchunk2Size / 2 / wave.NumChannels;
            scrollMultiplier = (int)(wave.Channels[0].Length * ScrollIntensityMultiplier);
        }

        private void UpdateChartHeights()
        {
            if (leftChart == null) return;

            var axisY = leftChart.ChartAreas[0].AxisY;
            axisY.Minimum = waveDrawer.GetMinSample(wave.Channels[0]) - WaveHeightPadding;
            axisY.Maximum = waveDrawer.GetMaxSample(wave.Channels[0]) + WaveHeightPadding;

            if (wave.IsMono()) return;

            axisY = rightChart.ChartAreas[0].AxisY;
            axisY.Minimum = waveDrawer.GetMinSample(wave.Channels[1]) - WaveHeightPadding;
            axisY.Maximum = waveDrawer.GetMaxSample(wave.Channels[1]) + WaveHeightPadding;
        }

        private void UpdateScrollerMax()
        {
            if (wave == null || leftChart == null || WaveScroller == null || ScalerBar == null) return;
            
            WaveScroller.Maximum = wave.Subchunk2Size / 2 / wave.NumChannels - leftChart.Width - ScalerBar.Value;

            if (WaveScroller.Maximum < 0)
            {
                WaveScroller.Maximum = 0;
            }
        }

        private void SaveHandler(object sender, RoutedEventArgs e)
        {
            wave.Save();
        }

        private void MoveCursorWhilePlaying()
        {
            
        }

        /**
         * Play/Pause Wave
         */
        public unsafe void PlayPauseHandler(object sender, RoutedEventArgs e)
        {
            if (!bPlaying)
            {
                RecordButton.IsEnabled = false;
                PlayPauseIcon.Source = AppImage.PauseIcon;

                // Get the wave data in bytes starting at the cursor position.
                byte[] data = wave.GetBytesFromChannels((int)GetCursorPosition());

                fixed (byte* p = data)
                {
                    ModelessDialog.SetWaveData(p, (uint)data.Length, wave.NumChannels, wave.SampleRate, wave.BlockAlign, wave.BitsPerSample);
                }
                ModelessDialog.BeginPlay();
                die = false;
                stopListener = new Thread(Listen);
                stopListener.Start();
                bPlaying = true;
            }
            else
            {
                PlayPauseIcon.Source = bPaused ? AppImage.PlayIcon : AppImage.PauseIcon;
                bPaused = !bPaused;
                RecordButton.IsEnabled = !RecordButton.IsEnabled;

                ModelessDialog.PausePlay();
            }
        }

        /**
         * Stops Playing Wave
         */
        public unsafe void StopHandler(object sender, RoutedEventArgs e)
        {
            OpenButton.IsEnabled = true;
            SaveButton.IsEnabled = true;
            PlayPauseButton.IsEnabled = true;
            RecordButton.IsEnabled = true;

            PlayPauseIcon.Source = AppImage.PlayIcon;

            if (bPlaying)
            {
                ModelessDialog.EndPlay();
                bPlaying = false;
            }
            else if (bRecording)
            {
                ModelessDialog.EndRecord();

                int recordedLength = (int)ModelessDialog.GetDWDataLength();
                byte[] recordedBytes = new byte[recordedLength];
                Marshal.Copy(ModelessDialog.GetSaveBuffer(), recordedBytes, 0, recordedLength);
                short[][] recordedSamples = Wave.GetChannelsFromBytes(ref recordedBytes, recordedLength / 2, 0, 1);

                if (wave != null)
                {
                    wave.InsertSamples(recordedSamples, (int)GetCursorPosition());
                }

                UpdateScalerMax();
                UpdateScrollerMax();
                UpdateChartHeights();
                ClearCharts();
                RedrawWaves();
            }
            
            if (!bRecording)
            {
                // Reset cursor to the beginning of the track.
                SyncCursors(0, 0);
                WaveScroller.Value = 0;
            }

            bRecording = false;
        }

        /**
         * Start Recording
         */
        public unsafe void RecordHandler(object sender, RoutedEventArgs e)
        {
            OpenButton.IsEnabled = false;
            SaveButton.IsEnabled = false;
            PlayPauseButton.IsEnabled = false;
            RecordButton.IsEnabled = false;

            bRecording = true;

            fixed (byte* p = new byte[1])
            {
                ModelessDialog.SetWaveData(p, 1, wave.NumChannels, wave.SampleRate, wave.BlockAlign, wave.BitsPerSample);
            }
            
            ModelessDialog.BeginRecord();
        }

        private void ClearCharts()
        {
            if (leftChart == null) return;
            leftChart.Series[0].Points.Clear();

            if (rightChart == null) return;
            rightChart.Series[0].Points.Clear();
        }

        private void RedrawWaves()
        {
            if (wave == null) return;

            // Incrementer defines how many samples are drawn per pixel. If the scalerbar is a hundredth 
            int incrementer = ScalerBar.Value == ScalerBar.Minimum ? 1 : (int)(wave.Channels[0].Length * IncrementerMultiplier);

            waveDrawer.DrawWave(wave.Channels[0], ref leftChart, (int)WaveScroller.Value, leftChart.Width + ScalerBar.Value, incrementer);
            
            if (wave.IsMono()) return;
            waveDrawer.DrawWave(wave.Channels[1], ref rightChart, (int)WaveScroller.Value, rightChart.Width + ScalerBar.Value, incrementer);
        }

        private void WaveScrollHandler(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ClearCharts();
            RedrawWaves();
        }

        private void CutCopyDeleteHandler(object sender, ExecutedRoutedEventArgs e)
        {
            var cursor = leftChart.ChartAreas[0].CursorX;

            bool isCopying = e.Command == commands.Copy;

            short[][] temp = wave.ExtractSamples((int)(cursor.SelectionStart + WaveScroller.Value), (int)GetCursorPosition(), !isCopying);

            if (e.Command == commands.Cut || e.Command == commands.Copy)
            {
                cutSamples = temp;
            }

            SyncCursors(cursor.SelectionStart, cursor.SelectionStart);
            UpdateScalerMax();
            UpdateScrollerMax();
            ClearCharts();
            RedrawWaves();
        }

        public void ClearHandler(object sender, RoutedEventArgs e)
        {
            wave = new Wave();

            UpdateScalerMax();
            UpdateScrollerMax();
            ClearCharts();
        }

        private void ScaleCharts(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            ScalerBar.Value += e.Delta * scrollMultiplier;
        }

        private void PasteHandler(object sender, RoutedEventArgs e)
        {
            wave.InsertSamples(cutSamples, (int)GetCursorPosition());
            UpdateScalerMax();
            UpdateScrollerMax();
            ClearCharts();
            RedrawWaves();
        }

        private const int SAMPLES_AT_A_TIME = 5000;
        private void DFTHandler(object sender, RoutedEventArgs e)
        {
            /*Complex[] A = Fourier.DFT(wave.Channels[0], SAMPLES_AT_A_TIME, 0);
            for (int i = 1; i < wave.Channels[0].Length / SAMPLES_AT_A_TIME; i++)
            {
                Complex[] A2 = Fourier.DFT(wave.Channels[0], SAMPLES_AT_A_TIME, i * SAMPLES_AT_A_TIME);
                for (int j = 0; j < A.Length; j++)
                {
                    A[j] += A2[j];
                }
            }
            Fourier.DivideByN(A, SAMPLES_AT_A_TIME);*/
            //Fourier.PrintDoubles(Fourier.GetAmplitudes(A));
            //Fourier.PrintComplex(A);


            dftChart = ChartCreator.CreateDFTChart();
            DFTHost.Child = dftChart;

            //short[][] deez2 = Windowing.Triangular(wave.Channels);
            short[][] deez = Windowing.Hann(wave.Channels);

            Complex[] test = Fourier.DFT(deez[0], SAMPLES_AT_A_TIME, 10000);
            Fourier.DivideByN(test, SAMPLES_AT_A_TIME);

            double[] amplitudes = Fourier.GetAmplitudes(test);

            dftChart.ChartAreas[0].AxisY.Maximum = (int)amplitudes.Max() + 1;

            for (int i = 0; i < amplitudes.Length; ++i)
            {
                dftChart.Series[0].Points.AddXY(i, amplitudes[i]);
            }
        }

        private void FilterHandler(object sender, RoutedEventArgs e)
        {
            //var dftCursor = dftChart.ChartAreas[0].CursorX;
            //var sampleCursor = leftChart.ChartAreas[0].CursorX;

            //short[][] samplesToFilter = wave.ExtractSamples((int)sampleCursor.SelectionStart, (int)sampleCursor.SelectionEnd, true);
            short[][] samplesToFilter = wave.ExtractSamples(10000, 30000, true);

            //Filter.FilterRange((int)dftCursor.SelectionStart, (int)dftCursor.SelectionEnd, samplesToFilter);
            Filter.FilterRange(0, 10, wave.SampleRate, samplesToFilter);
            wave.InsertSamples(samplesToFilter, 10000);

            ClearCharts();
            RedrawWaves();
        }

        private void PressStop()
        {
            StopButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void Listen()
        {
            while (!die)
            {
                die = ModelessDialog.checkStopped();
                if (die)
                {
                    stopButtonDelegate del = new stopButtonDelegate(PressStop);
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, del);
                }
            }
            die = false;
            ModelessDialog.setStopped(false);
            return;
        }

        private void ScalerHandler(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateScrollerMax();
            ClearCharts();
            RedrawWaves();
        }
    }
}
