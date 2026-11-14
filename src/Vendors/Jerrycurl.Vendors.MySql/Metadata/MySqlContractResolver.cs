using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Jerrycurl.Cqs.Metadata;
using MySql.Data.MySqlClient;

namespace Jerrycurl.Vendors.MySql.Metadata;

public class MySqlContractResolver : IBindingContractResolver
{
    public int Priority => 1000;

    private MethodInfo GetMySqlReaderMethod(string methodName)
    {
        Type reader = typeof(MySqlDataReader);

        return reader.GetMethod(methodName, [typeof(int)]) ?? throw new InvalidOperationException("No such method.");
    }

    private MethodInfo GetValueReaderProxy(IBindingColumnInfo columnInfo, IBindingValueContract fallback)
    {
        if (columnInfo.Column.Type == typeof(TimeSpan))
            return this.GetMySqlReaderMethod(nameof(MySqlDataReader.GetTimeSpan));
        else if (columnInfo.Column.Type == typeof(sbyte))
            return this.GetMySqlReaderMethod(nameof(MySqlDataReader.GetSByte));
        else if (columnInfo.Column.Type == typeof(ushort))
            return this.GetMySqlReaderMethod(nameof(MySqlDataReader.GetUInt16));
        else if (columnInfo.Column.Type == typeof(uint))
            return this.GetMySqlReaderMethod(nameof(MySqlDataReader.GetUInt32));
        else if (columnInfo.Column.Type == typeof(ulong))
            return this.GetMySqlReaderMethod(nameof(MySqlDataReader.GetUInt64));

        return fallback?.Read(columnInfo);
    }

    public IBindingParameterContract GetParameterContract(IBindingMetadata metadata)
    {
        if (metadata.Type == typeof(float[]))
        {
            IBindingParameterContract fallback = metadata.Parameter;

            // VECTOR values are exchanged as binary strings of packed little-endian float32 elements
            return new BindingParameterContract()
            {
                Convert = o => o is float[] vector ? ToVectorBytes(vector) : DBNull.Value,
                Write = fallback.Write,
            };
        }

        return null;
    }

    public IBindingCompositionContract GetCompositionContract(IBindingMetadata metadata) => null;
    public IBindingHelperContract GetHelperContract(IBindingMetadata metadata) => null;

    public IBindingValueContract GetValueContract(IBindingMetadata metadata)
    {
        IBindingValueContract fallback = metadata.Value;

        if (metadata.Type == typeof(float[]))
        {
            return new BindingValueContract()
            {
                Convert = this.GetVectorReaderProxy,
                Read = ci => this.GetValueReaderProxy(ci, fallback),
            };
        }

        return new BindingValueContract()
        {
            Convert = fallback.Convert,
            Read = ci => this.GetValueReaderProxy(ci, fallback),
        };
    }

    private Expression GetVectorReaderProxy(IBindingValueInfo valueInfo)
    {
        MethodInfo convertInfo = typeof(MySqlContractResolver).GetMethod(nameof(FromVectorValue), BindingFlags.NonPublic | BindingFlags.Static);

        return Expression.Call(convertInfo, Expression.Convert(valueInfo.Value, typeof(object)));
    }

    private static byte[] ToVectorBytes(float[] vector)
    {
        byte[] bytes = MemoryMarshal.AsBytes(vector.AsSpan()).ToArray();

        if (!BitConverter.IsLittleEndian)
        {
            for (int i = 0; i < bytes.Length; i += sizeof(float))
                Array.Reverse(bytes, i, sizeof(float));
        }

        return bytes;
    }

    private static float[] FromVectorValue(object value)
    {
        switch (value)
        {
            case null or DBNull:
                return null;
            case float[] vector:
                return vector;
            case byte[] bytes when bytes.Length % sizeof(float) == 0:
                if (!BitConverter.IsLittleEndian)
                {
                    bytes = (byte[])bytes.Clone();

                    for (int i = 0; i < bytes.Length; i += sizeof(float))
                        Array.Reverse(bytes, i, sizeof(float));
                }

                return MemoryMarshal.Cast<byte, float>(bytes).ToArray();
            default:
                throw new InvalidCastException($"Cannot convert value of type '{value.GetType().Name}' to a vector.");
        }
    }
}
