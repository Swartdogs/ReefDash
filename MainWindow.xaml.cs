using System.Windows;
using System.Windows.Controls;

namespace ReefDash
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FieldWindow? fieldWindow = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ToggleFieldView(object sender, RoutedEventArgs e)
        {
            if (fieldWindow == null)
            {
                fieldWindow = new FieldWindow();
                fieldWindow.Show();
            }
            else
            {
                fieldWindow.Close();
                fieldWindow = null;
            }
        }
    }
}