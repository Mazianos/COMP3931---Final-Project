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
using System.Windows.Forms.DataVisualization.Charting;

namespace WaveAnalyzer
{
    public partial class MainWindow : Window
    {
        private Wave wave;
        private WaveDrawer waveDrawer;
        private bool bRecording;
        private bool bPlaying;
        private IntPtr hwnd;
        


        private Chart leftChart;
        private Chart rightChart;

        private bool isPlaying;
        private bool isRecording = false;
        private short[][] cutSamples;

        public MainWindow()
        {
            InitializeComponent();
            CreateChart();

            waveDrawer = new WaveDrawer();
            cutSamples = null;

            SetIconImages();

            //InitWave();
        }

        private void CreateChart()
        {
            leftChart = new Chart();
            rightChart = new Chart();

            leftChart.Height = (int)(SystemParameters.PrimaryScreenHeight / 4);
            rightChart.Height = (int)(SystemParameters.PrimaryScreenHeight / 4);
            leftChart.BackColor = System.Drawing.Color.Transparent;
            rightChart.BackColor = System.Drawing.Color.Transparent;

            Series leftSeries = new Series();
            Series rightSeries = new Series();

            leftChart.Series.Add(leftSeries);
            rightChart.Series.Add(rightSeries);

            leftChart.Series[0].ChartType = SeriesChartType.FastLine;
            rightChart.Series[0].ChartType = SeriesChartType.FastLine;

            ChartArea leftArea = new ChartArea();
            ChartArea rightArea = new ChartArea();

            var axisX = leftArea.AxisX;
            axisX.ScrollBar.Enabled = false;
            axisX.ScaleView.Zoomable = false;
            axisX.LabelStyle.Enabled = false;

            var axisY = leftArea.AxisY;
            axisY.Minimum = -1;
            axisY.Maximum = 1;
            axisY.MajorGrid.Enabled = false;

            axisX = rightArea.AxisX;
            axisX.ScrollBar.Enabled = false;
            axisX.ScaleView.Zoomable = false;
            axisX.LabelStyle.Enabled = false;

            axisY = rightArea.AxisY;
            axisY.Minimum = -1;
            axisY.Maximum = 1;
            axisY.MajorGrid.Enabled = false;
            //chartArea.BackColor = System.Drawing.Color.Transparent;

            var cursorX = leftArea.CursorX;
            cursorX.IsUserEnabled = true;
            cursorX.IsUserSelectionEnabled = true;

            cursorX = rightArea.CursorX;
            cursorX.IsUserEnabled = true;
            cursorX.IsUserSelectionEnabled = true;

            leftChart.ChartAreas.Add(leftArea);
            rightChart.ChartAreas.Add(rightArea);

            LeftHost.Child = leftChart;
            RightHost.Child = rightChart;

            leftChart.Click += ChartClick;
            rightChart.Click += ChartClick;

            leftChart.MouseWheel += LeftChart_MouseWheel;

            leftChart.SelectionRangeChanging += LeftChart_SelectionRangeChanging;

        }

        private void LeftChart_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            ZoomHandler(e.Delta, e.X);
        }

        private void LeftChart_SelectionRangeChanging(object sender, CursorEventArgs e)
        {
            rightChart.ChartAreas[0].CursorX.Position = leftChart.ChartAreas[0].CursorX.Position;
        }

        private void ChartClick(object sender, EventArgs e)
        {
            var chart = (Chart)sender;
            if (chart == null) return;
            Trace.WriteLine(chart.ChartAreas[0].CursorX.SelectionStart + " " + chart.ChartAreas[0].CursorX.SelectionEnd);

            
        }

        private class Zooms
        {
            public double Start { get; set; }
            public double End { get; set; }
        }
        
        private readonly Stack<Zooms> leftZooms = new Stack<Zooms>();
        private readonly Stack<Zooms> rightZooms = new Stack<Zooms>();


