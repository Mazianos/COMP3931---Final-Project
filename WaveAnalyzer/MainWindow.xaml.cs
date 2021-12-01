using System.Diagnostics;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Shapes;
using System.ComponentModel;
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
        private Wave clipboard;
        private const int WaveHeightPadding = 1000;
        private const float IncrementerMultiplier = 0.001f;
        private const float ScrollIntensityMultiplier = 0.0005f;
        private int scrollMultiplier = 1;
        private const int FilterSize = 50;

        public MainWindow()
        {
            InitializeComponent();

            waveDrawer = new WaveDrawer();
            waveZoomer = new WaveZoomer();
            commands = new Commands();
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
            FilterIcon.Source = AppImage.FilterIcon;
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
            PlayPauseButton.IsEnabled = true;
            StopButton.IsEnabled = true;
            Hann.IsEnabled = true;
            Triang.IsEnabled = true;
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

            SampleEntry.Text = wave.SampleRate.ToString();

            Trace.WriteLine("Done!");

            DFTHost.Child = null;

            UpdateScalerMax();
            UpdateScrollerMax();

            WaveScroller.Value = WaveScroller.Minimum;
            ScalerBar.Value = ScalerBar.Maximum;

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
            axisY.Minimum = wave.Channels[0].Min() - WaveHeightPadding;
            axisY.Maximum = wave.Channels[0].Max() + WaveHeightPadding;

            if (wave.IsMono()) return;

            axisY = rightChart.ChartAreas[0].AxisY;
            axisY.Minimum = wave.Channels[1].Min() - WaveHeightPadding;
            axisY.Maximum = wave.Channels[1].Max() + WaveHeightPadding;
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

        /**
         * Play/Pause Wave
         */
        public unsafe void PlayPauseHandler(object sender, RoutedEventArgs e)
        {
            if (!bPlaying)
            {
                OpenButton.IsEnabled = false;
                SaveButton.IsEnabled = false;
                RecordButton.IsEnabled = false;
                ClearButton.IsEnabled = false;
                DFTButton.IsEnabled = false;
                FilterButton.IsEnabled = DFTHost != null;
                PlayPauseIcon.Source = AppImage.PauseIcon;

                // Get the wave data in bytes starting at the cursor position.
                byte[] data = wave.GetBytesFromChannels((int)GetCursorPosition());

                wave.SampleRate = System.Int32.Parse(SampleEntry.Text);

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
                PlayPauseIcon.Source = AppImage.PlayIcon;
                OpenButton.IsEnabled = true;
                SaveButton.IsEnabled = true;
                PlayPauseButton.IsEnabled = true;
                RecordButton.IsEnabled = true;
                ClearButton.IsEnabled = true;
                DFTButton.IsEnabled = true;

                bPaused = !bPaused;

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
            SaveButton.IsEnabled = true;
            ClearButton.IsEnabled = true;
            DFTButton.IsEnabled = true;
            FilterButton.IsEnabled = DFTHost != null;

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

                Wave recordedWave = new Wave();
                recordedWave.Channels = recordedSamples;


                if (wave != null)
                {
                    wave.InsertSamples(recordedWave, (int)GetCursorPosition());
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
            StopButton.IsEnabled = true;

            bRecording = true;

            wave.SampleRate = System.Int32.Parse(SampleEntry.Text);

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

            // Incrementer defines how many samples are drawn per pixel.
            int incrementer = 1;

            if (ScalerBar.Value > ScalerBar.Minimum)
            {
                if (ScalerBar.Value > ScalerBar.Maximum / 2)
                {
                    incrementer = (int)(wave.Channels[0].Length * IncrementerMultiplier);
                }
                else
                {
                    incrementer = (int)(wave.Channels[0].Length * IncrementerMultiplier / 2);
                }
            }

            if (incrementer < 1)
            {
                incrementer = 1;
            }

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
                clipboard = new Wave(wave);
                clipboard.Channels = temp;
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
            DFTHost.Child = null;

            PlayPauseButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            Hann.IsEnabled = false;
            Triang.IsEnabled = false;
            SaveButton.IsEnabled = false;
            FilterButton.IsEnabled = false;

            UpdateScalerMax();
            UpdateScrollerMax();
            ClearCharts();
        }

        private void ScaleCharts(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            ScalerBar.Value -= e.Delta * scrollMultiplier;
        }

        private void PasteHandler(object sender, RoutedEventArgs e)
        {
            wave.InsertSamples(clipboard, (int)GetCursorPosition());
            UpdateScalerMax();
            UpdateScrollerMax();
            ClearCharts();
            RedrawWaves();
        }

        private void DFTHandler(short[][] samples)
        {
            dftChart = ChartCreator.CreateDFTChart();
            DFTHost.Child = dftChart;

            Complex[] leftChannel = Fourier.DFT(samples[0], samples[0].Length, 0);
            Fourier.DivideByN(leftChannel, samples[0].Length);
            double[] leftAmplitudes = Fourier.GetAmplitudes(leftChannel);

            dftChart.ChartAreas[0].AxisY.Maximum = (int)leftAmplitudes.Max() + 1;

            for (int i = 0; i < leftAmplitudes.Length; ++i)
            {
                dftChart.Series[0].Points.AddXY(i, leftAmplitudes[i]);
            }

            //For 2 channels!
            if (samples.Length == 2)
            {
                Complex[] rightChannel = Fourier.DFT(samples[1], samples.Length, 0);
                Fourier.DivideByN(rightChannel, samples[1].Length);
                double[] rightAmplitudes = Fourier.GetAmplitudes(rightChannel);
            }

            FilterButton.IsEnabled = true;
        }

        private void FilterHandler(object sender, RoutedEventArgs e)
        {
            var dftCursor = dftChart.ChartAreas[0].CursorX;

            int rangeStart = (int)dftCursor.SelectionStart;
            int rangeEnd = (int)dftCursor.SelectionEnd;

            if (rangeStart > dftChart.Width / 2)
            {
                rangeStart = dftChart.Width / 2;
            }
            if (rangeEnd > dftChart.Width / 2)
            {
                rangeEnd = dftChart.Width / 2;
            }

            Filter.FilterRange(rangeStart, rangeEnd, dftChart.Series[0].Points.Count, FilterSize, wave.SampleRate, wave.Channels);

            UpdateChartHeights();
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

        private void Triang_Click(object sender, RoutedEventArgs e)
        {
            var cursor = leftChart.ChartAreas[0].CursorX;

            short[][] temp = wave.ExtractSamples((int)(cursor.SelectionStart + WaveScroller.Value), (int)GetCursorPosition(), false);

            temp = Windowing.Triangular(temp);

            DFTHandler(temp);
        }

        private void Hann_Click(object sender, RoutedEventArgs e)
        {
            var cursor = leftChart.ChartAreas[0].CursorX;

            short[][] temp = wave.ExtractSamples((int)(cursor.SelectionStart + WaveScroller.Value), (int)GetCursorPosition(), false);

            temp = Windowing.Hann(temp);

            DFTHandler(temp);
        }
    }
}
