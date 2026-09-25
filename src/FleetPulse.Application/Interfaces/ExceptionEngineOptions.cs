namespace FleetPulse.Application.Interfaces;

public class ExceptionEngineOptions
{
    public const string SectionName = "ExceptionEngine";

    /// <summary>Seconds between full scans of all in-progress trips by the hosted engine.</summary>
    public int IntervalSeconds { get; set; } = 30;
}