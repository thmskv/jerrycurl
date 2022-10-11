using Jerrycurl.Collections;
using Jerrycurl.Cqs.Language;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Metadata;
using System;

namespace Jerrycurl.Mvc
{
    public static class DomainExtensions
    {
        public static void Use(this IDomainOptions options, ITableContractResolver resolver)
            => options.Schemas?.Use(resolver);

        public static void Use(this IDomainOptions options, IBindingContractResolver resolver)
            => options.Schemas?.Use(resolver);

        public static void Use(this IDomainOptions options, IJsonContractResolver resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            JsonMetadataBuilder builder = options?.Schemas.Builders.FirstOfType<JsonMetadataBuilder>();

            builder?.Add(resolver);
        }

        public static void Use(this IDomainOptions options, IRelationContractResolver resolver)
            => options.Schemas?.Use(resolver);
    }
}
