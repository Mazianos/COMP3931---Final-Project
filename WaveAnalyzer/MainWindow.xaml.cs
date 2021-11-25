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

namespace WaveAnalyzer
{
    public partial class MainWindow : Window
    {
        private Chart leftChart;
        private Chart rightChart;
        private Wave wave;
        private WaveDrawer waveDrawer;
        private WaveZoomer waveZoomer;
        private Commands commands;
        private IntPtr hwnd;
        private bool bRecording;
        private bool bPlaying;
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
            waveZoomer.HandleZoom(ref leftChart, e.Delta, e.X);
            waveZoomer.HandleZoom(ref rightChart, e.Delta, e.X);
        }

        private void ChartSelectionHandler(object sender, CursorEventArgs e)
        {
            if (sender == leftChart)
            {
                SyncCursors(leftChart.ChartAreas[0].CursorX.Position);
            }
            else
            {
                SyncCursors(rightChart.ChartAreas[0].CursorX.Position);
            }
        }

        private void SyncCursors(double position)
        {
            leftChart.ChartAreas[0].CursorX.Position = position;
            rightChart.ChartAreas[0].CursorX.Position = position;
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
            short[] channel = wave.Channels[0];
            byte[] dataArr = new byte[channel.Length];
            Buffer.BlockCopy(channel, 0, dataArr, 0, channel.Length);
            byte* Pdata;
            fixed (byte* data = dataArr)
            {
                Pdata = data;
            }
            //SetSaveBuffer(Pdata);
            //SetDWDataLength((uint)wave.GetDataLength());

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
        public void PlayPauseHandler(object sender, RoutedEventArgs e)
        {
            if (!bPlaying)
            {
                PlayPauseIcon.Source = AppImage.PauseIcon;
                ModelessDialog.BeginPlay();
            }
            else
            {
                PlayPauseIcon.Source = AppImage.PlayIcon;
                ModelessDialog.PausePlay();
            }

            bPlaying = !bPlaying;
        }

        /**
         * Stops Playing Wave
         */
        public void StopHandler(object sender, RoutedEventArgs e)
        {
            ModelessDialog.EndRecord();
            bRecording = false;
        }

        /**
         * Start Recording
         */
        public void RecordHandler(object sender, RoutedEventArgs e)
        {
            ModelessDialog.BeginRecord();
            bRecording = true;
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
            short[][] temp = wave.ExtractSamples((int)(cursor.SelectionStart + WaveScroller.Value), (int)(cursor.SelectionEnd + WaveScroller.Value));

            if (e.Command == commands.Cut)
            {
                cutSamples = temp;
            }

            SyncCursors(cursor.SelectionStart);

            ClearCharts();
            RedrawWaves();
        }

        private void PasteHandler(object sender, RoutedEventArgs e)
        {
            wave.InsertSamples(cutSamples, (int)(leftChart.ChartAreas[0].CursorX.SelectionEnd + WaveScroller.Value));

            ClearCharts();
            RedrawWaves();
        }
    }
}
