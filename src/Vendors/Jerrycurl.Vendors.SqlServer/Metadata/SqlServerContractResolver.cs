using System;
using System.Data;
using System.Reflection;
using Jerrycurl.Cqs.Metadata;
using Microsoft.Data.SqlClient;

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

    private MethodInfo GetValueReadMethod(IBindingColumnInfo columnInfo, IBindingValueContract fallback)
    {
        if (columnInfo.Column.Type == typeof(DateTimeOffset))
            return this.GetSqlReaderMethod("GetDateTimeOffset");
        else if (columnInfo.Column.Type == typeof(TimeSpan))
            return this.GetSqlReaderMethod("GetTimeSpan");

        return fallback?.Read(columnInfo);
    }
}
