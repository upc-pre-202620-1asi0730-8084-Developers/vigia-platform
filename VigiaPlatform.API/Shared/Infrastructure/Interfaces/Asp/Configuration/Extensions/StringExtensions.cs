namespace VigiaPlatform.API.Shared.Infrastructure.Interfaces.Asp.Configuration.Extensions;
using System.Text.RegularExpressions;

public static partial class StringExtensions
{
    [GeneratedRegex("^[a-z]+(-[a-z]+)*$", RegexOptions.Compiled)]
    private static partial Regex KebabCaseRegex();

    public static string ToKebabCase(this string text)
    {
        return string.IsNullOrEmpty(text) 
            ? text 
            : KebabCaseRegex().Replace(text, "-$1").Trim().ToLower();
    }
    
}