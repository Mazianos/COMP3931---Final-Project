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
        private WaveSelector currentSelection;
        private short[][] cutSamples;

        private Color waveColor = new Color()
        {
            R = 248,
            G = 175,
            B = 96,
            A = 255
        };
        private Color selectionColor = new Color()
        {
            R = 96,
            G = 175,
            B = 248,
            A = 255
        };
        private Color selectorColor = new Color()
        {
            R = 255,
            G = 100,
            B = 100,
            A = 255
        };

        public MainWindow()
        {
            InitializeComponent();

            waveDrawer = new WaveDrawer(waveColor);
            cutSamples = null;

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
            ClearCanvases();
            RedrawWaves();
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

        private void WaveScrollHandler(object sender, ScrollChangedEventArgs e)
        {
            ClearCanvases();
            RedrawWaves();
        }

        private void ClearCanvases()
        {
            LeftChannelCanvas.Children.Clear();
            RightChannelCanvas.Children.Clear();
        }

        private void RedrawWaves()
        {
            if (wave != null)
            {
                waveDrawer.DrawWave(wave.GetChannels()[0], ref LeftChannelCanvas, WaveScroll.HorizontalOffset, SystemParameters.PrimaryScreenWidth);
                if (!wave.IsMono())
                {
                    waveDrawer.DrawWave(wave.GetChannels()[1], ref RightChannelCanvas, WaveScroll.HorizontalOffset, SystemParameters.PrimaryScreenWidth);
                }
            }
        }

        private void WaveMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            // Select a portion of the wave from the current mouse position.
            currentSelection = new WaveSelector((int)(e.GetPosition(WaveScroll).X + WaveScroll.HorizontalOffset), selectionColor, selectorColor);
            UpdateSelection(currentSelection.StartX);
        }

        private void WaveMouseMoveHandler(object sender, MouseEventArgs e)
        {
            // Update the selection if the mouse is held down.
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSelection((int)(e.GetPosition(WaveScroll).X + WaveScroll.HorizontalOffset));
            }
        }

        private void UpdateSelection(int xPosition)
        {
            // Return if no wave is found or if there is no current selection.
            if (wave == null || currentSelection == null)
            {
                return;
            }

            ClearCanvases();

            // Update the selection by giving it the current x position of the mouse and the relevant canvases.
            currentSelection.UpdateSelection(xPosition, ref LeftChannelCanvas);
            if (!wave.IsMono())
            {
                currentSelection.UpdateSelection(xPosition, ref RightChannelCanvas);
            }

            // Redraw the waves on top of the created selection rectangles.
            RedrawWaves();

            currentSelection.DrawSelector(ref LeftChannelCanvas);
            if (!wave.IsMono())
            {
                currentSelection.DrawSelector(ref RightChannelCanvas);
            }
        }

        private void CutDeleteHandler(object sender, RoutedEventArgs e)
        {
            short[][] temp = wave.ExtractSamples(currentSelection.StartX, currentSelection.CurrentX);
            
            if (e.Source.Equals(CutButton))
            {
                cutSamples = temp;
            }

            int previousCurrentX = currentSelection.CurrentX;
            currentSelection = new WaveSelector(previousCurrentX, selectionColor, selectorColor);
            UpdateSelection(currentSelection.StartX);
            // WHY AREN'T YOU DRAWING THE DAMN LINE??
            /*foreach(var x in LeftChannelCanvas.Children)
            {
                Trace.WriteLine(x);
            }*/
        }

        private void PasteHandler(object sender, RoutedEventArgs e)
        {
            wave.InsertSamples(cutSamples, currentSelection.CurrentX);

            ClearCanvases();
            RedrawWaves();
        }
    }
}
