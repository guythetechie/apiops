using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace extractor;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes
internal sealed class ApiManagementService
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
{
    private readonly ApiService apiService;
    private readonly ILogger logger;

    public ApiManagementService(ApiService apiService, ILogger<ApiManagementService> logger)
    {
        this.apiService = apiService;
        this.logger = logger;
    }

    public async ValueTask Export(CancellationToken cancellationToken)
    {
        logger.LogInformation("Exporting apis...");
        await apiService.ExportAll(cancellationToken);
    }
}