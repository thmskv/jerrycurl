using System;
using Jerrycurl.Collections;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Language
{
    public static class SchemaExtensions
    {
        public static ISchemaStore Use(this ISchemaStore store, IRelationContractResolver resolver)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            RelationMetadataBuilder builder = store.Builders.FirstOfType<RelationMetadataBuilder>();

            builder?.Add(resolver);

            return store;
        }
    }
}
