using System.Windows.Input;

namespace WaveAnalyzer
{
    /// <summary>
    /// Enables hotkeys to be used to cut, copy, paste, and delete from the time domain chart
    /// </summary>
    public class Commands
    {
        public RoutedCommand Cut { get; private set; }
        public RoutedCommand Paste { get; private set; }
        public RoutedCommand Delete { get; private set; }
        public RoutedCommand Copy { get; private set; }

        public Commands()
        {
            Cut = new RoutedCommand();
            Paste = new RoutedCommand();
            Delete = new RoutedCommand();
            Copy = new RoutedCommand();

            Cut.InputGestures.Add(new KeyGesture(Key.X, ModifierKeys.Control));
            Paste.InputGestures.Add(new KeyGesture(Key.V, ModifierKeys.Control));
            Delete.InputGestures.Add(new KeyGesture(Key.Delete));
            Copy.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control));
        }
    }
}
