
using TeamCepheus.Robot.Sensors;

/// <summary>
/// Background service that continuously runs the sensor task cycle.
/// </summary>
class SensorBackgroundService : BackgroundService
{
    private readonly ISensorsController _sensorsController;
    private readonly ILogger<SensorBackgroundService> _logger;

    public SensorBackgroundService(ISensorsController sensorsController, ILogger<SensorBackgroundService> logger)
    {
        _sensorsController = sensorsController;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SensorBackgroundService starting");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _sensorsController.SensorTask();
                await Task.Delay(100, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown - treat as a graceful stop
            _logger.LogInformation("SensorBackgroundService cancellation requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SensorBackgroundService");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SensorBackgroundService stopping");
        _sensorsController.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
