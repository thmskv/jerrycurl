using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Metadata.Json;
using Shouldly;

namespace Jerrycurl.Mvc.Test;

public class JsonTests
{
    public void Test_DefaultOptions_UseSystemTextJson()
    {
        var options = new DefaultOptionsEngine().GetDefaults();

        GetBindingConventions(options).ShouldHaveSingleItem().Options.ShouldBeSameAs(JsonSerializerOptions.Default);
        GetNamingPolicy(options).ShouldBeOfType<DefaultJsonNamingPolicy>().Options.ShouldBeSameAs(JsonSerializerOptions.Default);
    }

    public void Test_UseJson_ReplacesDefaultOptions()
    {
        var options = new DefaultOptionsEngine().GetDefaults();
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        options.UseJson(serializerOptions);

        GetBindingConventions(options).ShouldHaveSingleItem().Options.ShouldBeSameAs(serializerOptions);
        GetNamingPolicy(options).ShouldBeOfType<DefaultJsonNamingPolicy>().Options.ShouldBeSameAs(serializerOptions);
    }

    public void Test_UseJson_KeepsForeignNamingPolicy()
    {
        var options = new DefaultOptionsEngine().GetDefaults();
        var policy = new StubNamingPolicy();
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        options.Use(policy);
        options.UseJson(serializerOptions);

        GetBindingConventions(options).ShouldHaveSingleItem().Options.ShouldBeSameAs(serializerOptions);
        GetNamingPolicy(options).ShouldBeSameAs(policy);
    }

    private static List<JsonBindingConvention> GetBindingConventions(DomainOptions options)
        => options.Schemas.Builders.OfType<BindingMetadataBuilder>().SelectMany(b => b.Conventions.OfType<JsonBindingConvention>()).ToList();

    private static IJsonNamingPolicy GetNamingPolicy(DomainOptions options)
        => options.Schemas.Builders.OfType<JsonMetadataBuilder>().Single().NamingPolicy;

    private class StubNamingPolicy : IJsonNamingPolicy
    {
        public string GetPropertyName(IJsonMetadata metadata) => null;
    }

    private class DefaultOptionsEngine : ProcEngine
    {
        public DefaultOptionsEngine()
            : base(null)
        {
        }

        public DomainOptions GetDefaults() => this.GetDefaultOptions();
    }
}
