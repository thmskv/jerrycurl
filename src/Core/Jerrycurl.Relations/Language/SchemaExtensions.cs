using System;
using Jerrycurl.Collections;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Language;

public static class SchemaExtensions
{
    public static ISchemaStore Use(this ISchemaStore store, IRelationConvention convention)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(convention);

        RelationMetadataBuilder builder = store.Builders.FirstOfType<RelationMetadataBuilder>();

        builder?.Conventions.Add(convention);

        return store;
    }
}
