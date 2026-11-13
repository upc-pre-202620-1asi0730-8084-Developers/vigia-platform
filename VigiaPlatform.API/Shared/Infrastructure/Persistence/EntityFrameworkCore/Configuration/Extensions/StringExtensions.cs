namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Humanizer;
using System.Text.RegularExpressions;

public static class StringExtensions
{
    public static string ToSnakeCase(this string text)
    {
        var separatedAcronyms = Regex.Replace(text, "([A-Z]+)([A-Z][a-z])", "$1_$2");
        return Regex.Replace(separatedAcronyms, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
    }

    public static string ToPlural(this string text)
    {
        return text.Pluralize(false);
    }
}
