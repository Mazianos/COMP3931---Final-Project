using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace WaveAnalyzer
{
    public class Commands
    {
        public RoutedCommand Cut { get; private set; }
        public RoutedCommand Paste { get; private set; }
        public RoutedCommand Delete { get; private set; }

        public Commands()
        {
            Cut = new RoutedCommand();
            Paste = new RoutedCommand();
            Delete = new RoutedCommand();

            Cut.InputGestures.Add(new KeyGesture(Key.X, ModifierKeys.Control));
            Paste.InputGestures.Add(new KeyGesture(Key.V, ModifierKeys.Control));
            Delete.InputGestures.Add(new KeyGesture(Key.Delete));
        }
    }
}
