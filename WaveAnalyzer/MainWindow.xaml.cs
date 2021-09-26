using System;
using System.Collections.Generic;
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
using System.IO;

namespace WaveAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        double[] samples;
        complex[] values;
        bool isConverted;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void fileOpenHandler(object sender, RoutedEventArgs e)
        {
            string openPath = "SoundFile1.txt";

            TextReader fileReader = File.OpenText(openPath);
            string currentSample;
            List<double> sampleList = new List<double>();
            while ((currentSample = fileReader.ReadLine()) != null)
            {
                sampleList.Add(double.Parse(currentSample));
            }

            samples = sampleList.ToArray();
            values = new complex[samples.Length];

            for (int i = 0; i < samples.Length; ++i)
            {
                textBlock.Text += samples[i] + ", ";
            }
        }

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

        private void fileSaveHandler(object sender, RoutedEventArgs e)
        {

        }
    }
}
