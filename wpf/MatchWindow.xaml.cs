using System.Windows;

namespace OneBullet;

public partial class MatchWindow : Window
{
    public MatchWindow(ApiClient api, ImageCache images, string matchId, string? subject)
    {
        DataContext = new MatchViewModel(api, images, matchId, subject);
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
