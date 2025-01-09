using System.Windows;
using System.Windows.Controls;

namespace ReefDash
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DashClient _client;
        private bool _autoScroll = true;

        public MainWindow()
        {
            InitializeComponent();

            _client = new DashClient(_ipTextBox.Text, int.Parse(_portTextBox.Text));

            _client.LogMessage += s =>
            {
                Dispatcher.Invoke(() => _textArea.Text += s + Environment.NewLine);
            };

            _client.ConnectionStatusChanged += connected =>
            {
                Dispatcher.Invoke(() => _statusLabel.Content = connected ? "Connected" : "Disconnected");
            };

            _client.EventReceived += (e, m) =>
            {
                Console.WriteLine($"[{e}] {m}");
            };
        }

        private void _updateButton_Click(object sender, RoutedEventArgs e)
        {
            _client.SetServerAddress(_ipTextBox.Text, int.Parse(_portTextBox.Text));
        }

        private void _connectButton_Click(object sender, RoutedEventArgs e)
        {
            _client.Start();
        }

        private void _disconnectButton_Click(object sender, RoutedEventArgs e)
        {
            _client.Stop();
        }

        private void _startDataTransmissionButton_Click(object sender, RoutedEventArgs e)
        {
            _client.EnableDataTransmission(true);
            _client.EnableEventTransmission(true);
        }

        private void _stopDataTransmissionButton_Click(object sender, RoutedEventArgs e)
        {
            _client.EnableDataTransmission(false);
            _client.EnableEventTransmission(false);
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange == 0)
            {
                _autoScroll = _textAreaScrollViewer.VerticalOffset == _textAreaScrollViewer.ScrollableHeight;
            }
            else if (_autoScroll)
            {
                _textAreaScrollViewer.ScrollToBottom();
            }
        }
    }
}