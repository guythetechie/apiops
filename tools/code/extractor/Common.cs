using Azure.Core;
using Azure.Core.Pipeline;
using Azure.ResourceManager;
using Azure.ResourceManager.ApiManagement.Models;
using common;
using Flurl;
using Microsoft.OpenApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace extractor;

//internal abstract record DefaultApiSpecification
//{
//    public record Wadl : DefaultApiSpecification { }

//    public record OpenApi(OpenApiSpecVersion Version, OpenApiFormat Format) : DefaultApiSpecification { }
//}

//internal delegate IAsyncEnumerable<(ApiName Name, ApiCreateOrUpdateContent Content)> ListApis(CancellationToken cancellationToken);

//internal delegate IAsyncEnumerable<JsonObject> ListRestResources(Uri uri, CancellationToken cancellationToken);

//internal delegate ValueTask<JsonObject> GetRestResource(Uri uri, CancellationToken cancellationToken);

//internal delegate ValueTask<Stream> DownloadResource(Uri uri, CancellationToken cancellationToken);

//internal static class ResourceIdentifierExtensions
//{
//    public static Uri GetUri(this ResourceIdentifier resourceIdentifier, ArmEnvironment environment)
//    {
//        return environment.Endpoint
//                          .AppendPathSegment(resourceIdentifier.ToString())
//                          .ToUri();
//    }
//}

internal readonly record struct AuthenticatedHttpPipeline
{
    public HttpPipeline Value { get; }

    public AuthenticatedHttpPipeline(BearerTokenAuthenticationPolicy policy)
    {
        Value = HttpPipelineBuilder.Build(ClientOptions.Default, policy);
    }
};

internal readonly record struct NonAuthenticatedHttpPipeline
{
    public static HttpPipeline Value { get; } = HttpPipelineBuilder.Build(ClientOptions.Default, new NonAuthenticatedPipelinePolicy());

    private sealed class NonAuthenticatedPipelinePolicy : HttpPipelinePolicy
    {
        public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            RemoveAuthorizationHeader(message);
            ProcessNext(message, pipeline);
        }

        public override async ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            RemoveAuthorizationHeader(message);
            await ProcessNextAsync(message, pipeline);
        }

        private static void RemoveAuthorizationHeader(HttpMessage message)
        {
            if (message.Request.Headers.TryGetValue(HttpHeader.Names.Authorization, out var _))
            {
                message.Request.Headers.Remove(HttpHeader.Names.Authorization);
            }
        }
    }
}
