using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace extractor;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed class Extractor : BackgroundService
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    private readonly ApiManagementService apiManagementService;
    private readonly ILogger logger;
    private readonly IHostApplicationLifetime applicationLifetime;

    public Extractor(ApiManagementService apiManagementService, ILogger<Extractor> logger, IHostApplicationLifetime applicationLifetime)
    {
        this.apiManagementService = apiManagementService;
        this.logger = logger;
        this.applicationLifetime = applicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Beginning execution...");

            await ExportService(cancellationToken);

            logger.LogInformation("Execution complete.");
        }
        catch (OperationCanceledException)
        {
            // Don't throw if operation was canceled
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "");
            Environment.ExitCode = -1;
            throw;
        }
        finally
        {
            applicationLifetime.StopApplication();
        }
    }

    private async ValueTask ExportService(CancellationToken cancellationToken)
    {
        await apiManagementService.Export(cancellationToken);
    }
}