using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;

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
        private short leftMinSample;
        private short leftMaxSample;
        private short rightMaxSample;
        private short rightMinSample;

        public MainWindow()
        {
            InitializeComponent();

            waveDrawer = new WaveDrawer();
            waveZoomer = new WaveZoomer();
            commands = new Commands();
            cutSamples = null;

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
        }

        private void SetupCommands()
        {
            CommandBindings.Add(new CommandBinding(commands.Cut, CutDeleteHandler));
            CommandBindings.Add(new CommandBinding(commands.Paste, PasteHandler));
            CommandBindings.Add(new CommandBinding(commands.Delete, CutDeleteHandler));
        }

        private void SetupCharts()
        {
            leftChart = ChartCreator.CreateChart();
            rightChart = ChartCreator.CreateChart();

            LeftHost.Child = leftChart;
            RightHost.Child = rightChart;

            leftChart.MouseWheel += ChartMouseWheelHandler;
            rightChart.MouseWheel += ChartMouseWheelHandler;

            leftChart.SelectionRangeChanging += ChartSelectionHandler;
            rightChart.SelectionRangeChanging += ChartSelectionHandler;
        }

        private void ChartMouseWheelHandler(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (wave == null) return;

            waveZoomer.HandleZoom(ref leftChart, e.Delta, e.X);
            if (!wave.IsMono())
            {
                waveZoomer.HandleZoom(ref rightChart, e.Delta, e.X);
            }
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

            leftMinSample = waveDrawer.GetMinSample(wave.Channels[0]);
            leftMaxSample = waveDrawer.GetMaxSample(wave.Channels[0]);

            if (!wave.IsMono())
            {
                rightMinSample = waveDrawer.GetMinSample(wave.Channels[1]);
                rightMaxSample = waveDrawer.GetMaxSample(wave.Channels[1]);
            }

            WaveScroller.Maximum = wave.Subchunk2Size / 2 / wave.NumChannels - 1000;

            // Drawing.
            ClearCharts();
            RedrawWaves();
        }

        private void SaveHandler(object sender, RoutedEventArgs e)
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
                byte[] data = wave.GetChannelsInBytes((int)GetCursorPosition());

                fixed (byte* p = data)
                {
                    ModelessDialog.SetWaveData(p, (uint)data.Length, wave.NumChannels, wave.SampleRate, wave.BlockAlign, wave.BitsPerSample);
                }
                ModelessDialog.BeginPlay();
                die = false;
                stopListener = new Thread(listen);
                stopListener.Start();
                bPlaying = true;
            }
            else
            {
                if (!bPaused)
                {
                    RecordButton.IsEnabled = true;
                    PlayPauseIcon.Source = AppImage.PlayIcon;
                    bPaused = true;
                } else
                {
                    RecordButton.IsEnabled = false;
                    PlayPauseIcon.Source = AppImage.PauseIcon;
                    bPaused = false;
                }
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
                short[][] recordedSamples = Wave.ExtractSamples(ref recordedBytes, recordedLength / 2, 0, 1);

                if (wave != null)
                {
                    wave.InsertSamples(recordedSamples, (int)GetCursorPosition());
                }

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
            ModelessDialog.BeginRecord();
        }

        private void ClearCharts()
        {
            leftChart.Series[0].Points.Clear();
            rightChart.Series[0].Points.Clear();
        }

        private void RedrawWaves()
        {
            if (wave != null)
            {
                waveDrawer.DrawWave(wave.Channels[0], ref leftChart, (int)WaveScroller.Value, leftChart.Width, leftMinSample, leftMaxSample);
                if (!wave.IsMono())
                {
                    waveDrawer.DrawWave(wave.Channels[1], ref rightChart, (int)WaveScroller.Value, rightChart.Width, rightMinSample, rightMaxSample);
                }
            }
        }

        private void WaveScrollHandler(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ClearCharts();
            RedrawWaves();
        }

        private void CutDeleteHandler(object sender, ExecutedRoutedEventArgs e)
        {
            var cursor = leftChart.ChartAreas[0].CursorX;
            short[][] temp = wave.ExtractSamples((int)(cursor.SelectionStart + WaveScroller.Value), (int)GetCursorPosition());

            if (e.Command == commands.Cut)
            {
                cutSamples = temp;
            }

            SyncCursors(cursor.SelectionStart, cursor.SelectionStart);

            ClearCharts();
            RedrawWaves();
        }

        private void PasteHandler(object sender, RoutedEventArgs e)
        {
            wave.InsertSamples(cutSamples, (int)GetCursorPosition());

            ClearCharts();
            RedrawWaves();
        }


        private const int SAMPLES_AT_A_TIME = 1000;
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


            Complex[] test = Fourier.DFT(wave.Channels[0], SAMPLES_AT_A_TIME, 0);
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
            var dftCursor = dftChart.ChartAreas[0].CursorX;
            var sampleCursor = leftChart.ChartAreas[0].CursorX;

            //short[][] samplesToFilter = wave.ExtractSamples((int)sampleCursor.SelectionStart, (int)sampleCursor.SelectionEnd);
            short[][] samplesToFilter = wave.ExtractSamples(0, 1000);

            Filter.FilterRange((int)dftCursor.SelectionStart, (int)dftCursor.SelectionEnd, samplesToFilter);

            wave.InsertSamples(samplesToFilter, 0);

            ClearCharts();
            RedrawWaves();
        }


        private void pressStop()
        {
            StopButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void listen()
        {
            while (!die)
            {
                die = ModelessDialog.checkStopped();
                if (die)
                {
                    stopButtonDelegate del = new stopButtonDelegate(pressStop);
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, del);
                }
            }
            die = false;
            ModelessDialog.setStopped(false);
            return;
        }
    }
}
