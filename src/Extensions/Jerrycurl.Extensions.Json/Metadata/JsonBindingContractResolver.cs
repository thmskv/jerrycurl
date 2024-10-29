using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Metadata.Annotations;
using Jerrycurl.Reflection;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Extensions.Json.Metadata;

public class JsonBindingContractResolver : IBindingContractResolver
{
    public int Priority => 1;

    public JsonSerializerOptions Options { get; }

    private readonly JsonBindingHelperContract helper;

    public JsonBindingContractResolver(JsonSerializerOptions options)
    {
        this.Options = options ?? throw new ArgumentNullException(nameof(options));
        this.helper = new JsonBindingHelperContract(options);
    }

    private MethodInfo GetColumnReaderProxy(IBindingColumnInfo columnInfo)
    {
        if (columnInfo.Column.Type == typeof(string))
            return typeof(IDataRecord).GetMethod(nameof(IDataReader.GetString), [typeof(int)]);

        return null;
    }

    private Expression GetValueReaderProxy(IBindingValueInfo valueInfo)
    {
        Expression value = valueInfo.Value;

        if (value.Type != typeof(object) && value.Type != typeof(string))
            throw BindingException.Create(valueInfo.Metadata, $"Cannot deserialize JSON from type '{value.Type.GetSanitizedName()}'.");

        Expression nullCheck = null;

        if (valueInfo.CanBeDbNull)
            nullCheck = Expression.TypeIs(value, typeof(DBNull));

        if (valueInfo.CanBeNull)
        {
            Expression isNull = Expression.ReferenceEqual(value, Expression.Constant(null, value.Type));

            nullCheck = nullCheck == null ? isNull : Expression.AndAlso(nullCheck, isNull);
        }

        if (value.Type == typeof(object))
            value = Expression.Convert(value, typeof(string));

        Expression targetValue = this.GetDeserializeExpression(valueInfo.Metadata, value, valueInfo.Helper);

        if (nullCheck != null)
            return Expression.Condition(nullCheck, Expression.Default(targetValue.Type), targetValue);

        return targetValue;
    }

    private Expression GetDeserializeExpression(IBindingMetadata metadata, Expression value, Expression helper)
    {
        MethodInfo deserializeMethod = typeof(JsonSerializer).GetMethod(nameof(JsonSerializer.Deserialize), [typeof(string), typeof(Type), typeof(JsonSerializerOptions)]);

        if (helper != null)
        {
            Expression methodCall = Expression.Call(deserializeMethod, value, Expression.Constant(metadata.Type), helper);

            return Expression.Convert(methodCall, metadata.Type);
        }
        else
        {
            Expression methodCall = Expression.Call(deserializeMethod, value, Expression.Constant(metadata.Type), Expression.Default(typeof(JsonSerializerOptions)));

            return Expression.Convert(methodCall, metadata.Type);
        }
    }

    private bool HasJsonAttribute(IBindingMetadata metadata)
    {
        if (metadata.Relation.Annotations.OfType<JsonAttribute>().Any())
            return true;

        if (metadata.Relation.HasFlag(RelationMetadataFlags.List) && metadata.Relation.Item.Annotations.OfType<JsonAttribute>().Any())
            return true;

        return false;
    }

    private bool IsNativeJsonNode(IBindingMetadata metadata) => (metadata.Type == typeof(JsonNode) || metadata.Type == typeof(JsonValue) || metadata.Type == typeof(JsonArray));

    public IBindingParameterContract GetParameterContract(IBindingMetadata metadata)
    {
        if (this.HasJsonAttribute(metadata) || this.IsNativeJsonNode(metadata))
        {
            return new BindingParameterContract()
            {
                Convert = o => o != null ? (object)JsonSerializer.Serialize(o, metadata.Type, this.Options) : DBNull.Value,
            };
        }

        return null;
    }

    public IBindingCompositionContract GetCompositionContract(IBindingMetadata metadata) => null;
    public IBindingValueContract GetValueContract(IBindingMetadata metadata)
    {
        if (!this.HasJsonAttribute(metadata) && !this.IsNativeJsonNode(metadata))
            return null;

        return new BindingValueContract()
        {
            Convert = this.GetValueReaderProxy,
            Read = this.GetColumnReaderProxy,
        };
    }

    public IBindingHelperContract GetHelperContract(IBindingMetadata metadata) => this.HasJsonAttribute(metadata) ? this.helper : null;
}
