using System.Windows;

namespace OneBullet;

public partial class ProfileWindow : Window
{
    public ProfileWindow(ApiClient api, ImageCache images, string puuid, string displayName)
    {
        DataContext = new ProfileViewModel(api, images, puuid, displayName);
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
