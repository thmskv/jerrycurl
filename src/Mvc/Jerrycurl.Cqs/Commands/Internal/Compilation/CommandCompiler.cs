using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Jerrycurl.Cqs.Commands.Internal.Caching;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Queries.Internal;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Commands.Internal.Compilation
{
    internal class CommandCompiler
    {
        private delegate object BufferInternalConverter(object value, object helper);

        public BufferConverter Compile(MetadataIdentity metadata, ColumnMetadata columnInfo)
        {
            IBindingMetadata binding = metadata.Lookup<IBindingMetadata>();

            ParameterExpression inputParam = Expression.Parameter(typeof(object));
            ParameterExpression helperParam = Expression.Parameter(typeof(object));
            ParameterExpression helperVariable = this.GetHelperVariable(binding);

            Expression value = inputParam;

            if (binding != null)
            {
                Type sourceType = null;

                if (columnInfo != null)
                {
                    BindingColumnInfo bindingColumnInfo = new BindingColumnInfo()
                    {
                        Column = columnInfo,
                        CanBeNull = true,
                        Metadata = binding,
                    };

                    sourceType = binding.Value?.Read(bindingColumnInfo)?.ReturnType;
                }

                BindingValueInfo valueInfo = new BindingValueInfo()
                {
                    CanBeNull = true,
                    CanBeDbNull = true,
                    Metadata = binding,
                    Value = value,
                    SourceType = sourceType,
                    TargetType = binding.Type,
                    Helper = helperVariable,
                };

                try
                {
                    value = binding.Value?.Convert?.Invoke(valueInfo) ?? inputParam;
                }
                catch (BindingException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw BindingException.InvalidCast(binding, ex);
                }
                
            }

            value = this.GetObjectExpression(value);

            if (helperVariable != null)
            {
                Expression typedParam = Expression.Convert(helperParam, helperVariable.Type);
                Expression assignHelper = Expression.Assign(helperVariable, typedParam);

                value = Expression.Block(new[] { helperVariable }, assignHelper, value);
            }

            BufferInternalConverter innerFunc = Expression.Lambda<BufferInternalConverter>(value, inputParam, helperParam).Compile();

            object helperObject = binding?.Helper?.Object;

            return value => innerFunc(value, helperObject);
        }

        public BufferWriter Compile(IEnumerable<ColumnName> columnNames)
        {
            List<Expression> body = new List<Expression>();

            int index = 0;

            foreach (ColumnName columnName in columnNames)
            {
                IBindingMetadata metadata = columnName.Metadata.Lookup<IBindingMetadata>();

                Expression value = this.GetValueExpression(metadata, columnName.Column);
                Expression writer = this.GetWriterExpression(index++, value);

                body.Add(writer);
            }

            if (!body.Any())
                return (dr, buf) => { };

            ParameterExpression[] arguments = new[] { Arguments.DataReader, Arguments.Buffers };
            Expression block = Expression.Block(body);

            return Expression.Lambda<BufferWriter>(block, arguments).Compile();
        }

        private ParameterExpression GetHelperVariable(IBindingMetadata metadata)
            => metadata?.Helper?.Type != null ? Expression.Variable(metadata.Helper.Type) : null;

        private Expression GetValueExpression(IBindingMetadata metadata, ColumnMetadata columnInfo)
        {
            MethodInfo readMethod = this.GetValueReaderMethod(metadata, columnInfo);
            MethodInfo nullMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), new[] { typeof(int) });

            Expression readIndex = Expression.Constant(columnInfo.Index);
            Expression dataReader = Arguments.DataReader;

            if (readMethod.DeclaringType != typeof(IDataReader) && readMethod.DeclaringType != typeof(IDataRecord))
                dataReader = Expression.Convert(dataReader, readMethod.DeclaringType);

            Expression readValue = Expression.Call(dataReader, readMethod, readIndex);
            Expression isDbNull = Expression.Call(dataReader, nullMethod, readIndex);
            Expression dbNull = Expression.Field(null, typeof(DBNull).GetField(nameof(DBNull.Value)));

            Expression nullObject = Expression.Convert(dbNull, typeof(object));
            Expression readObject = Expression.Convert(readValue, typeof(object));

            return Expression.Condition(isDbNull, nullObject, readObject);
        }

        private Expression GetWriterExpression(int index, Expression value)
        {
            MethodInfo writeMethod = typeof(FieldBuffer).GetMethod(nameof(FieldBuffer.Write));

            Expression bufferIndex = Expression.ArrayAccess(Arguments.Buffers, Expression.Constant(index));

            return Expression.Call(bufferIndex, writeMethod, value);
        }

        private MethodInfo GetValueReaderMethod(IBindingMetadata metadata, ColumnMetadata columnInfo)
        {
            BindingColumnInfo bindingInfo = new BindingColumnInfo()
            {
                Metadata = metadata,
                Column = columnInfo,
            };

            MethodInfo readMethod = metadata.Value?.Read?.Invoke(bindingInfo);

            if (readMethod == null)
                readMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), new Type[] { typeof(int) });

            return readMethod;
        }

        private Expression GetObjectExpression(Expression expression)
        {
            if (expression.Type.IsValueType)
                return Expression.Convert(expression, typeof(object));

            return expression;
        }

        private static class Arguments
        {
            public static ParameterExpression DataReader { get; } = Expression.Parameter(typeof(IDataReader), "dataReader");
            public static ParameterExpression Buffers { get; } = Expression.Parameter(typeof(FieldBuffer[]), "buffers");
            public static ParameterExpression Helpers { get; } = Expression.Parameter(typeof(ElasticArray), "helpers");
        }
    }
}
