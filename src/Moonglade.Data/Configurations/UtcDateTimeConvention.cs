using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Moonglade.Data.Configurations;

public static class UtcDateTimeConvention
{
    private static readonly ValueConverter<DateTime, DateTime> DateTimeConverter = new(
        value => value,
        value => new DateTime(value.Ticks, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableDateTimeConverter = new(
        value => value,
        value => value.HasValue
            ? new DateTime(value.Value.Ticks, DateTimeKind.Utc)
            : value);

    public static void ConfigureUtcDateTimeContract(this ModelBuilder modelBuilder)
    {
        foreach (var property in GetUtcDateTimeProperties(modelBuilder.Model))
        {
            property.SetValueConverter(
                property.ClrType == typeof(DateTime)
                    ? DateTimeConverter
                    : NullableDateTimeConverter);
        }
    }

    public static void ConfigureUtcDateTimeColumnType(
        this ModelBuilder modelBuilder,
        string columnType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnType);

        foreach (var property in GetUtcDateTimeProperties(modelBuilder.Model))
        {
            property.SetColumnType(columnType);
        }
    }

    public static bool IsUtcDateTimeProperty(IReadOnlyProperty property)
    {
        var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        return clrType == typeof(DateTime) && property.Name.EndsWith("Utc", StringComparison.Ordinal);
    }

    private static IEnumerable<IMutableProperty> GetUtcDateTimeProperties(IMutableModel model) =>
        model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Where(IsUtcDateTimeProperty);
}
