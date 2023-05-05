using LanguageExt;
using Microsoft.Extensions.Configuration;

namespace common;

public static class ConfigurationExtensions
{
    public static string GetValue(this IConfiguration configuration, string key) =>
        configuration.TryGetValue(key)
                     .IfNoneThrow($"Could not find '{key}' in configuration.");

    public static Option<string> TryGetValue(this IConfiguration configuration, string key) =>
        configuration.TryGetSection(key)
                     .Bind(section => Prelude.Optional(section.Value));

    public static Option<IConfigurationSection> TryGetSection(this IConfiguration configuration, string key)
    {
        var section = configuration.GetSection(key);

        return section.Exists()
                ? Option<IConfigurationSection>.Some(section)
                : Option<IConfigurationSection>.None;
    }
}
