using System;
using System.Data;
using System.Reflection;
using System.Text.Json.Nodes;
using Jerrycurl.Cqs.Metadata;
using Microsoft.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;

namespace Jerrycurl.Vendors.SqlServer.Metadata;

public class SqlServerContractResolver : IBindingContractResolver
{
    public const int DefaultStringSize = 4000;

    public int Priority => 1000;
    public IBindingParameterContract GetParameterContract(IBindingMetadata metadata)
    {
        if (metadata.Type == typeof(DateTime) || metadata.Type == typeof(DateTime?))
        {
            IBindingParameterContract fallback = metadata.Parameter;

            return new BindingParameterContract()
            {
                Convert = fallback.Convert,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    pi.Parameter.DbType = DbType.DateTime2;
                }
            };
        }
        else if (metadata.Type == typeof(string))
        {
            IBindingParameterContract fallback = metadata.Parameter;

            return new BindingParameterContract()
            {
                Convert = fallback.Convert,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    if (pi.Parameter.Size < DefaultStringSize)
                        pi.Parameter.Size = DefaultStringSize;
                }
            };
        }
        else if (this.IsVectorType(metadata.Type))
        {
            IBindingParameterContract fallback = metadata.Parameter;

            return new BindingParameterContract()
            {
                Convert = fallback.Convert,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    // null vectors are sent untyped, as SQL Server requires a typed null vector to carry its dimensions
                    // (see SqlVector<T>.CreateNull); an untyped NULL converts implicitly to any vector column
                    if (pi.Parameter is SqlParameter sqlParam && sqlParam.Value is not DBNull and not null)
                        sqlParam.SqlDbType = SqlDbTypeExtensions.Vector;
                }
            };
        }
        else if (this.IsJsonType(metadata.Type))
        {
            IBindingParameterContract fallback = metadata.Parameter;

            // JSON is sent as nvarchar(max), which converts implicitly to the 'json' type in SQL Server 2025
            // while remaining compatible with JSON stored in nvarchar columns on earlier versions
            return new BindingParameterContract()
            {
                Convert = fallback.Convert,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    pi.Parameter.Size = -1;
                }
            };
        }
        else if (metadata.Type.FullName == "Microsoft.SqlServer.Types.SqlGeography")
        {
            IBindingParameterContract fallback = metadata.Parameter;

            return new BindingParameterContract()
            {
                Convert = fallback.Convert,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    if (pi.Parameter is SqlParameter sqlParam)
                        sqlParam.UdtTypeName = "geography";
                }
            };
        }

        return null;
    }

    public IBindingValueContract GetValueContract(IBindingMetadata metadata)
    {
        IBindingValueContract fallback = metadata.Value;

        return new BindingValueContract()
        {
            Convert = fallback.Convert,
            Read = ci => this.GetValueReadMethod(ci, fallback),
        };
    }

    public IBindingCompositionContract GetCompositionContract(IBindingMetadata metadata) => null;
    public IBindingHelperContract GetHelperContract(IBindingMetadata metadata) => null;

    private MethodInfo GetSqlReaderMethod(string methodName)
    {
        Type reader = typeof(SqlDataReader);

        return reader.GetMethod(methodName, [typeof(int)]);
    }

    private bool IsVectorType(Type type) => (this.GetVectorElementType(type) != null);
    private Type GetVectorElementType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SqlVector<>))
            return type.GetGenericArguments()[0];

        return null;
    }

    private bool IsJsonType(Type type) => typeof(JsonNode).IsAssignableFrom(type);

    private MethodInfo GetValueReadMethod(IBindingColumnInfo columnInfo, IBindingValueContract fallback)
    {
        // vector columns are read natively regardless of the reported field type; the element type comes from the property
        if (this.GetVectorElementType(columnInfo.Metadata.Type) is Type elementType)
            return typeof(SqlDataReader).GetMethod(nameof(SqlDataReader.GetSqlVector)).MakeGenericMethod(elementType);

        if (columnInfo.Column.Type == typeof(DateTimeOffset))
            return this.GetSqlReaderMethod("GetDateTimeOffset");
        else if (columnInfo.Column.Type == typeof(TimeSpan))
            return this.GetSqlReaderMethod("GetTimeSpan");

        return fallback?.Read(columnInfo);
    }
}
