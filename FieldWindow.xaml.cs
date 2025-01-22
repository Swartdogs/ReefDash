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
using System.Windows.Shapes;

namespace ReefDash
{
    /// <summary>
    /// Interaction logic for FieldWindow.xaml
    /// </summary>
    public partial class FieldWindow : Window
    {
        private double _scale;

        public FieldWindow()
        {
            InitializeComponent();
            FieldImage.Source = new BitmapImage(new Uri("C:\\Users\\crm11\\source\\repos\\ReefDash\\image.png"));
        }

        private void FieldImage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateScaling();
            DrawCircleAtCoordinate(0, 0);
        }

        private void UpdateScaling()
        {
            double imageWidth = FieldImage.Source.Width;
            double imageHeight = FieldImage.Source.Height;

            double windowWidth = ActualWidth;
            double windowHeight = ActualHeight;

            double scaleX = windowWidth / imageWidth;
            double scaleY = windowHeight / imageHeight;

            _scale = scaleX < scaleY ? scaleX : scaleY;

            FieldImage.Width = imageWidth * scaleX;
            FieldImage.Height = imageHeight * scaleY;
        }

        private Point FieldToPixel(double x, double y)
        {
            double centerX = ActualWidth / 2;
            double centerY = ActualHeight / 2;

            double pixelX = (x + centerX) * _scale;
            double pixelY = (centerY - y) * _scale;

            return new Point(pixelX, pixelY);
        }

        private void DrawCircleAtCoordinate(double x, double y)
        {
            Point point = FieldToPixel(x, y);

            Ellipse circle = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = Brushes.Red
            };

            Canvas.SetLeft(circle, point.X - (circle.Width / 2));
            Canvas.SetTop(circle, point.Y - (circle.Height / 2));

            DrawingCanvas.Children.Add(circle);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawingCanvas.Children.Clear();
            UpdateScaling();
            DrawCircleAtCoordinate(0, 0);
        }
    }
}
