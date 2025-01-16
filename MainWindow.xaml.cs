﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ReefDash;

public partial class MainWindow : Window
{
    private bool isConnected;
    private int _seconds = 135;

    public MainWindow()
    {
        InitializeComponent();

        isConnected = false;

        var timer = new System.Windows.Threading.DispatcherTimer();

        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += (s, e) =>
        {
            _seconds--;

            if (_seconds < 0)
            {
                _seconds = 135;
            }

            UpdateProgressBar(_seconds, 135);
        };
        timer.Start();
    }

    private void AddInfoButton_Click(object sender, RoutedEventArgs e)
    {
        AddEventCard("This is an info message!", "info");
    }

    private void AddErrorButton_Click(object sender, RoutedEventArgs e)
    {
        AddEventCard("This is an error message!", "error");
    }

    private void UpdateProgressBar(int remainingTime, int totalTime)
    {
        if (totalTime > 0)
        {
            double percent = (double)remainingTime / totalTime * 100;
            ProgressBar.Value = percent;

            ProgressBar.Foreground = new SolidColorBrush(GetInterpolatedColor(percent));
        }
    }

    private Color GetInterpolatedColor(double percent)
    {
        percent = Math.Max(0, Math.Min(100, percent));

        byte red = 0;
        byte green = 0;

        if (percent > 50)
        {
            green = 255;
            red = (byte)(510 - ((51 * percent) / 10));
        }
        else
        {
            red = 255;
            green = (byte)((51 * percent) / 10);
        }

        return Color.FromRgb(red, green, 0);
    }

    private void AddEventCard(string eventMessage, string severity)
    {
        var eventCard = new EventCard
        {
            Message = eventMessage,
            BorderColor = severity == "error" ? Brushes.Red : Brushes.Green
        };

        EventCardsPanel.Children.Add(eventCard);

        if (severity != "error")
        {
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) =>
            {
                EventCardsPanel.Children.Remove(eventCard);
                timer.Stop();
            };
            timer.Start();
        }
    }

    private void UpdateRobotPosition(double x, double y)
    {
        Ellipse robotIndicator = new Ellipse
        {
            Width = 20,
            Height = 20,
            Fill = Brushes.Green,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        Canvas.SetLeft(robotIndicator, x);
        Canvas.SetTop(robotIndicator, y);
        FieldOverlay.Children.Add(robotIndicator);
    }
}
