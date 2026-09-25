namespace FleetPulse.Application.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, string? toName = null, CancellationToken cancellationToken = default);
}