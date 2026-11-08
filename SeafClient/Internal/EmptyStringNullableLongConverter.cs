using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeafClient.Internal;

/// <summary>Reads a nullable long that the server may send as <c>""</c> (e.g. trash sizes of directories).</summary>
internal sealed class EmptyStringNullableLongConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null: return null;
            case JsonTokenType.Number: return reader.GetInt64();
            case JsonTokenType.String:
                var s = reader.GetString();
                return long.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value is { } v) writer.WriteNumberValue(v); else writer.WriteNullValue();
    }
}
