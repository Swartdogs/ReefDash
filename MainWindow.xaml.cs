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

            _client.ServerResponseReceived += s =>
            {
                Dispatcher.Invoke(() => _textArea.Text += $"SERVER: {s}{Environment.NewLine}");
            };

            _client.ClientCommandTransmitted += s =>
            {
                Dispatcher.Invoke(() => _textArea.Text += $"CLIENT: {s}{Environment.NewLine}");
            };

            _client.ConnectionStatusChanged += connected =>
            {
                Dispatcher.Invoke(() => _statusLabel.Content = connected ? "Connected" : "Disconnected");
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

        private void _queryButton_Click(object sender, RoutedEventArgs e)
        {
            _client.SendQuery(ElementType.RobotValue, 4);
        }

        private void _getButton_Click(object sender, RoutedEventArgs e)
        {
            _client.SendGet(0, 1, 2, 3, 4);
        }

        private void _setButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _eventButton_Click(object sender, RoutedEventArgs e)
        {
            _client.SendEvent();
        }

        private void _buttonButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _pingButton_Click(object sender, RoutedEventArgs e)
        {

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