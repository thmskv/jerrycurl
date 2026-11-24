using Jerrycurl.Collections;
using Jerrycurl.Cqs.Language;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Razor.Metadata;
using Jerrycurl.Cqs.Metadata.Json;
using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Metadata;
using System;
using System.Linq;
using System.Text.Json;

namespace Jerrycurl.Razor;

public static class DomainExtensions
{
    extension(IDomainOptions options)
    {
        public void Use(ITableNamingPolicy policy)
            => options.Schemas?.Use(policy);

        public void Use(IBindingConvention convention)
            => options.Schemas?.Use(convention);

        public void Use(IJsonNamingPolicy policy)
            => options.Schemas?.Use(policy);

        public void Use(IRelationConvention convention)
            => options.Schemas?.Use(convention);

        public void UseJson() => options.UseJson(null);

        public void UseJson(JsonSerializerOptions serializerOptions)
        {
            serializerOptions ??= JsonSerializerOptions.Default;

            foreach (BindingMetadataBuilder builder in options?.Schemas?.Builders.OfType<BindingMetadataBuilder>() ?? [])
                builder.Conventions.Remove<JsonBindingConvention>();

            options.Use(new JsonBindingConvention(serializerOptions));

            // keep a naming policy set by another serializer (e.g. UseNewtonsoftJson), which owns [Json] properties
            JsonMetadataBuilder jsonBuilder = options?.Schemas?.Builders.FirstOfType<JsonMetadataBuilder>();

            if (jsonBuilder != null && (jsonBuilder.NamingPolicy == null || jsonBuilder.NamingPolicy is DefaultJsonNamingPolicy))
                jsonBuilder.NamingPolicy = new DefaultJsonNamingPolicy(serializerOptions);
        }
    }
}
