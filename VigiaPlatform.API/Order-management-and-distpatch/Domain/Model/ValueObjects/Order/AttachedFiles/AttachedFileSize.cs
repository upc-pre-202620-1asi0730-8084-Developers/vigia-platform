using System.Globalization;

namespace VigiaPlatform.API.Order_management_and_distpatch.Domain.Model.ValueObjects.Order.AttachedFiles;

/// <summary>Tamaño del archivo en bytes; se muestra como "320 KB" o "1.2 MB".</summary>
public record AttachedFileSize
{
    public long Value { get; }

    public AttachedFileSize(long value)
    {
        if (value < 0)
            throw new ArgumentException("File size cannot be negative.", nameof(value));

        Value = value;
    }

    public override string ToString()
    {
        const double kb = 1024d;
        const double mb = kb * 1024d;
        const double gb = mb * 1024d;
        var inv = CultureInfo.InvariantCulture;

        return Value switch
        {
            >= (long)gb => $"{(Value / gb).ToString("0.#", inv)} GB",
            >= (long)mb => $"{(Value / mb).ToString("0.#", inv)} MB",
            >= (long)kb => $"{(Value / kb).ToString("0", inv)} KB",
            _ => $"{Value} B"
        };
    }
}
