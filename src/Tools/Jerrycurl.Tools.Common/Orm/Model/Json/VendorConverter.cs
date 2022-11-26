using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jerrycurl.Facts;
using Jerrycurl.Tools.Orm.Model;

namespace Jerrycurl.Tools.Orm.Model.Json
{
    internal class VendorConverter : JsonConverter<OrmModel.VendorOptions>
    {
        public override OrmModel.VendorOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            OrmModel.VendorOptions vendorOptions;

            if (reader.TokenType == JsonTokenType.String)
                vendorOptions = new OrmModel.VendorOptions() { Package = reader.GetString() };
            else
                vendorOptions = JsonSerializer.Deserialize<OrmModel.VendorOptions>(ref reader);

            if (vendorOptions != null)
                vendorOptions.Package = DatabaseFacts.GetToolsNuGetPackage(vendorOptions.Package) ?? vendorOptions.Package;

            return vendorOptions;
        }

        public override void Write(Utf8JsonWriter writer, OrmModel.VendorOptions value, JsonSerializerOptions options) => throw new NotSupportedException();
    }
}
