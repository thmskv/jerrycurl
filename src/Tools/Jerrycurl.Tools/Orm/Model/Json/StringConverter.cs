using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jerrycurl.Tools.Orm.Model.Json
{
    internal class StringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                return null;

            string value = reader.GetString();

            if (value != null && value.Trim().Length == 0)
                value = null;

            return value;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => throw new NotSupportedException();
    }
}
