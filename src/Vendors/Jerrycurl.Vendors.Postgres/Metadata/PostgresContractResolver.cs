using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Metadata.Annotations;
using Npgsql;
using NpgsqlTypes;
using Pgvector;

namespace Jerrycurl.Vendors.Postgres.Metadata;

public class PostgresContractResolver : IBindingContractResolver
{
    public int Priority => 1000;

    private MethodInfo GetNpgsqlReaderMethod(string methodName)
    {
        Type reader = typeof(NpgsqlDataReader);

        return reader.GetMethod(methodName, [typeof(int)]) ?? throw new InvalidOperationException("No such method.");
    }

    private MethodInfo GetValueReaderProxy(IBindingColumnInfo columnInfo, IBindingValueContract fallback)
    {
        Type columnType = columnInfo.Column.Type;
        Type targetType = Nullable.GetUnderlyingType(columnInfo.Metadata.Type) ?? columnInfo.Metadata.Type;

        if (columnType == typeof(TimeSpan))
            return this.GetNpgsqlReaderMethod(nameof(NpgsqlDataReader.GetTimeSpan));

        // Npgsql reports 'date' and 'time' columns as DateOnly/TimeOnly, which GetValue() returns boxed
        // and cannot be cast to DateTime/TimeSpan properties; Npgsql reads them natively as either type
        if (columnType == typeof(DateOnly) && targetType == typeof(DateTime))
            return this.GetNpgsqlReaderMethod(nameof(NpgsqlDataReader.GetDateTime));
        else if (columnType == typeof(TimeOnly) && targetType == typeof(TimeSpan))
            return this.GetNpgsqlReaderMethod(nameof(NpgsqlDataReader.GetTimeSpan));

        return fallback?.Read(columnInfo);
    }

    public IBindingParameterContract GetParameterContract(IBindingMetadata metadata)
    {
        IBindingParameterContract fallback = metadata.Parameter;

        if (metadata.Relation.Annotations.OfType<JsonAttribute>().Any())
        {
            return new BindingParameterContract()
            {
                Convert = fallback.Convert,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    if (pi.Parameter is NpgsqlParameter npgParam)
                        npgParam.NpgsqlDbType = NpgsqlDbType.Json;

                    this.SetInputParameter(pi);
                }
            };
        }
        else if (metadata.Type == typeof(XDocument))
        {
            return new BindingParameterContract()
            {
                Convert = fallback.Convert,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    if (pi.Parameter is NpgsqlParameter npgParam)
                        npgParam.NpgsqlDbType = NpgsqlDbType.Xml;

                    this.SetInputParameter(pi);
                }
            };
        }
        else if (metadata.Type == typeof(TimeSpan) || metadata.Type == typeof(TimeSpan?))
        {
            // DbType.Time maps to 'time' (time of day), which rejects negative or 24h+ values;
            // 'interval' covers the full TimeSpan range and casts implicitly to 'time' on assignment
            return new BindingParameterContract()
            {
                Convert = fallback.Convert,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    if (pi.Parameter is NpgsqlParameter npgParam)
                        npgParam.NpgsqlDbType = NpgsqlDbType.Interval;

                    this.SetInputParameter(pi);
                }
            };
        }
        else if (this.GetVectorTypeName(metadata.Type) is string vectorTypeName)
        {
            return new BindingParameterContract()
            {
                Convert = fallback.Convert,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    if (pi.Parameter is NpgsqlParameter npgParam)
                        npgParam.DataTypeName = vectorTypeName;

                    this.SetInputParameter(pi);
                }
            };
        }
        else if (metadata.Type == typeof(DateTime) || metadata.Type == typeof(DateTime?) || metadata.Type == typeof(DateTimeOffset) || metadata.Type == typeof(DateTimeOffset?))
        {
            return new BindingParameterContract()
            {
                Convert = fallback.Convert,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    if (pi.Parameter is NpgsqlParameter npgParam)
                    {
                        npgParam.NpgsqlDbType = pi.Parameter.Value switch
                        {
                            DateTime dt => dt.Kind == DateTimeKind.Utc ? NpgsqlDbType.TimestampTz : NpgsqlDbType.Timestamp,
                            DateTimeOffset dt => dt.Offset.Ticks == 0 ? NpgsqlDbType.TimestampTz : NpgsqlDbType.Timestamp,
                            _ => npgParam.NpgsqlDbType,
                        };
                    }

                    this.SetInputParameter(pi);
                }
            };
        }
        else
        {
            return new BindingParameterContract()
            {
                Convert = fallback.Convert,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    this.SetInputParameter(pi);
                }
            };
        }
    }

    private string GetVectorTypeName(Type type)
    {
        if (type == typeof(Vector))
            return "vector";
        else if (type == typeof(HalfVector))
            return "halfvec";
        else if (type == typeof(SparseVector))
            return "sparsevec";

        return null;
    }

    private void SetInputParameter(IBindingParameterInfo paramInfo)
    {
        if (paramInfo.Parameter.Direction == ParameterDirection.InputOutput || paramInfo.Parameter.Direction == ParameterDirection.Output)
            paramInfo.Parameter.Direction = ParameterDirection.Input;
    }

    public IBindingValueContract GetValueContract(IBindingMetadata metadata)
    {
        IBindingValueContract fallback = metadata.Value;

        return new BindingValueContract()
        {
            Convert = fallback.Convert,
            Read = ci => this.GetValueReaderProxy(ci, fallback),
        };
    }

    public IBindingCompositionContract GetCompositionContract(IBindingMetadata metadata) => null;
    public IBindingHelperContract GetHelperContract(IBindingMetadata metadata) => null;
}
