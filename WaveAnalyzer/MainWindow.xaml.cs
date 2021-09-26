using System;
using System.Collections.Generic;
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

namespace WaveAnalyzer
{
    public partial class MainWindow : Window
    {
        const string filepath = "SoundFile1.txt";
        double[] samples;
        complex[] values;
        bool isConverted;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void fileOpenHandler(object sender, RoutedEventArgs e)
        {
            isConverted = false;

            // Parse sample values line by line.
            TextReader fileReader = File.OpenText(filepath);
            string currentSample;
            List<double> sampleList = new List<double>();
            while ((currentSample = fileReader.ReadLine()) != null)
            {
                sampleList.Add(double.Parse(currentSample));
            }

            samples = sampleList.ToArray();
            values = new complex[samples.Length];
            textBlock.Text = "";

            for (int i = 0; i < samples.Length; ++i)
            {
                textBlock.Text += samples[i] + ", ";
            }

            fileReader.Close();

            fourierButton.IsEnabled = true;
        }

        private void fileSaveHandler(object sender, RoutedEventArgs e)
        {
            if (isConverted)
            {
                // Convert values back to samples.
                samples = Fourier.inverseDFT(values);
                
                isConverted = false;
            }

            // Write sample values to a text file.
            File.WriteAllText(filepath, String.Empty);

            for (int i = 0; i < samples.Length; ++i)
            {
                File.AppendAllText(filepath, Math.Round(samples[i], 3) + "\n");
            }
        }

        /**
         * Converts between samples and frequency bins.
         */
        public void fourierHandler(object sender, RoutedEventArgs e)
        {
            if (isConverted)
            {
                samples = Fourier.inverseDFT(values);

                textBlock.Text = "";

                for (int i = 0; i < samples.Length; ++i)
                {
                    textBlock.Text += Math.Round(samples[i], 3) + ", ";
                }
            }
            else
            {
                values = Fourier.DFT(samples);

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
