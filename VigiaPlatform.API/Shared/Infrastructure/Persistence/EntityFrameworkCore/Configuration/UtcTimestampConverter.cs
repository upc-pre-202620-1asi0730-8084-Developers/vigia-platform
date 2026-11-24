namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// MySQL binds DateTimeOffset without fractional seconds; use a UTC DateTime provider value.
public sealed class UtcTimestampConverter() : ValueConverter<DateTimeOffset,DateTime>(
    v => v.UtcDateTime, v => new DateTimeOffset(DateTime.SpecifyKind(v,DateTimeKind.Utc)));
