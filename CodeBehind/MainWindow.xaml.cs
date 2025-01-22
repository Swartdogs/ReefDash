using System.Windows;

namespace ReefDash;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ElevatorWindow? _elevatorWindow = null;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void ToggleElevatorWindow(object sender, RoutedEventArgs e)
    {
        if (_elevatorWindow is null)
        {
            _elevatorWindow = new ElevatorWindow(this);
            _elevatorWindow.Closed += (s, e) =>
            {
                _elevatorWindow = null;
            };
            _elevatorWindow.Show();
        }
        else
        {
            _elevatorWindow.Close();
            _elevatorWindow = null;
        }
    }
}
