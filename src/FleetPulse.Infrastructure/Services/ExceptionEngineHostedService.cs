using FleetPulse.Application.Interfaces;
using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetPulse.Infrastructure.Services;

/// <summary>
/// Periodic full scan of all in-progress trips so issues surface even when a driver stops
/// reporting (the ingest hook in <see cref="LocationService"/> covers per-point detections).
/// </summary>
public class ExceptionEngineHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval;
    private readonly ILogger<ExceptionEngineHostedService> _logger;

    public ExceptionEngineHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<ExceptionEngineOptions> options,
        ILogger<ExceptionEngineHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.IntervalSeconds));
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception engine cycle failed.");
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var trips = await scope.ServiceProvider.GetRequiredService<ITripRepository>()
            .GetByStatusAsync(TripStatus.InProgress, ct);
        var detector = scope.ServiceProvider.GetRequiredService<IExceptionDetectionService>();

        foreach (var trip in trips)
            await detector.EvaluateTripAsync(trip.Id, ct);
    }
}