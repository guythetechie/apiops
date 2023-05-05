using System;
using System.IO;

namespace common;

public readonly record struct ServiceDirectory : IArtifactDirectory
{
    public ArtifactPath Path { get; }

    public ServiceDirectory(DirectoryInfo directory)
    {
        Path = new ArtifactPath(directory.FullName);
    }
}

public readonly record struct ServiceUri
{
    public Uri Value { get; }

    public ServiceUri(Uri uri)
    {
        Value= uri;
    }
}