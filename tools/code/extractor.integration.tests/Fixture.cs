using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ApiManagement;
using common;
using Medallion.Shell;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace extractor.integration.tests;

internal static class Fixture
{
    public static IServiceProvider DefaultServiceProvider { get; } = GetServiceProvider();

    private static ServiceProvider GetServiceProvider()
    {
        return new ServiceCollection().AddSingleton(GetConfiguration)
                                      .AddSingleton(GetTokenCredential)
                                      .AddSingletonStruct(GetArmEnvironment)
                                      .AddSingleton(GetArmClient)
                                      .AddSingleton(GetApiManagementServiceResource)
                                      .AddSingletonStruct(GetServiceDirectory)
                                      .BuildServiceProvider();
    }

    private static IConfiguration GetConfiguration(IServiceProvider provider)
    {
        var builder = new ConfigurationBuilder().AddEnvironmentVariables()
                                                .AddUserSecrets(typeof(Fixture).Assembly)
                                                .AddInMemoryCollection(new Dictionary<string, string?>
                                                {
                                                    ["EXTRACTOR_ARTIFACTS_PATH"] = Path.Combine(Path.GetTempPath(), "apiops-extractor", Path.GetRandomFileName())
                                                });

        // Add YAML configuration if path is defined
        var configuration = builder.Build();
        configuration.TryGetValue("CONFIGURATION_YAML_PATH")
                     .Iter(path => builder.AddYamlFile(path));

        return builder.Build();
    }

    private static TokenCredential GetTokenCredential(IServiceProvider provider)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();

