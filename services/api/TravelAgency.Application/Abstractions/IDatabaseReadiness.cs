namespace TravelAgency.Application.Abstractions;

public interface IDatabaseReadiness
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
