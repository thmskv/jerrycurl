using Jerrycurl.Collections;
using Jerrycurl.Cqs.Language;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Metadata;
using System;

namespace Jerrycurl.Mvc;

public static class DomainExtensions
{
    public static void Use(this IDomainOptions options, ITableNamingPolicy policy)
        => options.Schemas?.Use(policy);

    public static void Use(this IDomainOptions options, IBindingConvention convention)
        => options.Schemas?.Use(convention);

    public static void Use(this IDomainOptions options, IJsonNamingPolicy policy)
    {
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));

        JsonMetadataBuilder builder = options?.Schemas.Builders.FirstOfType<JsonMetadataBuilder>();

        if (builder != null)
            builder.NamingPolicy = policy;
    }

    public static void Use(this IDomainOptions options, IRelationConvention convention)
        => options.Schemas?.Use(convention);
}
