using System.Windows;

namespace OneBullet;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _closing;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private async void StartupCheck_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveStartupAsync(_viewModel.StartWithWindows).ConfigureAwait(true);
    }

    protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_closing)
        {
            base.OnClosing(e);
            return;
        }
        // Closing the window quits the whole app (backend included),
        // so users never end up with an invisible running instance.
        e.Cancel = true;
        _closing = true;
        try
        {
            await _viewModel.ShutdownAsync().ConfigureAwait(true);
        }
        catch { }
        Application.Current.Shutdown();
    }
}
