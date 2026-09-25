namespace FleetPulse.Application.DTOs.Geo;

public class GeocodeResultDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? DisplayName { get; set; }
}

public class RoutePointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class RouteResultDto
{
    public List<RoutePointDto> Points { get; set; } = new();
}
