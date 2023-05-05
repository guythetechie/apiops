using Azure.Core;
using Azure.ResourceManager.ApiManagement;
using Azure.ResourceManager.ApiManagement.Models;
using LanguageExt;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace common;

public readonly record struct ApisDirectory : IArtifactDirectory
{
    public static string Name { get; } = "apis";

    public ArtifactPath Path { get; }

    public ServiceDirectory ServiceDirectory { get; }

    public ApisDirectory(ServiceDirectory serviceDirectory)
    {
        Path = serviceDirectory.Path.Append(Name);
        ServiceDirectory = serviceDirectory;
    }
}

public readonly record struct ApiName
{
    public ApiName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("API name cannot be null or whitespace.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public readonly record struct ApiDirectory : IArtifactDirectory
{
    public ArtifactPath Path { get; }

    public ApisDirectory ApisDirectory { get; }

    public ApiDirectory(ApiName apiName, ApisDirectory apisDirectory)
    {
        Path = apisDirectory.Path.Append(apiName.ToString());
        ApisDirectory = apisDirectory;
    }
}

public readonly record struct ApiInformationFile : IArtifactFile
{
    public static string Name { get; } = "apiInformation.json";

    public ArtifactPath Path { get; }

    public ApiDirectory ApiDirectory { get; }

    public string ApiName => ApiDirectory.GetName();

    public ApiInformationFile(ApiDirectory apiDirectory)
    {
        Path = apiDirectory.Path.Append(Name);
        ApiDirectory = apiDirectory;
    }
}
public static partial class Serialization
{
    public static JsonObject Serialize(this ApiCreateOrUpdateContent content)
    {
        var json = new JsonObject()
            .AddPropertyIfNotNull("apiRevision", content.ApiRevision)
            .AddPropertyIfNotNull("apiRevisionDescription", content.ApiRevisionDescription)
            .AddPropertyIfNotNull("apiType", content.SoapApiType?.Serialize())
            .AddPropertyIfNotNull("apiVersion", content.ApiVersion)
            .AddPropertyIfNotNull("apiVersionDescription", content.ApiVersionDescription)
            .AddPropertyIfNotNull("apiVersionSet", content.ApiVersionSet?.Serialize())
            .AddPropertyIfNotNull("apiVersionSetId", content.ApiVersionSetId?.Serialize())
            .AddPropertyIfNotNull("authenticationSettings", content.AuthenticationSettings?.Serialize())
            .AddPropertyIfNotNull("contact", content.Contact?.Serialize())
            .AddPropertyIfNotNull("description", content.Description)
            .AddPropertyIfNotNull("displayName", content.DisplayName)
            .AddPropertyIfNotNull("format", content.Format?.Serialize())
            .AddPropertyIfNotNull("isCurrent", content.IsCurrent)
            .AddPropertyIfNotNull("license", content.License?.Serialize())
            .AddPropertyIfNotNull("path", content.Path)
            .AddPropertyIfNotNull("serviceUrl", content.ServiceUri?.AbsoluteUri)
            .AddPropertyIfNotNull("sourceApiId", content.SourceApiId?.Serialize())
            .AddPropertyIfNotNull("subscriptionKeyParameterNames", content.SubscriptionKeyParameterNames?.Serialize())
            .AddPropertyIfNotNull("subscriptionRequired", content.IsSubscriptionRequired)
            .AddPropertyIfNotNull("termsOfServiceUrl", content.TermsOfServiceUri?.AbsoluteUri)
            .AddPropertyIfNotNull("type", content.ApiType?.Serialize())
            .AddPropertyIfNotNull("value", content.Value)
            .AddPropertyIfNotNull("wsdlSelector", content.WsdlSelector?.Serialize());

        if (content.Protocols is not null)
        {
            json.AddPropertyIfNotNull("protocols", content.Protocols
                                                          .Map(Serialize)
                                                          .ToJsonArray());
        }

        return json;
    }

    public static ApiCreateOrUpdateContent DeserializeApiCreateOrUpdateContent(JsonObject jsonObject)
    {
        var content = new ApiCreateOrUpdateContent()
        {
            ApiRevision = jsonObject.TryGetStringProperty("apiRevision")
                                    .IfLeftNull(),
            ApiRevisionDescription = jsonObject.TryGetStringProperty("apiRevisionDescription")
                                               .IfLeftNull(),
            ApiType = jsonObject.TryGetProperty("type")
                                .Map(DeserializeApiType)
                                .IfLeftNull(),
            ApiVersion = jsonObject.TryGetStringProperty("apiVersion")
                                   .IfLeftNull(),
            ApiVersionDescription = jsonObject.TryGetStringProperty("apiVersionDescription")
                                              .IfLeftNull(),
            ApiVersionSet = jsonObject.TryGetJsonObjectProperty("apiVersionSet")
                                      .Map(DeserializeApiVersionSetContractDetails)
                                      .IfLeftNull(),
            ApiVersionSetId = jsonObject.TryGetProperty("apiVersionSetId")
                                        .Map(DeserializeResourceIdentifier)
                                        .IfLeftNull(),
            AuthenticationSettings = jsonObject.TryGetJsonObjectProperty("authenticationSettings")
                                               .Map(DeserializeAuthenticationSettingsContract)
                                               .IfLeftNull(),
            Contact = jsonObject.TryGetJsonObjectProperty("contact")
                                .Map(DeserializeApiContactInformation)
                                .IfLeftNull(),
            Description = jsonObject.TryGetStringProperty("description")
                                    .IfLeftNull(),
            DisplayName = jsonObject.TryGetStringProperty("displayName")
                                    .IfLeftNull(),
            Format = jsonObject.TryGetProperty("format")
                               .Map(DeserializeContentFormat)
                               .IfLeftNull(),
            IsCurrent = jsonObject.TryGetBoolProperty("isCurrent")
                                  .IfLeftNull(),
            IsSubscriptionRequired = jsonObject.TryGetBoolProperty("subscriptionRequired")
                                               .IfLeftNull(),
            License = jsonObject.TryGetJsonObjectProperty("license")
                                .Map(DeserializeApiLicenseInformation)
                                .IfLeftNull(),
            Path = jsonObject.TryGetStringProperty("path")
                             .IfLeftNull(),
            ServiceUri = jsonObject.TryGetUriProperty("serviceUrl")
                                   .IfLeftNull(),
            SoapApiType = jsonObject.TryGetProperty("apiType")
                                    .Map(DeserializeSoapApiType)
                                    .IfLeftNull(),
            SourceApiId = jsonObject.TryGetProperty("sourceApiId")
                                    .Map(DeserializeResourceIdentifier)
                                    .IfLeftNull(),
            SubscriptionKeyParameterNames = jsonObject.TryGetJsonObjectProperty("subscriptionKeyParameterNames")
                                                      .Map(DeserializeSubscriptionKeyParameterNamesContract)
                                                      .IfLeftNull(),
            TermsOfServiceUri = jsonObject.TryGetUriProperty("termsOfServiceUrl")
                                          .IfLeftNull(),
            Value = jsonObject.TryGetStringProperty("value")
                              .IfLeftNull(),
            WsdlSelector = jsonObject.TryGetJsonObjectProperty("wsdlSelector")
                                     .Map(DeserializeApiCreateOrUpdatePropertiesWsdlSelector)
                                     .IfLeftNull()
        };

        jsonObject.TryGetJsonArrayProperty("protocols")
                  .Map(jsonArray => jsonArray.Choose(node => Prelude.Optional(node)))
                  .Map(nodes => nodes.Map(DeserializeApiOperationInvokableProtocol))
                  .Iter(nodes => nodes.Iter(content.Protocols.Add));

        return content;
    }

    public static JsonNode Serialize(this ApiType apiType)
        => JsonValue.Create(apiType.ToString()) ?? throw new JsonException("Value cannot be null.");

    public static ApiType DeserializeApiType(JsonNode node) =>
        node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var value)
            ? value switch
            {
                _ when value.Equals(ApiType.Http.ToString(), StringComparison.OrdinalIgnoreCase) => ApiType.Http,
                _ when value.Equals(ApiType.Soap.ToString(), StringComparison.OrdinalIgnoreCase) => ApiType.Soap,
                _ when value.Equals(ApiType.WebSocket.ToString(), StringComparison.OrdinalIgnoreCase) => ApiType.WebSocket,
                _ when value.Equals(ApiType.GraphQL.ToString(), StringComparison.OrdinalIgnoreCase) => ApiType.GraphQL,
                _ => throw new JsonException($"'{value}' is not a valid api type.")
            }
            : throw new JsonException("Node must be a string JSON value.");

    public static JsonNode Serialize(this SoapApiType apiType)
        => JsonValue.Create(apiType.ToString()) ?? throw new JsonException("Value cannot be null.");

    public static SoapApiType DeserializeSoapApiType(JsonNode node) =>
        node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var value)
            ? value switch
            {
                _ when value.Equals(SoapApiType.SoapToRest.ToString(), StringComparison.OrdinalIgnoreCase) => SoapApiType.SoapToRest,
                _ when value.Equals(SoapApiType.SoapPassThrough.ToString(), StringComparison.OrdinalIgnoreCase) => SoapApiType.SoapPassThrough,
                _ when value.Equals(SoapApiType.WebSocket.ToString(), StringComparison.OrdinalIgnoreCase) => SoapApiType.WebSocket,
                _ when value.Equals(SoapApiType.GraphQL.ToString(), StringComparison.OrdinalIgnoreCase) => SoapApiType.GraphQL,
                _ => throw new JsonException($"'{value}' is not a valid api type.")
            }
            : throw new JsonException("Node must be a string JSON value.");

    public static JsonObject Serialize(this ApiVersionSetContractDetails contractDetails) =>
        new JsonObject()
            .AddPropertyIfNotNull("description", contractDetails.Description)
            .AddPropertyIfNotNull("id", contractDetails.Id)
            .AddPropertyIfNotNull("name", contractDetails.Name)
            .AddPropertyIfNotNull("versionHeaderName", contractDetails.VersionHeaderName)
            .AddPropertyIfNotNull("versioningScheme", contractDetails.VersioningScheme?.Serialize())
            .AddPropertyIfNotNull("versionQueryName", contractDetails.VersionQueryName);

    public static ApiVersionSetContractDetails DeserializeApiVersionSetContractDetails(JsonObject jsonObject) =>
        new()
        {
            Description = jsonObject.TryGetStringProperty("description")
                                    .IfLeftNull(),
            Id = jsonObject.TryGetStringProperty("id")
                           .IfLeftNull(),
            Name = jsonObject.TryGetStringProperty("name")
                             .IfLeftNull(),
            VersionHeaderName = jsonObject.TryGetStringProperty("versionHeaderName")
                                          .IfLeftNull(),
            VersioningScheme = jsonObject.TryGetProperty("versioningScheme")
                                         .Map(DeserializeVersioningScheme)
                                         .IfLeftNull(),
            VersionQueryName = jsonObject.TryGetStringProperty("versionQueryName")
                                         .IfLeftNull()
        };

    public static JsonNode Serialize(this VersioningScheme versioningScheme)
        => JsonValue.Create(versioningScheme.ToString()) ?? throw new JsonException("Value cannot be null.");

    public static VersioningScheme DeserializeVersioningScheme(JsonNode node) =>
        node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var value)
            ? value switch
            {
                _ when value.Equals(VersioningScheme.Header.ToString(), StringComparison.OrdinalIgnoreCase) => VersioningScheme.Header,
                _ when value.Equals(VersioningScheme.Query.ToString(), StringComparison.OrdinalIgnoreCase) => VersioningScheme.Query,
                _ when value.Equals(VersioningScheme.Segment.ToString(), StringComparison.OrdinalIgnoreCase) => VersioningScheme.Segment,
                _ => throw new JsonException($"'{value}' is not a valid versioning scheme.")
            }
            : throw new JsonException("Node must be a string JSON value.");

    public static JsonObject Serialize(this AuthenticationSettingsContract contract) =>
        new JsonObject()
            .AddPropertyIfNotNull("oAuth2", contract.OAuth2?.Serialize())
            .AddPropertyIfNotNull("openid", contract.OpenId?.Serialize());

    public static AuthenticationSettingsContract DeserializeAuthenticationSettingsContract(JsonObject jsonObject) =>
        new()
        {
            OAuth2 = jsonObject.TryGetJsonObjectProperty("oAuth2")
                               .Map(DeserializeOAuth2AuthenticationSettingsContract)
                               .IfLeftNull(),
            OpenId = jsonObject.TryGetJsonObjectProperty("openid")
                               .Map(DeserializeOpenIdAuthenticationSettingsContract)
                               .IfLeftNull()
        };

    public static JsonObject Serialize(this OAuth2AuthenticationSettingsContract contract) =>
        new JsonObject()
            .AddPropertyIfNotNull("authorizationServerId", contract.AuthorizationServerId)
            .AddPropertyIfNotNull("scope", contract.Scope);

    public static OAuth2AuthenticationSettingsContract DeserializeOAuth2AuthenticationSettingsContract(JsonObject jsonObject) =>
        new()
        {
            AuthorizationServerId = jsonObject.TryGetStringProperty("authorizationServerId")
                                              .IfLeftNull(),
            Scope = jsonObject.TryGetStringProperty("scope")
                              .IfLeftNull()
        };

    public static JsonObject Serialize(this OpenIdAuthenticationSettingsContract contract)
    {
        var json = new JsonObject()
                        .AddPropertyIfNotNull("openidProviderId", contract.OpenIdProviderId);

        if (contract.BearerTokenSendingMethods is not null)
        {
            json.AddProperty("bearerTokenSendingMethods", contract.BearerTokenSendingMethods
                                                                  .Choose(method => Prelude.Optional((JsonNode?)method.ToString()))
                                                                  .ToJsonArray());
        }

        return json;
    }

    public static OpenIdAuthenticationSettingsContract DeserializeOpenIdAuthenticationSettingsContract(JsonObject jsonObject)
    {
        var contract = new OpenIdAuthenticationSettingsContract()
        {
            OpenIdProviderId = jsonObject.TryGetStringProperty("openidProviderId")
                                         .IfLeftNull()
        };

        jsonObject.TryGetJsonArrayProperty("bearerTokenSendingMethods")
                  .Map(jsonArray => jsonArray.Choose(node => node.TryAsJsonValue()
                                                                 .Bind(value => value.TryGetValue<string>()))
                                             .Map(method => new BearerTokenSendingMethod(method)))
                  .Iter(methods => methods.Iter(contract.BearerTokenSendingMethods.Add));

        return contract;
    }

    public static JsonObject Serialize(this ApiContactInformation contactInformation) =>
        new JsonObject()
            .AddPropertyIfNotNull("email", contactInformation.Email)
            .AddPropertyIfNotNull("name", contactInformation.Name)
            .AddPropertyIfNotNull("url", contactInformation.Uri.ToString());

    public static ApiContactInformation DeserializeApiContactInformation(JsonObject jsonObject) =>
        new()
        {
            Email = jsonObject.TryGetStringProperty("email")
                              .IfLeftNull(),
            Name = jsonObject.TryGetStringProperty("name")
                             .IfLeftNull(),
            Uri = jsonObject.TryGetUriProperty("url")
                            .IfLeftNull()
        };

    public static JsonNode Serialize(this ContentFormat format)
        => JsonValue.Create(format.ToString()) ?? throw new JsonException("Value cannot be null.");

    public static ContentFormat DeserializeContentFormat(JsonNode node) =>
        node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var value)
            ? value switch
            {
                _ when value.Equals(ContentFormat.WadlXml.ToString(), StringComparison.OrdinalIgnoreCase) => ContentFormat.WadlXml,
                _ when value.Equals(ContentFormat.WadlLinkJson.ToString(), StringComparison.OrdinalIgnoreCase) => ContentFormat.WadlLinkJson,
                _ when value.Equals(ContentFormat.SwaggerJson.ToString(), StringComparison.OrdinalIgnoreCase) => ContentFormat.SwaggerJson,
                _ when value.Equals(ContentFormat.SwaggerLinkJson.ToString(), StringComparison.OrdinalIgnoreCase) => ContentFormat.SwaggerLinkJson,
                _ when value.Equals(ContentFormat.Wsdl.ToString(), StringComparison.OrdinalIgnoreCase) => ContentFormat.Wsdl,
                _ when value.Equals(ContentFormat.WsdlLink.ToString(), StringComparison.OrdinalIgnoreCase) => ContentFormat.WsdlLink,
                _ when value.Equals(ContentFormat.OpenApi.ToString(), StringComparison.OrdinalIgnoreCase) => ContentFormat.OpenApi,
                _ when value.Equals(ContentFormat.OpenApiJson.ToString(), StringComparison.OrdinalIgnoreCase) => ContentFormat.OpenApiJson,
                _ when value.Equals(ContentFormat.OpenApiLink.ToString(), StringComparison.OrdinalIgnoreCase) => ContentFormat.OpenApiLink,
                _ when value.Equals(ContentFormat.OpenApiJsonLink.ToString(), StringComparison.OrdinalIgnoreCase) => ContentFormat.OpenApiJsonLink,
                _ when value.Equals(ContentFormat.GraphQLLink.ToString(), StringComparison.OrdinalIgnoreCase) => ContentFormat.GraphQLLink,
                _ => throw new JsonException($"'{value}' is not a valid content format.")
            }
            : throw new JsonException("Node must be a string JSON value.");

    public static JsonObject Serialize(this ApiLicenseInformation licenseInformation) =>
        new JsonObject()
            .AddPropertyIfNotNull("name", licenseInformation.Name)
            .AddPropertyIfNotNull("url", licenseInformation.Uri.ToString());

    public static ApiLicenseInformation DeserializeApiLicenseInformation(JsonObject jsonObject) =>
        new()
        {
            Name = jsonObject.TryGetStringProperty("name")
                             .IfLeftNull(),
            Uri = jsonObject.TryGetUriProperty("url")
                            .IfLeftNull()
        };

    public static JsonNode Serialize(ApiOperationInvokableProtocol protocol) =>
        JsonValue.Create(protocol.ToString()) ?? throw new JsonException("Value cannot be null.");

    public static ApiOperationInvokableProtocol DeserializeApiOperationInvokableProtocol(JsonNode node) =>
        node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var value)
            ? value switch
            {
                _ when value.Equals(ApiOperationInvokableProtocol.Http.ToString(), StringComparison.OrdinalIgnoreCase) => ApiOperationInvokableProtocol.Http,
                _ when value.Equals(ApiOperationInvokableProtocol.Https.ToString(), StringComparison.OrdinalIgnoreCase) => ApiOperationInvokableProtocol.Https,
                _ when value.Equals(ApiOperationInvokableProtocol.Ws.ToString(), StringComparison.OrdinalIgnoreCase) => ApiOperationInvokableProtocol.Ws,
                _ when value.Equals(ApiOperationInvokableProtocol.Wss.ToString(), StringComparison.OrdinalIgnoreCase) => ApiOperationInvokableProtocol.Wss,
                _ => throw new JsonException($"'{value}' is not a valid protocol.")
            }
            : throw new JsonException("Node must be a string JSON value.");

    public static JsonObject Serialize(this ApiCreateOrUpdatePropertiesWsdlSelector selector) =>
        new JsonObject()
            .AddPropertyIfNotNull("wsdlEndpointName", selector.WsdlEndpointName)
            .AddPropertyIfNotNull("wsdlServiceName", selector.WsdlServiceName);

    public static ApiCreateOrUpdatePropertiesWsdlSelector DeserializeApiCreateOrUpdatePropertiesWsdlSelector(JsonObject jsonObject) =>
        new()
        {
            WsdlEndpointName = jsonObject.TryGetStringProperty("wsdlEndpointName")
                                         .IfLeftNull(),
            WsdlServiceName = jsonObject.TryGetStringProperty("wsdlServiceName")
                                        .IfLeftNull()
        };

    public static JsonObject Serialize(this SubscriptionKeyParameterNamesContract contract) =>
        new JsonObject()
            .AddPropertyIfNotNull("header", contract.Header)
            .AddPropertyIfNotNull("query", contract.Query);

    public static SubscriptionKeyParameterNamesContract DeserializeSubscriptionKeyParameterNamesContract(JsonObject jsonObject) =>
        new()
        {
            Header = jsonObject.TryGetStringProperty("header")
                               .IfLeftNull(),
            Query = jsonObject.TryGetStringProperty("query")
                              .IfLeftNull()
        };
}

public static class ApiDataExtensions
{
    public static ApiCreateOrUpdateContent ToCreateOrUpdateContent(this ApiData data)
    {
        var content = new ApiCreateOrUpdateContent
        {
            ApiRevision = data.ApiRevision,
            ApiRevisionDescription = data.ApiRevisionDescription,
            ApiType = data.ApiType,
            ApiVersion = data.ApiVersion,
            ApiVersionDescription = data.ApiVersionDescription,
            ApiVersionSet = data.ApiVersionSet,
            ApiVersionSetId = data.ApiVersionSetId,
            AuthenticationSettings = data.AuthenticationSettings,
            Contact = data.Contact,
            Description = data.Description,
            DisplayName = data.DisplayName,
            IsCurrent = data.IsCurrent,
            IsSubscriptionRequired = data.IsSubscriptionRequired,
            License = data.License,
            Path = data.Path,
            ServiceUri = data.ServiceUri,
            SourceApiId = data.SourceApiId,
            SubscriptionKeyParameterNames = data.SubscriptionKeyParameterNames,
            TermsOfServiceUri = data.TermsOfServiceUri
        };

        data.Protocols.Iter(content.Protocols.Add);

        return content;
    }
}