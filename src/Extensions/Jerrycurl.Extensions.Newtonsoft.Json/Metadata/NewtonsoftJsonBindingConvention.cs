using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Metadata.Annotations;
using Jerrycurl.Reflection;
using Jerrycurl.Relations.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jerrycurl.Extensions.Newtonsoft.Json.Metadata;

public class NewtonsoftJsonBindingConvention : IBindingConvention
{
    /// <summary>
    /// Runs after the built-in System.Text.Json convention (100), so Newtonsoft owns [Json] properties
    /// regardless of registration order, and before vendor conventions (1000), which decorate its contracts.
    /// </summary>
    public int Priority => 200;

    public JsonSerializerSettings Settings { get; }

    private readonly NewtonsoftJsonBindingHelperContract helper;

    public NewtonsoftJsonBindingConvention(JsonSerializerSettings settings)
    {
        this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.helper = new NewtonsoftJsonBindingHelperContract(settings);
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

            nullCheck = nullCheck == null ? isNull : Expression.OrElse(nullCheck, isNull);
        }

        if (value.Type == typeof(object))
            value = Expression.Convert(value, typeof(string));

        Expression targetValue;

        if (IsNativeJToken(valueInfo.Metadata.Type))
            targetValue = this.GetParseJTokenExpression(valueInfo.Metadata, value);
        else
            targetValue = this.GetDeserializeExpression(valueInfo.Metadata, value, valueInfo.Helper);

        if (nullCheck != null)
            return Expression.Condition(nullCheck, Expression.Default(targetValue.Type), targetValue);

        return targetValue;
    }

    private Expression GetParseJTokenExpression(IBindingMetadata metadata, Expression json)
    {
        MethodInfo parseMethod = typeof(JToken).GetMethod(nameof(JToken.Parse), [typeof(string)]);

        return Expression.Convert(Expression.Call(parseMethod, json), metadata.Type);
    }

    private Expression GetDeserializeExpression(IBindingMetadata metadata, Expression json, Expression helper)
    {
        MethodInfo deserializeMethod = typeof(JsonConvert).GetMethod(nameof(JsonConvert.DeserializeObject), [typeof(string), typeof(Type), typeof(JsonSerializerSettings)]);

        Expression settings = helper ?? Expression.Default(typeof(JsonSerializerSettings));
        Expression methodCall = Expression.Call(deserializeMethod, json, Expression.Constant(metadata.Type), settings);

        return Expression.Convert(methodCall, metadata.Type);
    }

    private static bool HasJsonAttribute(IBindingMetadata metadata)
    {
        if (metadata.Relation.Annotations.OfType<JsonAttribute>().Any())
            return true;

        if (metadata.Relation.HasFlag(RelationMetadataFlags.List) && metadata.Relation.Item.Annotations.OfType<JsonAttribute>().Any())
            return true;

        return false;
    }

    private static bool IsNativeJToken(Type type) => typeof(JToken).IsAssignableFrom(type);

    // System.Text.Json nodes are left to the built-in convention, even when marked [Json]
    private static bool IsForeignNative(Type type) => typeof(JsonNode).IsAssignableFrom(type);

    private static bool IsJsonObject(IBindingMetadata metadata) => HasJsonAttribute(metadata) && !IsNativeJToken(metadata.Type) && !IsForeignNative(metadata.Type);

    public IBindingParameterContract GetParameterContract(IBindingMetadata metadata)
    {
        if (IsNativeJToken(metadata.Type))
        {
            return new BindingParameterContract()
            {
                Convert = o => (object)((JToken)o)?.ToString(Formatting.Indented) ?? DBNull.Value,
            };
        }
        else if (IsJsonObject(metadata))
        {
            return new BindingParameterContract()
            {
                Convert = o => o != null ? (object)JsonConvert.SerializeObject(o, this.Settings) : DBNull.Value,
            };
        }

        return null;
    }

    public IBindingCompositionContract GetCompositionContract(IBindingMetadata metadata) => null;

    public IBindingValueContract GetValueContract(IBindingMetadata metadata)
    {
        if (!IsNativeJToken(metadata.Type) && !IsJsonObject(metadata))
            return null;

        return new BindingValueContract()
        {
            Convert = this.GetValueReaderProxy,
            Read = this.GetColumnReaderProxy,
        };
    }

    public IBindingHelperContract GetHelperContract(IBindingMetadata metadata) => IsJsonObject(metadata) ? this.helper : null;
}
