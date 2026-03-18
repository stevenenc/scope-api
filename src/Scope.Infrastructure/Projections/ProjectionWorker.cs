using Microsoft.Extensions.Hosting;

namespace Scope.Infrastructure.Projections;

public class ProjectionWorker : BackgroundService
{
    private readonly ProjectionRunner _projectionRunner;

    public ProjectionWorker(ProjectionRunner projectionRunner)
    {
        _projectionRunner = projectionRunner;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _projectionRunner.RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProjectionWorker] Error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}