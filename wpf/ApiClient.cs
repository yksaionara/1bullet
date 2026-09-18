using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OneBullet;

// Typed client for the local 1 Bullet Python backend. Talks to 127.0.0.1
// only; the backend never listens on any other interface.
public sealed class ApiClient
{
    public const string ReleaseApi =
        "https://api.github.com/repos/yksaionara/1bullet/releases/latest";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private string _baseUrl = "http://127.0.0.1:5000";

    public void SetBaseUrl(string baseUrl) => _baseUrl = baseUrl.TrimEnd('/');

    public async Task<HealthResponse?> GetHealthAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"{_baseUrl}/api/health", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<HealthResponse>(Json, ct).ConfigureAwait(false);
    }

    public async Task<BoardDto?> GetBoardAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"{_baseUrl}/api/live", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<BoardDto>(Json, ct).ConfigureAwait(false);
    }

    public async Task<SettingsDto?> GetSettingsAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"{_baseUrl}/api/settings", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SettingsDto>(Json, ct).ConfigureAwait(false);
    }

    public async Task<(bool ok, string message)> SaveSettingsAsync(
        Dictionary<string, object?> values, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"{_baseUrl}/api/settings", values, ct).ConfigureAwait(false);
        var body = await response.Content
            .ReadFromJsonAsync<SettingsSaveResponse>(Json, ct).ConfigureAwait(false);
        if (body is null) return (false, "No response from the backend.");
        return (body.Ok, body.Message ?? "");
    }

    public async Task<byte[]?> GetBackgroundAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(
            $"{_baseUrl}/api/settings/background", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> UploadBackgroundAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "image", Path.GetFileName(filePath));
        using var response = await _http.PostAsync(
            $"{_baseUrl}/api/settings/background", content, ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task RemoveBackgroundAsync(CancellationToken ct = default)
    {
        using var _ = await _http.DeleteAsync(
            $"{_baseUrl}/api/settings/background", ct).ConfigureAwait(false);
    }

    public async Task RequestShutdownAsync(CancellationToken ct = default)
    {
        try
        {
            using var _ = await _http.PostAsync(
                $"{_baseUrl}/api/shutdown", content: null, ct).ConfigureAwait(false);
        }
        catch
        {
            // Expected: the backend exits as it handles this request.
        }
    }
}

public static class UpdateChecker
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

    static UpdateChecker()
    {
        Http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("1bullet", "1.0"));
    }

    public sealed record UpdateInfo(string Version, string DownloadUrl);

    // Returns update info when the newest 1bullet release is newer than the
    // running app, else null. Any failure means "no update" — update checks
    // must never break the tracker.
    public static async Task<UpdateInfo?> CheckAsync(string currentVersion)
    {
        try
        {
            var release = await Http.GetFromJsonAsync<ReleaseDto>(ApiClient.ReleaseApi)
                .ConfigureAwait(false);
            var latest = (release?.TagName ?? "").Trim();
            if (latest == "" || Versions.Compare(latest, currentVersion) <= 0) return null;
            var want = Versions.NormalizeAssetName("1 Bullet Setup.exe");
            var asset = release!.Assets
                .FirstOrDefault(a => Versions.NormalizeAssetName(a.Name) == want);
            var url = asset?.BrowserDownloadUrl ?? release.HtmlUrl;
            if (string.IsNullOrWhiteSpace(url)) return null;
            return new UpdateInfo(latest.TrimStart('v', 'V'), url);
        }
        catch
        {
            return null;
        }
    }
}
