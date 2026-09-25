namespace FleetPulse.Application.Interfaces;

public class GoogleMapsOptions
{
    public const string SectionName = "GoogleMaps";

    /// <summary>Google Cloud API key with the Roads API (nearestRoads) enabled.</summary>
    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://roads.googleapis.com/v1";

    /// <summary>Seconds a snapped result is reused for the same ~11 m grid cell.</summary>
    public int CacheTtlSeconds { get; set; } = 120;

    public int CacheMaxEntries { get; set; } = 8192;
}