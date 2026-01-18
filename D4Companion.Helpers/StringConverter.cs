using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace D4Companion.Helpers
{
    public class StringConverter : JsonConverter<string>
    {
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value);

        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                JsonTokenType.String => reader.GetString() ?? string.Empty,
                JsonTokenType.Number => reader.GetInt32().ToString() ?? string.Empty,
                JsonTokenType.Null => string.Empty,
                _ => throw new JsonException(),
            };
    }
}
