using System.Windows;

namespace ReefDash;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    [STAThread]
    public static void Main()
    {
        App app = new App();
        app.Run(new MainWindow());
    }
}
