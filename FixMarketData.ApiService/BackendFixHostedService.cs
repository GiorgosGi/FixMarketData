using QuickFix;

namespace FixMarketData.Backend;

public sealed class BackendFixHostedService(
    BackendFixApplication backendFixApplication,
    ILogger<BackendFixHostedService> logger) : IHostedService, IDisposable
{
    private IInitiator? _initiator;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = new SessionSettings("initiator.cfg");
        var storeFactory = new FileStoreFactory(settings);
        var logFactory = new FileLogFactory(settings);

        _initiator = new QuickFix.Transport.SocketInitiator(backendFixApplication, storeFactory, settings, logFactory);
        _initiator.Start();

        logger.LogInformation("FIX initiator started.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _initiator?.Stop();
        logger.LogInformation("FIX initiator stopped.");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _initiator?.Dispose();
    }
}
