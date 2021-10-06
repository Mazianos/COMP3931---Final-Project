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

namespace WaveAnalyzer
{
    public partial class MainWindow : Window
    {
        private Wave wave;
        private WaveDrawer waveDrawer;
        ImageSourceConverter converter;
        private bool isPlaying;

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

        private void SaveHandler(object sender, RoutedEventArgs e)
        {

        }

        private void PlayPauseHandler(object sender, RoutedEventArgs e)
        {
            PlayPauseIcon.Source = isPlaying ? (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\play.png"))
                : (ImageSource)converter.ConvertFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\images\pause.png"));
            isPlaying = !isPlaying;
        }

        private void StopHandler(object sender, RoutedEventArgs e)
        {

        }

        private void RecordHandler(object sender, RoutedEventArgs e)
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
         * Converts between samples and frequency bins.
         */
        /*public void FourierHandler(object sender, RoutedEventArgs e)
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
        }*/
    }
}
