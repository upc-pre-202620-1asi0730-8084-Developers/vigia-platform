namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.AttachedFiles;

public record AttachedFilename
{
    public string Value { get; }

    public const int MaxLength = 255;

    public AttachedFilename(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var normalized = value.Trim();

        if (normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("The filename contains invalid characters.", nameof(value));

        if (normalized.Length > MaxLength)
            throw new ArgumentException($"Filename cannot exceed {MaxLength} characters.", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;
}
