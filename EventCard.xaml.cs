using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReefDash;

/// <summary>
/// Interaction logic for EventCard.xaml
/// </summary>
public partial class EventCard : UserControl
{
    public static readonly DependencyProperty BorderColorProperty =
        DependencyProperty.Register("BorderColor", typeof(SolidColorBrush), typeof(EventCard), new PropertyMetadata(Brushes.Green));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register("Message", typeof(string), typeof(EventCard), new PropertyMetadata(string.Empty));

    public SolidColorBrush BorderColor
    {
        get => (SolidColorBrush)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public EventCard()
    {
        InitializeComponent();
    }
}
