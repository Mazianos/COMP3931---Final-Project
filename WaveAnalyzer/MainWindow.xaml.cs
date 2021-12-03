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
//564 lines before adding comments

namespace WaveAnalyzer
{
    public partial class MainWindow : Window
    {
        private Chart leftChart;
        private Chart rightChart;
        private Chart dftChart;

        private Wave wave;                                          // Currently loaded wave
        private WaveDrawer waveDrawer;                              // Draws the currently loaded wave
        private Commands commands;                                  // Enables hotkeys to be used
        private Thread stopListener;                                // Waits for the wave to stop before resetting the window
        private delegate void stopButtonDelegate();                 // Allows the stopListener to tell the main window that the wave has stopped
        private bool bRecording;                                    // Are we recording?
        private bool bPlaying;                                      // Are we playing?
        private bool bPaused = false;                               // Are we paused?
        private static bool die = false;                            // Should the stopListener finish?
        private Wave clipboard;                                     // Wave being saved in our clipboard
        private const int WaveHeightPadding = 1000;                 // Pads space in y axis
        private const float IncrementerMultiplier = 0.001f;         // Helps determine how many samples are drawn per pixel
        private const float ScrollIntensityMultiplier = 0.0005f;    // Helps determine how much we zoom at a time
        private int scrollMultiplier = 1;                           // Default value for how much we zoom at a time
        private const int FilterSize = 50;                          // Default value for how many samples we filter at a time

        /// <summary>
        /// Initialize the window, wave, waveDrawer, icons, hotkeys/shortcuts, recorder, and charts
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            waveDrawer = new WaveDrawer();
            commands = new Commands();
            wave = new Wave();

            SetIconImages();
            SetupCommands();
            SetupCharts();

            ModelessDialog.InitWave();
        }

        /// <summary>
        /// Sets the icons
        /// </summary>
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

        /// <summary>
        /// Sets the hotkeys/shortcuts
        /// </summary>
        private void SetupCommands()
        {
            CommandBindings.Add(new CommandBinding(commands.Cut, CutCopyDeleteHandler));
            CommandBindings.Add(new CommandBinding(commands.Paste, PasteHandler));
            CommandBindings.Add(new CommandBinding(commands.Delete, CutCopyDeleteHandler));
            CommandBindings.Add(new CommandBinding(commands.Copy, CutCopyDeleteHandler));
        }

        /// <summary>
        /// Initializes the charts
        /// </summary>
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

        /// <summary>
        /// Allows the user to select portions of the charts
        /// </summary>
        /// <param name="sender">Which chart was selected</param>
        /// <param name="e">Event that triggered this</param>
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

        /// <summary>
        /// Ensures that the red lines on both time-domain wave graphs are synchronized
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="end">Ending position</param>
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

        /// <summary>
        /// Get where the cursor is at the moment
        /// </summary>
        /// <returns>Current cursor location</returns>
        private double GetCursorPosition()
        {
            return leftChart.ChartAreas[0].CursorX.SelectionEnd + WaveScroller.Value;
        }

