using Microsoft.OpenApi;
using System;

namespace common;

public interface IApiSpecificationFile : IArtifactFile
{
    ApiDirectory ApiDirectory { get; }
}

public readonly record struct OpenApiSpecificationFile : IApiSpecificationFile
{
    public ArtifactPath Path { get; }

    public ApiDirectory ApiDirectory { get; }

    public OpenApiSpecVersion Version { get; }

    public OpenApiFormat Format { get; }

    public OpenApiSpecificationFile(OpenApiSpecVersion version, OpenApiFormat format, ApiDirectory apiDirectory)
    {
        var fileName = GetFileName(format);
        Path = apiDirectory.Path.Append(fileName);
        ApiDirectory = apiDirectory;
        Version = version;
        Format = format;
    }

    private static string GetFileName(OpenApiFormat format) =>
        format switch
        {
            OpenApiFormat.Json => "specification.json",
            OpenApiFormat.Yaml => "specification.yaml",
            _ => throw new NotSupportedException()
        };
}

public readonly record struct GraphQlSpecificationFile : IApiSpecificationFile
{
    public static string Name { get; } = "specification.graphql";

    public ArtifactPath Path { get; }

    public ApiDirectory ApiDirectory { get; }

    public GraphQlSpecificationFile(ApiDirectory apiDirectory)
    {
        Path = apiDirectory.Path.Append(Name);
        ApiDirectory = apiDirectory;
    }
}

public readonly record struct WsdlSpecificationFile : IApiSpecificationFile
{
    public static string Name { get; } = "specification.wsdl";

    public ArtifactPath Path { get; }

    public ApiDirectory ApiDirectory { get; }

    public WsdlSpecificationFile(ApiDirectory apiDirectory)
    {
        Path = apiDirectory.Path.Append(Name);
        ApiDirectory = apiDirectory;
    }
}

public readonly record struct WadlSpecificationFile : IApiSpecificationFile
{
    public static string Name { get; } = "specification.wadl";

    public ArtifactPath Path { get; }

    public ApiDirectory ApiDirectory { get; }

    public WadlSpecificationFile(ApiDirectory apiDirectory)
    {
        Path = apiDirectory.Path.Append(Name);
        ApiDirectory = apiDirectory;
    }
}