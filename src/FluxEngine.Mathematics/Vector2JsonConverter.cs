using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluxEngine.Mathematics;

public sealed class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a Vector2 object.");

        var x = 0f;
        var y = 0f;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected a Vector2 coordinate name.");

            var name = reader.GetString();
            if (!reader.Read())
                throw new JsonException("Unexpected end of Vector2 object.");

            if (string.Equals(name, "x", StringComparison.OrdinalIgnoreCase))
                x = reader.GetSingle();
            else if (string.Equals(name, "y", StringComparison.OrdinalIgnoreCase))
                y = reader.GetSingle();
            else
                reader.Skip();
        }

        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}