        /// <summary>
        /// Open a file
        /// </summary>
        /// <param name="sender">Open button</param>
        /// <param name="e">Event that called this</param>
        public unsafe void OpenHandler(object sender, RoutedEventArgs e)
        {
            PlayPauseButton.IsEnabled = true;
            StopButton.IsEnabled = true;
            SaveButton.IsEnabled = true;
            Hann.IsEnabled = true;
            Triang.IsEnabled = true;
            // Opens the open file dialog box.
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "WAV files (*.wav)|*.wav|Modified RLE files (*.rmle)|*.rmle|All files (*.*)|*.*"
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

        /// <summary>
        /// Update max length of wave displayed
        /// </summary>
        private void UpdateScalerMax()
        {
            if (wave == null) return;

            ScalerBar.Maximum = wave.Subchunk2Size / 2 / wave.NumChannels;
            scrollMultiplier = (int)(wave.Channels[0].Length * ScrollIntensityMultiplier);
        }

        /// <summary>
        /// Update max height of wave displayed
        /// </summary>
        private void UpdateChartHeights()
        {
            if (leftChart == null) return;

            var axisY = leftChart.ChartAreas[0].AxisY;
            axisY.Minimum = wave.Channels[0].Min() - WaveHeightPadding;
            axisY.Maximum = wave.Channels[0].Max() + WaveHeightPadding;

            var axisX = leftChart.ChartAreas[0].AxisX;
            TickMark tm = new TickMark();
            tm.Size = wave.SampleRate;
            axisX.MajorGrid.Enabled = true;
            axisX.MajorTickMark = tm;

            if (wave.IsMono()) return;

            axisY = rightChart.ChartAreas[0].AxisY;
            axisY.Minimum = wave.Channels[1].Min() - WaveHeightPadding;
            axisY.Maximum = wave.Channels[1].Max() + WaveHeightPadding;

            axisX = rightChart.ChartAreas[0].AxisX;
        }

        /// <summary>
        /// Update current scroll bar
        /// </summary>
        private void UpdateScrollerMax()
        {
            if (wave == null || leftChart == null || WaveScroller == null || ScalerBar == null) return;
            
            WaveScroller.Maximum = wave.Subchunk2Size / 2 / wave.NumChannels - leftChart.Width - ScalerBar.Value;

            if (WaveScroller.Maximum < 0)
            {
                WaveScroller.Maximum = 0;
            }
        }

        /// <summary>
        /// Save a file
        /// </summary>
        /// <param name="sender">Save button</param>
        /// <param name="e">Event that called this</param>
        private void SaveHandler(object sender, RoutedEventArgs e)
        {
            wave.Save();
        }


        /// <summary>
        /// Play the current wave OR resume/pause the wave appropriately
        /// </summary>
        /// <param name="sender">Play/Pause button</param>
        /// <param name="e">Event that called this</param>
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
                if (bPaused)
                {
                    OpenButton.IsEnabled = false;
                    SaveButton.IsEnabled = false;
                    RecordButton.IsEnabled = false;
                    ClearButton.IsEnabled = false;
                    DFTButton.IsEnabled = false;
                    FilterButton.IsEnabled = DFTHost != null;
                    PlayPauseIcon.Source = AppImage.PauseIcon;
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
                }
                bPaused = !bPaused;

                ModelessDialog.PausePlay();
            }
        }

        /// <summary>
        /// Stop playing a wave OR stop recording, reset the button statuses
        /// </summary>
        /// <param name="sender">Stop button</param>
        /// <param name="e">Event that called this</param>
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
                bPaused = false;

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

        /// <summary>
        /// Begin Recording
        /// </summary>
        /// <param name="sender">Record button</param>
        /// <param name="e">Event that called this</param>
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

        /// <summary>
        /// Clear all charts, removing all points from them
        /// </summary>
        private void ClearCharts()
        {
            if (leftChart == null) return;
            leftChart.Series[0].Points.Clear();

            if (rightChart == null) return;
            rightChart.Series[0].Points.Clear();
        }

        /// <summary>
        /// Redraw the wave(s) on the appropriate charts
        /// </summary>
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

            int rate;
            string time;
            if (wave.Subchunk2Size / (wave.BitsPerSample / 8) / wave.SampleRate < 120)
            {
                rate = wave.SampleRate;
                time = "Seconds";
            }
            else
            {
                rate = wave.SampleRate * 60;
                time = "Minutes";
            }

            var axisX = leftChart.ChartAreas[0].AxisX;
            axisX.MajorTickMark = new TickMark();
            axisX.MajorTickMark.Size = 3;
            axisX.MajorTickMark.Interval = rate;
            axisX.Title = time;

            if (wave.IsMono()) return;
            waveDrawer.DrawWave(wave.Channels[1], ref rightChart, (int)WaveScroller.Value, rightChart.Width + ScalerBar.Value, incrementer);

            axisX = rightChart.ChartAreas[0].AxisX;
            axisX.MajorTickMark = new TickMark();
            axisX.MajorTickMark.Size = 3;
            axisX.MajorTickMark.Interval = rate;
            axisX.Title = time;
        }

        /// <summary>
        /// Move a wave along the chart
        /// </summary>
        /// <param name="sender">The scroll bar being moved</param>
        /// <param name="e">Event sending this</param>
        private void WaveScrollHandler(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ClearCharts();
            RedrawWaves();
        }

        /// <summary>
        /// Handle hotkeys
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Command that called this</param>
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


        /// <summary>
        /// Clear the charts and reset the buttons
        /// </summary>
        /// <param name="sender">Clear button</param>
        /// <param name="e">Event that called this</param>
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

        /// <summary>
        /// Scale the charts according to the current scroll bar positions
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e"></param>
        private void ScaleCharts(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            ScalerBar.Value -= e.Delta * scrollMultiplier;
        }

        /// <summary>
        /// Paste the wave in the clipboard to the wave on the chart
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        private void PasteHandler(object sender, RoutedEventArgs e)
        {
            wave.InsertSamples(clipboard, (int)GetCursorPosition());
            UpdateScalerMax();
            UpdateScrollerMax();
            ClearCharts();
            RedrawWaves();
        }

        /// <summary>
        /// DFT selected samples and display them on the chart
        /// </summary>
        /// <param name="samples">Array of samples to DFT</param>
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

        /// <summary>
        /// Filter the selected frequencies
        /// </summary>
        /// <param name="sender">Filter button</param>
        /// <param name="e">Click event</param>
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

        /// <summary>
        /// Used in a delegate to press the stop button on the main window
        /// </summary>
        private void PressStop()
        {
            StopButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        /// <summary>
        /// Threadproc used to listen for the end of the wave being played.
        /// </summary>
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

        /// <summary>
        /// Scale the waves, clear the charts and redraw the waves
        /// </summary>
        /// <param name="sender">Scrollbar</param>
        /// <param name="e">Unused</param>
        private void ScalerHandler(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateScrollerMax();
            ClearCharts();
            RedrawWaves();
        }

        /// <summary>
        /// Window the selected samples using a Triangle window
        /// </summary>
        /// <param name="sender">Triangle Window Button</param>
        /// <param name="e">Click event</param>
        private void Triang_Click(object sender, RoutedEventArgs e)
        {
            var cursor = leftChart.ChartAreas[0].CursorX;

            short[][] temp = wave.ExtractSamples((int)(cursor.SelectionStart + WaveScroller.Value), (int)GetCursorPosition(), false);

            temp = Windowing.Triangular(temp);

            DFTHandler(temp);
        }

        /// <summary>
        /// Window the selected samples using a Hann window
        /// </summary>
        /// <param name="sender">Hann Window Button</param>
        /// <param name="e">Click event</param>
        private void Hann_Click(object sender, RoutedEventArgs e)
        {
            var cursor = leftChart.ChartAreas[0].CursorX;

            short[][] temp = wave.ExtractSamples((int)(cursor.SelectionStart + WaveScroller.Value), (int)GetCursorPosition(), false);

            temp = Windowing.Hann(temp);

            DFTHandler(temp);
        }
    }
}
