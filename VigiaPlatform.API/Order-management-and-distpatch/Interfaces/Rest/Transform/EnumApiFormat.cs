using System.Text;

namespace VigiaPlatform.API.Order_management_and_distpatch.Interfaces.Rest.Transform;

/// <summary>
///     Converts enums to and from the API format (UPPER_SNAKE_CASE, e.g. "IN_PREPARATION").
/// </summary>
internal static class EnumApiFormat
{
    public static string ToApi<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var name = value.ToString();
        var builder = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                builder.Append('_');
            builder.Append(char.ToUpperInvariant(name[i]));
        }

        return builder.ToString();
    }

    public static TEnum FromApi<TEnum>(string? value, string fieldName) where TEnum : struct, Enum
    {
        var normalized = (value ?? string.Empty).Trim().Replace("_", string.Empty);

        if (Enum.TryParse<TEnum>(normalized, true, out var result) && Enum.IsDefined(result))
            return result;

        throw new ArgumentException($"Invalid value '{value}' for {fieldName}", fieldName);
    }
}
