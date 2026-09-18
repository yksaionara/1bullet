using System.Threading;
using System.Windows;

namespace OneBullet;

public partial class App : Application
{
    private Mutex? _instanceMutex;
    private MainViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var created = false;
        _instanceMutex = new Mutex(false, @"Local\1BulletApp", out created);
        if (!created)
        {
            MessageBox.Show("1 Bullet is already running.", "1 Bullet",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        var api = new ApiClient();
        var backend = new BackendManager(api);
        var images = new ImageCache();
        _viewModel = new MainViewModel(api, backend, images);

        var window = new MainWindow(_viewModel);
        MainWindow = window;
        window.Show();
        _ = _viewModel.StartAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_viewModel is not null)
                await _viewModel.ShutdownAsync().ConfigureAwait(false);
        }
        catch { }
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
