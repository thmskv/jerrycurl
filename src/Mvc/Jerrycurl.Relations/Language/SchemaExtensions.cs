using System;
using Jerrycurl.Collections;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Language;

public static class SchemaExtensions
{
    public static ISchemaStore Use(this ISchemaStore store, IRelationConvention convention)
    {
        if (store == null)
            throw new ArgumentNullException(nameof(store));

        if (convention == null)
            throw new ArgumentNullException(nameof(convention));

        RelationMetadataBuilder builder = store.Builders.FirstOfType<RelationMetadataBuilder>();

        builder?.Add(convention);

        return store;
    }
}
