using System;
using System.Text.Json;
using Jerrycurl.Collections;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Metadata.Json;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Language;

public static class SchemaExtensions
{
    extension(ISchemaStore store)
    {
        public ISchemaStore Use(IBindingConvention convention)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(convention);

            BindingMetadataBuilder builder = store.Builders.FirstOfType<BindingMetadataBuilder>();

            builder?.Conventions.Add(convention);

            return store;
        }

        public void Use(IJsonNamingPolicy policy)
        {
            ArgumentNullException.ThrowIfNull(policy);

            JsonMetadataBuilder builder = store.Builders.FirstOfType<JsonMetadataBuilder>();

            builder?.NamingPolicy = policy;
        }

        public ISchemaStore Use(ITableNamingPolicy policy)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(policy);

            TableMetadataBuilder builder = store.Builders.FirstOfType<TableMetadataBuilder>();

            builder?.NamingPolicy = policy;

            return store;
        }
    }

    extension(SchemaStore store)
    {
        public static SchemaStore Default => DefaultSchemaStore.Value;
    }

    private static Lazy<SchemaStore> DefaultSchemaStore { get;} = new(() =>
    {
        return new SchemaStore(new DotNotation(),
        [
            new BindingMetadataBuilder()
            {
                Conventions = { new JsonBindingConvention(JsonSerializerOptions.Default) },
            },
            new ReferenceMetadataBuilder(),
            new TableMetadataBuilder(),
            new JsonMetadataBuilder()
            {
                NamingPolicy = new DefaultJsonNamingPolicy(JsonSerializerOptions.Default)
            }
        ]);
    });
}
