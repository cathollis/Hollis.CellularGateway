namespace Hollis.CellularGateway.WebApi.Services;

/// <summary>
/// Manages the lifecycle of the <see cref="AtClient.AtClient"/> — opens the modem
/// connection on application start and closes it on shutdown.
/// </summary>
public class ModemBackgroundService : BackgroundService
{
    private readonly AtClient.AtClient _atClient;
    private readonly ILogger<ModemBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public ModemBackgroundService(
        AtClient.AtClient atClient,
        IConfiguration configuration,
        ILogger<ModemBackgroundService> logger)
    {
        _atClient = atClient;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Opening modem connection...");
            await _atClient.OpenAsync(stoppingToken);
            _logger.LogInformation("Modem connection opened successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open modem connection");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Closing modem connection...");
        try
        {
            await _atClient.CloseAsync();
            _logger.LogInformation("Modem connection closed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing modem connection");
        }
    }
}
