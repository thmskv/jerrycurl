using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jerrycurl.Tools.Orm.Model.Json
{
    internal class TupleConverter : JsonConverter<TupleModel>
    {
        public override TupleModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();
        public override void Write(Utf8JsonWriter writer, TupleModel value, JsonSerializerOptions options)
        {

        }
    }
}
