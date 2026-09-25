namespace FleetPulse.Application.Interfaces;

public class RoutingOptions
{
    public const string SectionName = "Routing";

    /// <summary>
    /// Base URL of a self-hosted OSRM instance (e.g. "http://osrm:5000" in the
    /// docker network). When empty the app never calls a public OSRM server:
    /// routing falls back to Google (if a key is set) or a straight line.
    /// </summary>
    public string? BaseUrl { get; set; }
}