        return GetTokenCredential(configuration);
    }

    private static TokenCredential GetTokenCredential(IConfiguration configuration)
    {
        return configuration.TryGetValue("AZURE_BEARER_TOKEN")
                            .Map(GetCredentialFromToken)
                            .IfNone(() =>
                            {
                                var authorityHost = GetAzureAuthorityHost(configuration);

                                return GetDefaultAzureCredential(authorityHost);
                            });
    }

    private static Uri GetAzureAuthorityHost(IConfiguration configuration)
    {
        var cloudEnvironment = configuration.TryGetValue("AZURE_CLOUD_ENVIRONMENT")
                                            .IfNoneNull();

        return cloudEnvironment switch
        {
            null => AzureAuthorityHosts.AzurePublicCloud,
            "AzureGlobalCloud" or nameof(AzureAuthorityHosts.AzurePublicCloud) => AzureAuthorityHosts.AzurePublicCloud,
            "AzureChinaCloud" or nameof(AzureAuthorityHosts.AzureChina) => AzureAuthorityHosts.AzureChina,
            "AzureUSGovernment" or nameof(AzureAuthorityHosts.AzureGovernment) => AzureAuthorityHosts.AzureGovernment,
            "AzureGermanCloud" or nameof(AzureAuthorityHosts.AzureGermany) => AzureAuthorityHosts.AzureGermany,
            _ => throw new InvalidOperationException($"AZURE_CLOUD_ENVIRONMENT is invalid. Valid values are {nameof(AzureAuthorityHosts.AzurePublicCloud)}, {nameof(AzureAuthorityHosts.AzureChina)}, {nameof(AzureAuthorityHosts.AzureGovernment)}, {nameof(AzureAuthorityHosts.AzureGermany)}")
        };
    }

    private static TokenCredential GetDefaultAzureCredential(Uri azureAuthorityHost)
    {
        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            AuthorityHost = azureAuthorityHost
        });
    }

    private static TokenCredential GetCredentialFromToken(string token)
    {
        var jsonWebToken = new JsonWebToken(token);
        var expirationDate = new DateTimeOffset(jsonWebToken.ValidTo);
        var accessToken = new AccessToken(token, expirationDate);

        return DelegatedTokenCredential.Create((context, cancellationToken) => accessToken);
    }

    private static ArmEnvironment GetArmEnvironment(IServiceProvider provider)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var environment = configuration.TryGetValue("AZURE_CLOUD_ENVIRONMENT")
                                       .IfNoneNull();

        return environment switch
        {
            null => ArmEnvironment.AzurePublicCloud,
            "AzureGlobalCloud" or nameof(ArmEnvironment.AzurePublicCloud) => ArmEnvironment.AzurePublicCloud,
            "AzureChinaCloud" or nameof(ArmEnvironment.AzureChina) => ArmEnvironment.AzureChina,
            "AzureUSGovernment" or nameof(ArmEnvironment.AzureGovernment) => ArmEnvironment.AzureGovernment,
            "AzureGermanCloud" or nameof(ArmEnvironment.AzureGermany) => ArmEnvironment.AzureGermany,
            _ => throw new InvalidOperationException($"AZURE_CLOUD_ENVIRONMENT is invalid. Valid values are {nameof(ArmEnvironment.AzurePublicCloud)}, {nameof(ArmEnvironment.AzureChina)}, {nameof(ArmEnvironment.AzureGovernment)}, {nameof(ArmEnvironment.AzureGermany)}")
        };
    }

    private static ArmClient GetArmClient(IServiceProvider provider)
    {
        var credential = provider.GetRequiredService<TokenCredential>();

        var configuration = provider.GetRequiredService<IConfiguration>();
        var subscriptionId = configuration.GetValue("AZURE_SUBSCRIPTION_ID");

        var armEnvironment = provider.GetRequiredService<ArmEnvironment>();
        var options = new ArmClientOptions { Environment = armEnvironment };

        return new ArmClient(credential, subscriptionId, options);
    }

    private static ApiManagementServiceResource GetApiManagementServiceResource(IServiceProvider provider)
    {
        var client = provider.GetRequiredService<ArmClient>();

        var configuration = provider.GetRequiredService<IConfiguration>();
        var resourceGroupName = configuration.GetValue("AZURE_RESOURCE_GROUP_NAME");
        var resourceGroup = client.GetDefaultSubscription()
                                  .GetResourceGroups()
                                  .Get(resourceGroupName);

        var apiManagementServiceName = configuration.TryGetValue("API_MANAGEMENT_SERVICE_NAME")
                                                    .IfNone(() => configuration.GetValue("apimServiceName"));

        return resourceGroup.Value.GetApiManagementService(apiManagementServiceName);
    }

    private static ServiceDirectory GetServiceDirectory(IServiceProvider provider)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var directoryPath = configuration.GetValue("EXTRACTOR_ARTIFACTS_PATH");
        var directory = new DirectoryInfo(directoryPath);
        return new ServiceDirectory(directory);
    }

    public static async ValueTask RunExtractor(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var serviceResource = provider.GetRequiredService<ApiManagementServiceResource>();
        var configuration = provider.GetRequiredService<IConfiguration>();

        var command = Command.Run("dotnet",
                                  "run",
                                  "--project",
                                  configuration.GetValue("EXTRACTOR_PROJECT_PATH"),
                                  "--AZURE_BEARER_TOKEN",
                                  await GetBearerToken(provider, cancellationToken),
                                  "--API_MANAGEMENT_SERVICE_OUTPUT_FOLDER_PATH",
                                  configuration.GetValue("EXTRACTOR_ARTIFACTS_PATH"),
                                  "--API_MANAGEMENT_SERVICE_NAME",
                                  serviceResource.Id.Name,
                                  "--AZURE_SUBSCRIPTION_ID",
                                  serviceResource.Id.SubscriptionId ?? throw new InvalidOperationException("Subscription ID is null"),
                                  "--AZURE_RESOURCE_GROUP_NAME",
                                  serviceResource.Id.ResourceGroupName ?? throw new InvalidOperationException("Resource group name is null"));
        var commandResult = await command.Task;
        if (commandResult.Success is false)
        {
            throw new InvalidOperationException($"Running extractor failed with error {commandResult.StandardError}. Output is {commandResult.StandardOutput}");
        }
    }

    private static async ValueTask<string> GetBearerToken(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var tokenCredential = provider.GetRequiredService<TokenCredential>();

        var requestContext = new TokenRequestContext();// (scopes: new[] { "/.default" });
        var token = await tokenCredential.GetTokenAsync(requestContext, cancellationToken);

        return token.Token;
    }
}