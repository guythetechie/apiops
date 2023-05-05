using Azure.Core;
using LanguageExt;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace common;

public static partial class Serialization
{
    public static JsonNode Serialize(this ResourceIdentifier resourceIdentifier)
        => JsonValue.Create(resourceIdentifier.ToString()) ?? throw new JsonException("Value cannot be null.");

    public static ResourceIdentifier DeserializeResourceIdentifier(JsonNode node) =>
        node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var value)
            ? new ResourceIdentifier(value)
            : throw new JsonException("Node must be a string JSON value.");

    // Placeholder for exposing IUtf8JsonSerializable (https://github.com/Azure/azure-sdk-for-net/pull/35742) 
    //public static JsonObject SerializeIUtf8JsonSerializable<T>(T value) where T : IUtf8JsonSerializable
    //{
    //    using var stream = SerializeIUtf8JsonSerializableToStream(value);
    //    var node = JsonNode.Parse(stream) ?? throw new JsonException("Failed to deserialize stream.");
    //    return node.AsObject();
    //}

    //private static MemoryStream SerializeIUtf8JsonSerializableToStream<T>(T value) where T : IUtf8JsonSerializable
    //{
    //    var stream = new MemoryStream();
    //    using var writer = new Utf8JsonWriter(stream);
    //    writer.WritePropertyName("apiRevision"u8);
    //    var writeMethod = value.GetType()
    //                           .GetMethods(BindingFlags.NonPublic)
    //                           .FirstOrDefault(method => method.Name.Equals("Write", StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException($"Could not find method 'Write' on type {typeof(T).Name}.");

    //    writeMethod.Invoke(value, new[] { writer });
    //    stream.Position = 0;

    //    return stream;
    //}
}
