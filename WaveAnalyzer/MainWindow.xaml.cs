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
using System.IO;

namespace WaveAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string filepath = "wave.txt";
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

        public async void fourierHandler(object sender, RoutedEventArgs e)
        {
            //File.Create("wave.txt");
            if (isConverted)
            {
                samples = Fourier.inverseDFT(values);

                textBlock.Text = "";
                File.WriteAllText(filepath, String.Empty);
                
                for (int i = 0; i < samples.Length; ++i)
                {
                    string text = Math.Round(samples[i], 3) + ", ";
                    textBlock.Text += text;
                    File.AppendAllText(filepath, text);
                }
            }
            else
            {
                values = Fourier.DFT(samples);

                textBlock.Text = "";
                File.WriteAllText(filepath, String.Empty);

                for (int i = 0; i < values.Length; ++i)
                {
                    string text = "(" + Math.Round(values[i].real, 3) + ',' + Math.Round(values[i].imag, 3) + "), ";
                    textBlock.Text += text;
                    File.AppendAllText(filepath, text);
                }
            }

            isConverted = !isConverted;
        }

        private void fileSaveHandler(object sender, RoutedEventArgs e)
        {

        }
    }
}
