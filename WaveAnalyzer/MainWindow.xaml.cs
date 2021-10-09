﻿using System;
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



namespace WaveAnalyzer
{
    public partial class MainWindow : Window
    {
        const string filepath = "SoundFile1.txt";
        double[] samples = { 1, 2, 3, 4, 5 };
        Complex[] values;
        bool isConverted, bRecording, bPlaying;

        [System.ComponentModel.Browsable(false)]
        public IntPtr Handle { get; }

        [DllImport("RecordPlayLibrary.dll")]
        static extern bool WinProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int FindWindow(string lpClassName, String lpWindowName);


        public MainWindow()
        {
            InitializeComponent();
            //0x0110 is the code for WM_INITDIALOG
            //SendMessage(Handle, 0x0110, (IntPtr)null, (IntPtr)null);
        }

        public void FileOpenHandler(object sender, RoutedEventArgs e)
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
            byte[] waveBytes = File.ReadAllBytes(openFileDialog.FileName);

            // Get the header information.
            WaveHeader waveHeader = new WaveHeader();
            waveHeader.chunkID = ByteConverter.ToInt32BigEndian(waveBytes, 0);
            waveHeader.chunkSize = ByteConverter.ToInt32(waveBytes, 4);
            waveHeader.format = ByteConverter.ToInt32BigEndian(waveBytes, 8);
            waveHeader.subchunk1ID = ByteConverter.ToInt32BigEndian(waveBytes, 12);
            waveHeader.subchunk1Size = ByteConverter.ToInt32(waveBytes, 16);
            waveHeader.audioFormat = ByteConverter.ToInt16(waveBytes, 20);
            waveHeader.numChannels = ByteConverter.ToInt16(waveBytes, 22);
            waveHeader.sampleRate = ByteConverter.ToInt32(waveBytes, 24);
            waveHeader.byteRate = ByteConverter.ToInt32(waveBytes, 28);
            waveHeader.blockAlign = ByteConverter.ToInt16(waveBytes, 32);
            waveHeader.bitsPerSample = ByteConverter.ToInt16(waveBytes, 34);
            waveHeader.subchunk2ID = ByteConverter.ToInt32BigEndian(waveBytes, 36);
            waveHeader.subchunk2Size = ByteConverter.ToInt32(waveBytes, 40);

            List<short> leftChannel = new List<short>();
            // rightChannel (stereo) will only be needed if the number of channels is not 1 (mono).
            List<short> rightChannel = waveHeader.numChannels != 1 ? new List<short>() : null;

            // Skip 44 bytes to get to the sound data.
            int byteIndex = 44;
            int samples = waveHeader.subchunk2Size;
            
            // Iterate through the samples and push the float values to their respective channel lists.
            // For mono, samples are two bytes each. For stereo, it is four bytes, first two left, then two right.
            while (byteIndex < samples)
            {
                leftChannel.Add(ByteConverter.ToInt16(waveBytes, byteIndex));
                byteIndex += 2;

                if (rightChannel != null)
                {
                    rightChannel.Add(ByteConverter.ToInt16(waveBytes, byteIndex));
                    byteIndex += 2;
                }
            }

            Trace.WriteLine("Done!");

            // Drawing.
            SolidColorBrush redBrush = new SolidColorBrush
            {
                Color = Colors.Red
            };
            Polyline wavePolyline = new Polyline
            {
                Stroke = redBrush,
                StrokeThickness = 1
            };
            PointCollection pointCollection = new PointCollection();

            int min = leftChannel.Min();
            float denom = leftChannel.Max() - min;
            for (int i = 0; i < leftChannel.Count(); i += 1)
            {
                Point point = new Point()
                {
                    X = i,
                    Y = (leftChannel[i] - min) / denom * 100
                };
                pointCollection.Add(point);
            }

            wavePolyline.Points = pointCollection;
            
            waveCanvas.Children.Add(wavePolyline);

            waveCanvas.Width = leftChannel.Count();
            //waveCanvas.Margin = new System.Windows.Thickness(0, leftChannel.Max(), 0, 0);


            /*TextReader fileReader;

            
            if (openFileDialog.ShowDialog() == true)
            {
                // Filepath is stored in .FileName.
                fileReader = File.OpenText(openFileDialog.FileName);
                isConverted = false;

                // Parse sample values line by line.

                string currentSample;
                List<double> sampleList = new List<double>();
                while ((currentSample = fileReader.ReadLine()) != null)
                {
                    sampleList.Add(double.Parse(currentSample));
                }

                samples = sampleList.ToArray();
                values = Fourier.DFT(samples, 8);
                Fourier.divideByN(values, values.Length);

                double[] amplitudes = Fourier.getAmplitudes(values);
                Fourier.printDoubles(amplitudes);

                fileReader.Close();

                fourierButton.IsEnabled = true;
            }

            */


        }

        private void FileSaveHandler(object sender, RoutedEventArgs e)
        {
            if (isConverted)
            {
                // Convert values back to samples.
                samples = Fourier.InverseDFT(values, values.Length);
                
                isConverted = false;
            }

            // Write sample values to a text file.
            File.WriteAllText(filepath, string.Empty);

            for (int i = 0; i < samples.Length; ++i)
            {
                File.AppendAllText(filepath, Math.Round(samples[i], 3) + "\n");
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
                SendMessage(Handle, 0x0111, (IntPtr)((ushort)(((ulong)(1000)) & 0xffff)), (IntPtr)null);
                bRecording = true;
            } else
            {
                //0x0111 is the code for WM_COMMAND
                //1001 is the code for IDC_RECORD_END
                SendMessage(Handle, 0x0111, (IntPtr)((ushort)(((ulong)(1001)) & 0xffff)), (IntPtr)null);
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
            SendMessage(Handle, 0x0111, (IntPtr)((ushort)(((ulong)(1001)) & 0xffff)), (IntPtr)null);
        }

        /**
         * Play/Pause Wave
         */
        public void PlayPauseHandler(object sender, RoutedEventArgs e)
        {
            if (!bPlaying)
            {

                //0x0111 is the code for WM_COMMAND
                //1002 is the code for IDC_PLAY_BEG
                SendMessage(Handle, 0x0111, (IntPtr)((ushort)(((ulong)(1002)) & 0xffff)), (IntPtr)null);
                bPlaying = true;
            }
            else
            {

                //0x0111 is the code for WM_COMMAND
                //1003 is the code for IDC_PLAY_PAUSE
                SendMessage(Handle, 0x0111, (IntPtr)((ushort)(((ulong)(1003)) & 0xffff)), (IntPtr)null);
            }
        }

        /**
         * Converts between samples and frequency bins.
         */
        public void FourierHandler(object sender, RoutedEventArgs e)
        {
            if (isConverted)
            {
                samples = Fourier.InverseDFT(values, values.Length);

                textBlock.Text = "";

                for (int i = 0; i < samples.Length; ++i)
                {
                    textBlock.Text += Math.Round(samples[i], 3) + ", ";
                }
            }
            else
            {
                values = Fourier.DFT(samples, samples.Length);
                Fourier.DivideByN(values, values.Length);

                textBlock.Text = "";

                for (int i = 0; i < values.Length; ++i)
                {
                    textBlock.Text += "(" + Math.Round(values[i].real, 3) + ',' + Math.Round(values[i].imag, 3) + "), ";
                }
            }

            isConverted = !isConverted;
        }
    }
}
