using System.Windows;

namespace OneBullet;

public partial class ProfileWindow : Window
{
    public ProfileWindow(ApiClient api, ImageCache images, string puuid, string displayName,
        string? rank = null, string? rankIcon = null, string? peakRank = null, string? peakIcon = null)
    {
        DataContext = new ProfileViewModel(api, images, puuid, displayName,
            rank, rankIcon, peakRank, peakIcon);
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