        private void ZoomHandler(double delta, double position)
        {
            var leftAxisX = leftChart.ChartAreas[0].AxisX;
            var rightAxisX = rightChart.ChartAreas[0].AxisX;
            leftAxisX.ScaleView.Zoomable = true;
            rightAxisX.ScaleView.Zoomable = true;

            if (delta < 0)
            {
                if (leftZooms.Count > 0)
                {
                    var leftZoom = leftZooms.Pop();
                    var rightZoom = rightZooms.Pop();
                    if (leftZooms.Count == 0)
                    {
                        leftAxisX.ScaleView.ZoomReset();
                        rightAxisX.ScaleView.ZoomReset();
                    }
                    else
                    {
                        leftAxisX.ScaleView.Zoom(leftZoom.Start, leftZoom.End);
                        rightAxisX.ScaleView.Zoom(rightZoom.Start, rightZoom.End);
                    }
                }
            }
            else if (delta > 0)
            {
                var leftMin = leftAxisX.ScaleView.ViewMinimum;
                var leftMax = leftAxisX.ScaleView.ViewMaximum;
                var rightMin = rightAxisX.ScaleView.ViewMinimum;
                var rightMax = rightAxisX.ScaleView.ViewMaximum;

                leftZooms.Push(new Zooms { Start = leftMin, End = leftMax });
                rightZooms.Push(new Zooms { Start = rightMin, End = rightMax });

                leftAxisX.ScaleView.Zoom(leftAxisX.PixelPositionToValue(position) - (leftMax - leftMin) / 4, leftAxisX.PixelPositionToValue(position) + (leftMax - leftMin) / 4);
                rightAxisX.ScaleView.Zoom(rightAxisX.PixelPositionToValue(position) - (rightMax - rightMin) / 4, rightAxisX.PixelPositionToValue(position) + (rightMax - rightMin) / 4);
            }
            leftAxisX.ScaleView.Zoomable = false;
            rightAxisX.ScaleView.Zoomable = false;
        }

        private void SetIconImages()
        {
            OpenIcon.Source = AppImage.OpenIcon;
            SaveIcon.Source = AppImage.SaveIcon;
            PlayPauseIcon.Source = AppImage.PlayIcon;
            StopIcon.Source = AppImage.StopIcon;
            RecordIcon.Source = AppImage.RecordIcon;
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
            //SetSaveBuffer(Pdata);
            //SetDWDataLength((uint)wave.GetDataLength());

            Trace.WriteLine("Done!");

            waveDrawer.SetMinMaxSample(wave.GetChannels()[0]);

            WaveScroller.Maximum = wave.Subchunk2Size / 2 / wave.NumChannels - 1000;

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
                PlayPauseIcon.Source = AppImage.PauseIcon;
                //BeginPlay();
            }
            else
            {
                PlayPauseIcon.Source = AppImage.PlayIcon;
                //PausePlay();
            }

            bPlaying = !bPlaying;
        }

        /**
         * Stops Playing Wave
         */
        public void StopHandler(object sender, RoutedEventArgs e)
        {
            //EndRecord();
            isRecording = false;
        }

        /**
         * Start Recording
         */
        public void RecordHandler(object sender, RoutedEventArgs e)
        {
            //BeginRecord();
            isRecording = true;
        }

        private void WaveScrollHandler(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ClearCanvases();
            RedrawWaves();
        }

        private void ClearCanvases()
        {
            leftChart.Series[0].Points.Clear();
            rightChart.Series[0].Points.Clear();
        }

        private void RedrawWaves()
        {
            if (wave != null)
            {
                waveDrawer.DrawWave(wave.GetChannels()[0], ref leftChart, (int)WaveScroller.Value, leftChart.Width);
                if (!wave.IsMono())
                {
                    waveDrawer.DrawWave(wave.GetChannels()[1], ref rightChart, (int)WaveScroller.Value, rightChart.Width);
                }
            }
        }

        private void CutDeleteHandler(object sender, RoutedEventArgs e)
        {
            var cursor = leftChart.ChartAreas[0].CursorX;
            short[][] temp = wave.ExtractSamples((int)(cursor.SelectionStart + WaveScroller.Value), (int)(cursor.SelectionEnd + WaveScroller.Value));

            if (e.Source.Equals(CutButton))
            {
                cutSamples = temp;
            }
            leftChart.ChartAreas[0].CursorX.Position = cursor.SelectionStart;
            rightChart.ChartAreas[0].CursorX.Position = cursor.SelectionStart;

            ClearCanvases();
            RedrawWaves();
        }

        private void PasteHandler(object sender, RoutedEventArgs e)
        {
            var cursor = leftChart.ChartAreas[0].CursorX;

            wave.InsertSamples(cutSamples, (int)(leftChart.ChartAreas[0].CursorX.SelectionEnd + WaveScroller.Value));


            ClearCanvases();
            RedrawWaves();
        }
    }
}
