using System;
using System.Reflection;
using System.Text.Json.Nodes;
using Jerrycurl.Cqs.Metadata;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Jerrycurl.Vendors.Oracle.Metadata;

public class OracleContractResolver : IBindingContractResolver
{
    public int Priority => 1000;

    private MethodInfo GetOracleReaderMethod(string methodName)
    {
        Type reader = typeof(OracleDataReader);

        return reader.GetMethod(methodName, [typeof(int)]) ?? throw new InvalidOperationException("No such method.");
    }

    private MethodInfo GetValueReaderProxy(IBindingColumnInfo columnInfo, IBindingValueContract fallback)
    {
        if (columnInfo.Metadata.Type == typeof(OracleVector))
            return this.GetOracleReaderMethod(nameof(OracleDataReader.GetOracleVector));
        else if (columnInfo.Column.Type == typeof(TimeSpan))
            return this.GetOracleReaderMethod(nameof(OracleDataReader.GetTimeSpan));
        else if (columnInfo.Column.Type == typeof(DateTimeOffset) || columnInfo.Column.TypeName == "TimeStampTZ")
            return this.GetOracleReaderMethod(nameof(OracleDataReader.GetDateTimeOffset));

        return fallback?.Read(columnInfo);
    }

    public IBindingParameterContract GetParameterContract(IBindingMetadata metadata)
    {
        IBindingParameterContract fallback = metadata.Parameter;

        if (metadata.Type == typeof(TimeSpan) || metadata.Type == typeof(TimeSpan?))
        {
            // DbType.Time maps to OracleDbType.TimeStamp, which cannot bind TimeSpan values ("ORA-50028: Invalid parameter binding")
            return new BindingParameterContract()
            {
                Convert = fallback.Convert,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    if (pi.Parameter is OracleParameter oraParam)
                        oraParam.OracleDbType = OracleDbType.IntervalDS;
                }
            };
        }
        else if (metadata.Type == typeof(OracleVector))
        {
            return new BindingParameterContract()
            {
                Convert = o => o is OracleVector { IsNull: false } vector ? vector : DBNull.Value,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    if (pi.Parameter is OracleParameter oraParam && oraParam.Value is OracleVector)
                        oraParam.OracleDbType = OracleDbType.Vector;
                }
            };
        }
        else if (typeof(JsonNode).IsAssignableFrom(metadata.Type))
        {
            // JSON is sent as CLOB text, which converts implicitly to the 'JSON' type; VARCHAR2 binds
            // are limited to 4000 bytes on databases with the default MAX_STRING_SIZE
            return new BindingParameterContract()
            {
                Convert = fallback.Convert,
                Write = pi =>
                {
                    fallback?.Write?.Invoke(pi);

                    if (pi.Parameter is OracleParameter oraParam)
                        oraParam.OracleDbType = OracleDbType.Clob;
                }
            };
        }

        return null;
    }

    public IBindingCompositionContract GetCompositionContract(IBindingMetadata metadata) => null;
    public IBindingHelperContract GetHelperContract(IBindingMetadata metadata) => null;

    public IBindingValueContract GetValueContract(IBindingMetadata metadata)
    {
        IBindingValueContract fallback = metadata.Value;

        return new BindingValueContract()
        {
            Convert = fallback.Convert,
            Read = ci => this.GetValueReaderProxy(ci, fallback),
        };
    }

    
}
