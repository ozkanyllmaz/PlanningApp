using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PlanningApp.Persistence.Conventions;

// Yazarken UTC'ye çevirir; offset bilgisi saklanmaz, okunan değerin offset'i 0'dır
public sealed class UtcDateTimeOffsetConverter()
    : ValueConverter<DateTimeOffset, DateTimeOffset>(
        value => value.ToUniversalTime(),
        value => value);

public sealed class NullableUtcDateTimeOffsetConverter()
    : ValueConverter<DateTimeOffset?, DateTimeOffset?>(
        value => value.HasValue ? value.Value.ToUniversalTime() : value,
        value => value);
