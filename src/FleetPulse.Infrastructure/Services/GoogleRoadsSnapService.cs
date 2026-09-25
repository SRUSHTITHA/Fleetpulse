using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using FleetPulse.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetPulse.Infrastructure.Services;

/// <summary>
/// Snaps GPS fixes onto the nearest road via Google's Roads API (nearestRoads).
/// Results are cached per ~11 m grid cell so a stationary or slowly-moving vehicle
/// doesn't produce a paid Roads API call for every evaluation cycle.
/// </summary>
public class GoogleRoadsSnapService : IRoadSnapService
{
    private readonly HttpClient _http;
    private readonly GoogleMapsOptions _options;
    private readonly ILogger<GoogleRoadsSnapService> _logger;

    private static readonly ConcurrentDictionary<string, CacheEntry> SnapCache = new();

    public GoogleRoadsSnapService(
        HttpClient http,
        IOptions<GoogleMapsOptions> options,
        ILogger<GoogleRoadsSnapService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SnappedPoint?> TrySnapAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return null;

        var key = KeyFor(latitude, longitude);
        if (SnapCache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.Point;

        SnappedPoint? point = null;
        try
        {
            var url = $"{(string.IsNullOrWhiteSpace(_options.BaseUrl) ? "https://roads.googleapis.com/v1" : _options.BaseUrl.TrimEnd('/'))}" +
                      $"/nearestRoads?points={latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}" +
                      $"&key={Uri.EscapeDataString(_options.ApiKey)}";

            using var response = await _http.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                point = ParseSnappedPoint(stream);
                if (point is null)
                    _logger.LogDebug("Google Roads API returned no road for {Latitude},{Longitude}.", latitude, longitude);
            }
            else
            {
                _logger.LogWarning("Google Roads API returned {StatusCode} {Reason}.", (int)response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (Exception ex)
        {
            // Never let a Google outage break trip evaluation — the caller falls back to the raw fix.
            _logger.LogWarning(ex, "Google Roads API call failed for {Latitude},{Longitude}; falling back to the unsnapped fix.", latitude, longitude);
        }

        if (SnapCache.Count >= Math.Max(1, _options.CacheMaxEntries))
            SnapCache.Clear();

        SnapCache[key] = new CacheEntry(point, DateTime.UtcNow.AddSeconds(Math.Max(1, _options.CacheTtlSeconds)));
        return point;
    }

    private static SnappedPoint? ParseSnappedPoint(Stream json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("snappedPoints", out var snapped) || snapped.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var item in snapped.EnumerateArray())
        {
            if (!item.TryGetProperty("location", out var location) || location.ValueKind != JsonValueKind.Object)
                continue;

            var hasLat = location.TryGetProperty("latitude", out var latElement);
            var hasLng = location.TryGetProperty("longitude", out var lngElement);
            if (hasLat && hasLng && latElement.ValueKind == JsonValueKind.Number && lngElement.ValueKind == JsonValueKind.Number)
                return new SnappedPoint(latElement.GetDecimal(), lngElement.GetDecimal());
        }

        return null;
    }

    private static string KeyFor(decimal latitude, decimal longitude)
        => string.Create(CultureInfo.InvariantCulture, $"{Math.Round(latitude, 4)}|{Math.Round(longitude, 4)}");

    private sealed record CacheEntry(SnappedPoint? Point, DateTime ExpiresAt);
}