using Azure;
using Azure.ResourceManager.ApiManagement;
using Azure.ResourceManager.ApiManagement.Models;
using common;
using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace extractor.integration.tests;

[TestFixture]
public class ApiInformationFileTests
{
    [FsCheck.NUnit.Property(MaxTest = 5)]
    public Property Api_information_files_are_extracted()
    {
        var generator = GenerateApiDetail().NonEmptySeqOf()
                                           .DistinctBy(x => x.Name.Value.ToUpper(CultureInfo.CurrentCulture))
                                           .DistinctBy(x => x.Content.DisplayName.ToUpper(CultureInfo.CurrentCulture));

        var arbitrary = generator.ToArbitrary();

        return Prop.ForAll(arbitrary, async apiDetails =>
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            await apiDetails.ForEachParallel(async detail => await CreateApi(detail, cancellationToken), cancellationToken);
            await Fixture.RunExtractor(Fixture.DefaultServiceProvider, cancellationToken);

            // Assert
            apiDetails.Iter(apiDetail =>
            {
                var apiInformationFile = GetApiInformationFile(apiDetail.Name);
                apiInformationFile.Exists().Should().BeTrue();

                var fileJson = apiInformationFile.ReadAsJsonObject();
                var fileContent = Serialization.DeserializeApiCreateOrUpdateContent(fileJson);
                fileContent.Should().BeEquivalentTo(apiDetail.Content,
                                                    // Protocol comparison is handled below
                                                    options => options.Excluding(content => content.Protocols));
                fileContent.Protocols.Should().BeEquivalentTo(apiDetail.Content.Protocols);
            });

            // Teardown
            await apiDetails.ForEachParallel(async apiDetail => await DeleteApi(apiDetail.Name, cancellationToken), cancellationToken);
        });
    }

    private static Gen<ApiDetail> GenerateApiDetail()
    {
        return from name in ApiGenerator.ApiName
               from content in ApiGenerator.ApiCreateOrUpdateContent
               select new ApiDetail
               {
                   Content = content,
                   Name = name
               };
    }

    private static async ValueTask CreateApi(ApiDetail apiDetail, CancellationToken cancellationToken)
    {
        var serviceResource = Fixture.DefaultServiceProvider
                                     .GetRequiredService<ApiManagementServiceResource>();

        await serviceResource.GetApis()
                             .CreateOrUpdateAsync(WaitUntil.Completed, apiDetail.Name.Value, apiDetail.Content, ETag.All, cancellationToken);
    }

    private static ApiInformationFile GetApiInformationFile(ApiName apiName)
    {
        var serviceDirectory = Fixture.DefaultServiceProvider
                                      .GetRequiredService<ServiceDirectory>();
        var apisDirectory = new ApisDirectory(serviceDirectory);
        var apiDirectory = new ApiDirectory(apiName, apisDirectory);

        return new ApiInformationFile(apiDirectory);
    }

    private static async ValueTask DeleteApi(ApiName apiName, CancellationToken cancellationToken)
    {
        var serviceResource = Fixture.DefaultServiceProvider
                                     .GetRequiredService<ApiManagementServiceResource>();

        var api = await serviceResource.GetApiAsync(apiName.Value, cancellationToken);

        await api.Value.DeleteAsync(WaitUntil.Started, ETag.All, deleteRevisions: true, cancellationToken);
    }

    private readonly record struct ApiDetail
    {
        public required ApiName Name { get; init; }
        public required ApiCreateOrUpdateContent Content { get; init; }

        public override string ToString()
        {
            return $"Name = {Name}, Content = {Content.Serialize().ToJsonString()}";
        }
    }
}

public static class ApiGenerator
{
    public static Gen<ApiName> ApiName { get; } =
        from name in Generator.AlphaNumericString
        select new ApiName(name);

    public static Gen<ApiType> ApiType { get; } =
        Gen.Elements(Azure.ResourceManager.ApiManagement.Models.ApiType.Http);

    public static Gen<ApiOperationInvokableProtocol> ApiOperationInvokableProtocol { get; } =
        Gen.Elements(Azure.ResourceManager.ApiManagement.Models.ApiOperationInvokableProtocol.Http,
                     Azure.ResourceManager.ApiManagement.Models.ApiOperationInvokableProtocol.Https);

    public static Gen<ApiCreateOrUpdateContent> ApiCreateOrUpdateContent { get; } =
        from randomizer in Generator.Randomizer
        from lorem in Generator.Lorem
        let description = lorem.Paragraph()
        let displayName = randomizer.Guid().ToString()
        let path = randomizer.Guid().ToString()
        from protocols in ApiOperationInvokableProtocol.NonEmptySeqOf()
                                                       .DistinctBy(x => x.ToString())
        select new ApiCreateOrUpdateContent
        {
            Description = description,
            DisplayName = displayName,
            IsCurrent = true,
            IsSubscriptionRequired = true,
            Path = path,
        }
        .WithRequiredDefaults()
        .WithProtocols(protocols);

    public static ApiCreateOrUpdateContent WithRequiredDefaults(this ApiCreateOrUpdateContent content)
    {
        content.ApiRevision ??= "1";
        content.AuthenticationSettings ??= new AuthenticationSettingsContract();
        content.SubscriptionKeyParameterNames ??= new SubscriptionKeyParameterNamesContract
        {
            Header = "Ocp-Apim-Subscription-Key",
            Query = "subscription-key"
        };
        content.IsSubscriptionRequired ??= true;

        return content;
    }

    private static ApiCreateOrUpdateContent WithProtocols(this ApiCreateOrUpdateContent content, IEnumerable<ApiOperationInvokableProtocol> protocols)
    {
        protocols.Iter(content.Protocols.Add);
        return content;
    }
}