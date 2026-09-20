namespace NvidiaChatBot.Services;

public class MemoryCleanupHostedService : IHostedService, IDisposable
{
    private readonly ChatMemoryService _chatMemoryService;
    private Timer? _timer;

    public MemoryCleanupHostedService(ChatMemoryService chatMemoryService)
    {
        _chatMemoryService = chatMemoryService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Run cleanup every 1 minute
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        // Clean sessions older than 5 minutes
        _chatMemoryService.CleanupInactiveSessions(TimeSpan.FromMinutes(5));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
