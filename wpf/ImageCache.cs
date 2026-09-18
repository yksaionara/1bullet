using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OneBullet;

// Downloads static Riot CDN art once, keeps it under %LOCALAPPDATA%\1Bullet,
// and serves native WPF ImageSources. Never re-downloads a known URL and
// never blocks the UI thread on network I/O.
public sealed class ImageCache
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly ConcurrentDictionary<string, ImageSource?> _memory = new();
    private readonly string _dir;

    public ImageCache()
    {
        var local = Environment.GetEnvironmentVariable("LOCALAPPDATA")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "AppData", "Local");
        _dir = Path.Combine(local, "1Bullet", "wpf-image-cache");
        Directory.CreateDirectory(_dir);
    }

    public async Task<ImageSource?> GetAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (_memory.TryGetValue(url, out var cached)) return cached;

        try
        {
            var path = PathFor(url);
            byte[] bytes;
            if (File.Exists(path))
            {
                bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            }
            else
            {
                bytes = await Http.GetByteArrayAsync(url).ConfigureAwait(false);
                if (bytes.Length == 0 || bytes.Length > 8_000_000) return null;
                var withExt = Path.ChangeExtension(path, DetectExtension(bytes));
                await File.WriteAllBytesAsync(withExt, bytes).ConfigureAwait(false);
                path = withExt;
                TrimCache();
            }
            var image = Decode(bytes);
            _memory[url] = image;
            return image;
        }
        catch
        {
            _memory[url] = null;
            return null;
        }
    }

    private string PathFor(string url)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
        var ext = Path.GetExtension(new string(url.TakeWhile(c => c is not ('?' or '#')).ToArray()));
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp")) ext = ".img";
        return Path.Combine(_dir, hash + ext.ToLowerInvariant());
    }

    private static string DetectExtension(byte[] bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50) return ".png";
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) return ".jpg";
        if (bytes.Length >= 12 && bytes[0] == 0x52 && bytes[8] == 0x57) return ".webp";
        return ".img";
    }

    // Decodes raw image bytes for immediate UI use. BitmapImage uses native
    // codecs, so construction is marshalled to the UI thread; the frozen
    // result is then safe to use from any thread.
    public static ImageSource? Decode(byte[] bytes)
    {
        try
        {
            var app = System.Windows.Application.Current;
            if (app is not null && !app.Dispatcher.CheckAccess())
                return app.Dispatcher.Invoke(() => Load(bytes));
            return Load(bytes);
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource? Load(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private void TrimCache()
    {
        try
        {
            var files = new DirectoryInfo(_dir).GetFiles()
                .OrderBy(f => f.LastWriteTimeUtc).ToList();
            foreach (var file in files.Take(Math.Max(0, files.Count - 1000)))
            {
                try { file.Delete(); } catch { }
            }
        }
        catch { }
    }
}
