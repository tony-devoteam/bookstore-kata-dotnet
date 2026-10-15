using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Bookstore.Api.Serialization;

public sealed class OffsetRequiredDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private static readonly Regex ExplicitOffsetPattern = new(
        @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Date-time values must be ISO 8601 strings with an explicit offset.");

        var value = reader.GetString();
        if (value is null || !ExplicitOffsetPattern.IsMatch(value) ||
            !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
        {
            throw new JsonException("Date-time values must include a time and an explicit UTC offset.");
        }

        return date;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
