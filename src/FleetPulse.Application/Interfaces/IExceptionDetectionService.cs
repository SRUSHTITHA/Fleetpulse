namespace FleetPulse.Application.Interfaces;

public interface IExceptionDetectionService
{
    /// <summary>
    /// Evaluates a single trip against all enabled exception rules.
    /// Dedup: an open exception for (trip, rule) is updated, not re-created; a rule that
    /// stops firing resolves the open exception. New exceptions are broadcast to ops via
    /// <see cref="ILocationBroadcaster.BroadcastExceptionAsync"/>.
    /// </summary>
    Task EvaluateTripAsync(Guid tripId, CancellationToken cancellationToken = default);
}