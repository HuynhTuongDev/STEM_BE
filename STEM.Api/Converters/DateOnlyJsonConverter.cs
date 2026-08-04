using System.Text.Json;
using System.Text.Json.Serialization;

namespace STEM.Api.Converters;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
                return default;

            if (DateOnly.TryParse(value, out var date))
                return date;

            if (DateOnly.TryParseExact(value, DateFormat, out var dateExact))
                return dateExact;
        }

        // Handle missing field - return default
        if (reader.TokenType == JsonTokenType.PropertyName)
            return default;

        throw new JsonException($"Unable to parse \"{reader.GetString()}\" as DateOnly.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(DateFormat));
    }
}

public class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.PropertyName)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (DateOnly.TryParse(value, out var date))
                return date;

            if (DateOnly.TryParseExact(value, DateFormat, out var dateExact))
                return dateExact;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString(DateFormat));
        else
            writer.WriteNullValue();
    }
}
