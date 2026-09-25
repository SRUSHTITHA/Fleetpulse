using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FleetPulse.Application.DTOs.Geo;
using FleetPulse.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetPulse.Infrastructure.Services;

public class GeoService : IGeoService
{
    private readonly HttpClient _http;
    private readonly GoogleMapsOptions _maps;
    private readonly RoutingOptions _routing;
    private readonly ILogger<GeoService> _logger;

    public GeoService(
        HttpClient http,
        IOptions<GoogleMapsOptions> maps,
        IOptions<RoutingOptions> routing,
        ILogger<GeoService> logger)
    {
        _http = http;
        _maps = maps.Value;
        _routing = routing.Value;
        _logger = logger;
    }

    public async Task<GeocodeResultDto?> GeocodeAsync(string query, CancellationToken cancellationToken = default)
    {
        var trimmed = query.Trim();
        if (trimmed.Length == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(_maps.ApiKey))
        {
            var google = await GeocodeGoogleAsync(trimmed, cancellationToken);
            if (google is not null)
                return google;
        }

        var nominatim = await GeocodeNominatimAsync(trimmed, cancellationToken);
        return nominatim ?? await GeocodePhotonAsync(trimmed, cancellationToken);
    }

    public async Task<RouteResultDto> GetRouteAsync(
        double originLat,
        double originLng,
        double destLat,
        double destLng,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_maps.ApiKey))
        {
            var google = await RouteGoogleAsync(originLat, originLng, destLat, destLng, cancellationToken);
            if (google.Points.Count >= 2)
                return google;
        }

        // Use the self-hosted OSRM instance only when one is configured —
        // never a public OSRM server.
        if (!string.IsNullOrWhiteSpace(_routing.BaseUrl))
        {
            var osrm = await RouteOsrmAsync(originLat, originLng, destLat, destLng, cancellationToken);
            if (osrm.Points.Count >= 2)
                return osrm;
        }

        return new RouteResultDto
        {
            Points =
            {
                new RoutePointDto { Latitude = originLat, Longitude = originLng },
                new RoutePointDto { Latitude = destLat, Longitude = destLng }
            }
        };
    }

    private async Task<GeocodeResultDto?> GeocodeGoogleAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var url =
                "https://maps.googleapis.com/maps/api/geocode/json?address=" +
                Uri.EscapeDataString(query) +
                "&key=" + Uri.EscapeDataString(_maps.ApiKey!);

            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            var root = doc.RootElement;
            if (root.GetProperty("status").GetString() != "OK")
                return null;

            var first = root.GetProperty("results").EnumerateArray().FirstOrDefault();
            if (first.ValueKind != JsonValueKind.Object)
                return null;

            var location = first.GetProperty("geometry").GetProperty("location");
            return new GeocodeResultDto
            {
                Latitude = location.GetProperty("lat").GetDouble(),
                Longitude = location.GetProperty("lng").GetDouble(),
                DisplayName = first.GetProperty("formatted_address").GetString()
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google Geocoding failed for {Query}.", query);
            return null;
        }
    }

    private async Task<GeocodeResultDto?> GeocodeNominatimAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var url =
                "https://nominatim.openstreetmap.org/search?format=json&limit=1&q=" +
                Uri.EscapeDataString(query);

            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var results = await response.Content.ReadFromJsonAsync<List<NominatimHit>>(cancellationToken);
            var first = results?.FirstOrDefault();
            if (first is null ||
                !double.TryParse(first.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
                !double.TryParse(first.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                return null;

            return new GeocodeResultDto
            {
                Latitude = lat,
                Longitude = lng,
                DisplayName = first.DisplayName
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nominatim geocoding failed for {Query}.", query);
            return null;
        }
    }

    private async Task<GeocodeResultDto?> GeocodePhotonAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var url =
                "https://photon.komoot.io/api/?limit=1&q=" + Uri.EscapeDataString(query);

            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<PhotonResponse>(cancellationToken);
            var feature = result?.Features?.FirstOrDefault();
            var coordinates = feature?.Geometry?.Coordinates;
            if (coordinates is not { Length: >= 2 })
                return null;

            var properties = feature?.Properties;
            var displayName = string.Join(", ", new[]
            {
                properties?.Name,
                properties?.City,
                properties?.State,
                properties?.Country
            }.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());

            return new GeocodeResultDto
            {
                Latitude = coordinates[1],
                Longitude = coordinates[0],
                DisplayName = displayName
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Photon geocoding failed for {Query}.", query);
            return null;
        }
    }

    private async Task<RouteResultDto> RouteGoogleAsync(
        double originLat, double originLng, double destLat, double destLng, CancellationToken cancellationToken)
    {
        try
        {
            var origin = F(originLat) + "," + F(originLng);
            var dest = F(destLat) + "," + F(destLng);
            var url =
                "https://maps.googleapis.com/maps/api/directions/json?origin=" + origin +
                "&destination=" + dest +
                "&mode=driving&key=" + Uri.EscapeDataString(_maps.ApiKey!);

            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new RouteResultDto();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            if (doc.RootElement.GetProperty("status").GetString() != "OK")
                return new RouteResultDto();

            var polyline = doc.RootElement
                .GetProperty("routes")[0]
                .GetProperty("overview_polyline")
                .GetProperty("points")
                .GetString();

            if (string.IsNullOrEmpty(polyline))
                return new RouteResultDto();

            return new RouteResultDto
            {
                Points = DecodePolyline(polyline)
                    .Select(p => new RoutePointDto { Latitude = p.lat, Longitude = p.lng })
                    .ToList()
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google Directions failed.");
            return new RouteResultDto();
        }
    }

    private async Task<RouteResultDto> RouteOsrmAsync(
        double originLat, double originLng, double destLat, double destLng, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _routing.BaseUrl!.TrimEnd('/');
            var url =
                baseUrl + "/route/v1/driving/" +
                F(originLng) + "," + F(originLat) + ";" +
                F(destLng) + "," + F(destLat) +
                "?overview=full&geometries=geojson&steps=false";

            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new RouteResultDto();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            if (!doc.RootElement.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
                return new RouteResultDto();

            var coords = routes[0].GetProperty("geometry").GetProperty("coordinates");
            var points = new List<RoutePointDto>();
            foreach (var pair in coords.EnumerateArray())
            {
                points.Add(new RoutePointDto
                {
                    Longitude = pair[0].GetDouble(),
                    Latitude = pair[1].GetDouble()
                });
            }

            return new RouteResultDto { Points = points };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSRM routing failed.");
            return new RouteResultDto();
        }
    }

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    internal static List<(double lat, double lng)> DecodePolyline(string encoded)
    {
        var points = new List<(double lat, double lng)>();
        int index = 0, lat = 0, lng = 0;

        while (index < encoded.Length)
        {
            lat += Next(encoded, ref index);
            lng += Next(encoded, ref index);
            points.Add((lat / 1e5, lng / 1e5));
        }

        return points;
    }

    private static int Next(string encoded, ref int index)
    {
        int result = 0, shift = 0, b;
        do
        {
            b = encoded[index++] - 63;
            result |= (b & 0x1f) << shift;
            shift += 5;
        } while (b >= 0x20);

        return (result & 1) != 0 ? ~(result >> 1) : result >> 1;
    }

    private sealed class NominatimHit
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; } = "";

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = "";

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }

    private sealed class PhotonResponse
    {
        [JsonPropertyName("features")]
        public List<PhotonFeature>? Features { get; set; }
    }

    private sealed class PhotonFeature
    {
        [JsonPropertyName("geometry")]
        public PhotonGeometry? Geometry { get; set; }

        [JsonPropertyName("properties")]
        public PhotonProperties? Properties { get; set; }
    }

    private sealed class PhotonGeometry
    {
        [JsonPropertyName("coordinates")]
        public double[]? Coordinates { get; set; }
    }

    private sealed class PhotonProperties
    {
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
    }
}
