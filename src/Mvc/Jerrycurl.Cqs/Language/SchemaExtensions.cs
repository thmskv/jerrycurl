using System;
using Jerrycurl.Collections;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Language;

public static class SchemaExtensions
{
    public static ISchemaStore Use(this ISchemaStore store, IBindingConvention convention)
    {
        if (store == null)
            throw new ArgumentNullException(nameof(store));

        if (convention == null)
            throw new ArgumentNullException(nameof(convention));

        BindingMetadataBuilder builder = store.Builders.FirstOfType<BindingMetadataBuilder>();

        builder?.Add(convention);

        return store;
    }

    public static ISchemaStore Use(this ISchemaStore store, ITableNamingPolicy policy)
    {
        if (store == null)
            throw new ArgumentNullException(nameof(store));

        if (policy == null)
            throw new ArgumentNullException(nameof(policy));

        TableMetadataBuilder builder = store.Builders.FirstOfType<TableMetadataBuilder>();

        if (builder != null)
            builder.NamingPolicy = policy;

        return store;
    }
}
