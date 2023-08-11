using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace D4Companion.Helpers
{
    public class IntConverter : JsonConverter<int>
    {
        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value);

        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.True => 1,
                JsonTokenType.False => 0,
                JsonTokenType.String => int.TryParse(reader.GetString(), out var i) ? i : 0,
                JsonTokenType.Number => reader.GetInt32(),
                JsonTokenType.Null => 0,
                _ => throw new JsonException(),
            };
    }
}
