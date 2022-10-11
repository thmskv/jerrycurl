using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Dynamic;
using System.Xml.Linq;
using Jerrycurl.Relations;

namespace Jerrycurl.Cqs.Metadata
{
    public class DefaultBindingContractResolver : IBindingContractResolver
    {
        private readonly Type[] intTypes = new[] { typeof(int), typeof(long), typeof(short), typeof(char), typeof(byte), typeof(sbyte), typeof(ushort), typeof(uint), typeof(ulong) };
        private readonly Type[] decTypes = new[] { typeof(float), typeof(double), typeof(decimal) };

        public int Priority => 0;
        public IBindingHelperContract GetHelperContract(IBindingMetadata metadata) => null;

        public IBindingParameterContract GetParameterContract(IBindingMetadata metadata)
        {
            if (metadata.Type == typeof(XDocument))
                return this.GetXDocumentParameterContract();

            return new BindingParameterContract()
            {
                Convert = o => o ?? DBNull.Value,
                Write = info =>
                {
                    DbType? dbType = this.GetDbType(metadata);

                    if (dbType != null)
                        info.Parameter.DbType = dbType.Value;
                }
            };
        }

        public IBindingCompositionContract GetCompositionContract(IBindingMetadata metadata)
        {
            return new BindingCompositionContract()
            {
                Add = this.GetAddMethod(metadata),
                AddDynamic = this.GetAddDynamicMethod(metadata),
                Construct = this.GetConstructor(metadata),
            };
        }

        public IBindingValueContract GetValueContract(IBindingMetadata metadata)
        {
            return new BindingValueContract()
            {
                Read = this.GetRecordReaderMethod,
                Convert = this.GetValueReaderProxy,
            };
        }

        private MethodInfo GetAddMethod(IBindingMetadata metadata)
        {
            Type itemType = this.GetListElementType(metadata);
            Type oneType = this.GetOneValueType(metadata);

            if (itemType != null)
                return typeof(ICollection<>).MakeGenericType(itemType).GetMethod("Add");
            else if (oneType != null)
            {
                PropertyInfo valueInfo = metadata.Type.GetProperty(nameof(One<object>.Value), BindingFlags.Instance | BindingFlags.Public);

                if (valueInfo == null || valueInfo.SetMethod == null)
                    throw new InvalidOperationException("Invalid 'Value' property.");

                return valueInfo.SetMethod;
            }

            return null;
        }

        private NewExpression GetConstructor(IBindingMetadata metadata) => this.GetListConstructor(metadata) ?? this.GetDefaultConstructor(metadata);

        private bool IsOneValueType(IBindingMetadata metadata, out Type valueType)
            => ((valueType = this.GetOneValueType(metadata)) != null);

        private Type GetOneValueType(IBindingMetadata metadata)
        {
            Type openType = this.GetOpenType(metadata);

            if (openType == typeof(One<>))
                return metadata.Type.GetGenericArguments()[0];

            return null;
        }

        private Type GetListElementType(IBindingMetadata metadata)
        {
            Type[] openLists = new Type[]
            {
                typeof(List<>),
                typeof(IList<>),
                typeof(IReadOnlyList<>),
                typeof(IReadOnlyCollection<>),
                typeof(ICollection<>),
                typeof(IEnumerable<>),
            };

            Type openType = this.GetOpenType(metadata);

            if (openLists.Contains(openType))
                return metadata.Type.GetGenericArguments()[0];

            return null;
        }

        private DbType? GetDbType(IBindingMetadata metadata)
        {
            Type dataType = this.GetUnwrappedType(metadata.Type);

            if (dataType == typeof(byte))
                return DbType.Byte;
            else if (dataType == typeof(sbyte))
                return DbType.SByte;
            else if (dataType == typeof(short))
                return DbType.Int16;
            else if (dataType == typeof(ushort))
                return DbType.UInt16;
            else if (dataType == typeof(int))
                return DbType.Int32;
            else if (dataType == typeof(uint))
                return DbType.UInt32;
            else if (dataType == typeof(long))
                return DbType.Int64;
            else if (dataType == typeof(ulong))
                return DbType.UInt64;
            else if (dataType == typeof(float))
                return DbType.Single;
            else if (dataType == typeof(double))
                return DbType.Double;
            else if (dataType == typeof(decimal))
                return DbType.Decimal;
            else if (dataType == typeof(bool))
                return DbType.Boolean;
            else if (dataType == typeof(string))
                return DbType.String;
            else if (dataType == typeof(char))
                return DbType.StringFixedLength;
            else if (dataType == typeof(Guid))
                return DbType.Guid;
            else if (dataType == typeof(DateTime))
                return DbType.DateTime;
            else if (dataType == typeof(DateTimeOffset))
                return DbType.DateTimeOffset;
            else if (dataType == typeof(byte[]))
                return DbType.Binary;
            else if (dataType == typeof(XDocument))
                return DbType.String;

            return null;
        }

        private Type GetUnwrappedType(Type type) => type != null ? (Nullable.GetUnderlyingType(type) ?? type) : null;
        private Type GetOpenType(IBindingMetadata metadata)
        {
            if (metadata.Type.IsGenericType)
                return metadata.Type.GetGenericTypeDefinition();

            return null;
        }

        private NewExpression GetDefaultConstructor(IBindingMetadata metadata)
        {
            if (metadata.HasFlag(BindingMetadataFlags.Dynamic))
                return Expression.New(typeof(ExpandoObject));

            if (metadata.Type.IsValueType)
                return Expression.New(metadata.Type);

            ConstructorInfo constructorInfo = metadata.Type.GetConstructor(Type.EmptyTypes);

            if (constructorInfo != null)
                return Expression.New(constructorInfo);

            return null;
        }

        private NewExpression GetListConstructor(IBindingMetadata metadata)
        {
            Type elementType = this.GetListElementType(metadata);
            Type openType = this.GetOpenType(metadata);

            if (elementType != null && openType == typeof(One<>))
                return Expression.New(metadata.Type);
            else if (elementType != null)
            {
                Type concreteType = typeof(List<>).MakeGenericType(elementType);

                return Expression.New(concreteType);
            }

            return null;
        }

        private MethodInfo GetAddDynamicMethod(IBindingMetadata metadata)
        {
            if (metadata.HasFlag(BindingMetadataFlags.Dynamic))
            {
                Type dictionaryType = typeof(IDictionary<string, object>);

                return dictionaryType.GetMethod("Add", new[] { typeof(string), typeof(object) });
            }

            return null;
        }

        private bool IsIntegerType(Type type) => this.intTypes.Contains(type);
        private bool IsFloatType(Type type) => this.decTypes.Contains(type);
        private bool IsNumberType(Type type) => (this.IsIntegerType(type) || this.IsFloatType(type));

        private IEnumerable<Type> GetNumberTypes() => this.intTypes.Concat(this.decTypes);

        private Expression GetXDocumentReaderProxy(IBindingValueInfo valueInfo)
        {
            MethodInfo parseInfo = typeof(XDocument).GetMethod("Parse", new[] { typeof(string) });

            Expression value = valueInfo.Value;
            Expression stringValue = value.Type == typeof(string) ? value : Expression.Convert(value, typeof(string));
            Expression isNull = Expression.ReferenceEqual(valueInfo.Value, Expression.Constant(null));
            Expression parseXml = Expression.Call(parseInfo, stringValue);

            return Expression.Condition(isNull, Expression.Default(typeof(XDocument)), parseXml);
        }

        private BindingParameterContract GetXDocumentParameterContract()
        {
            return new BindingParameterContract()
            {
                Convert = o =>
                {
                    if (o is XDocument xd)
                        return xd.ToString();
                    else
                        return o ?? DBNull.Value;
                },
                Write = info =>
                {
                    info.Parameter.DbType = DbType.String;
                }
            };
        }

        private Expression GetObjectReaderProxy(IBindingValueInfo valueInfo)
        {
            Expression value = valueInfo.Value;
            Expression testValue = value;

            Stack<ConditionalExpression> conditions = new Stack<ConditionalExpression>();

            if (this.IsNumberType(valueInfo.Metadata.Type))
            {
                List<Type> testTypes = this.GetNumberTypes().Where(t => t != valueInfo.Metadata.Type).ToList();

                testTypes.Insert(0, valueInfo.Metadata.Type);

                foreach (Type numberType in testTypes)
                {
                    Expression unboxedValue = this.GetConvertExpression(valueInfo.Metadata, value, numberType);
                    Expression castValue = this.GetConvertExpression(valueInfo.Metadata, unboxedValue, valueInfo.Metadata.Type);

                    AddTypeIsExpression(numberType, castValue);
                }
            }
            else if (valueInfo.Metadata.Type == typeof(bool))
            {
                List<Type> testTypes = this.GetNumberTypes().Where(t => t != valueInfo.Metadata.Type).ToList();

                testTypes.Insert(0, typeof(bool));

                foreach (Type numberType in this.GetNumberTypes())
                {
                    Expression unboxedValue = this.GetConvertExpression(valueInfo.Metadata, value, numberType);
                    Expression boolValue = Expression.NotEqual(unboxedValue, Expression.Default(numberType));

                    AddTypeIsExpression(numberType, boolValue);
                }
            }

            if (conditions.Any())
                value = conditions.Aggregate((c0, c1) => Expression.Condition(c1.Test, c1.IfTrue, c0));

            if (valueInfo.CanBeDbNull || valueInfo.CanBeNull)
            {
                Expression isNull = this.GetIsNullExpression(valueInfo);
                Expression defaultValue = Expression.Default(valueInfo.Metadata.Type);

                if (valueInfo.Metadata.Type.IsValueType)
                    defaultValue = this.GetConvertExpression(valueInfo.Metadata, defaultValue, typeof(object));

                value = Expression.Condition(isNull, defaultValue, value, typeof(object));
            }

            if (!valueInfo.Metadata.Type.IsAssignableFrom(value.Type))
                value = this.GetConvertExpression(valueInfo.Metadata, value, valueInfo.Metadata.Type);

            return value;

            void AddTypeIsExpression(Type testType, Expression newValue)
            {
                Expression typeIs = Expression.TypeIs(value, testType);

                if (newValue.Type.IsValueType)
                    newValue = this.GetConvertExpression(valueInfo.Metadata, newValue, typeof(object));

                conditions.Push(Expression.Condition(typeIs, newValue, value));
            }
        }

        private Expression GetIsNullExpression(IBindingValueInfo valueInfo)
        {
            Expression isDbNull = Expression.TypeIs(valueInfo.Value, typeof(DBNull));
            Expression isNull = Expression.ReferenceEqual(valueInfo.Value, Expression.Constant(null));

            if (valueInfo.CanBeDbNull && valueInfo.CanBeNull)
                return Expression.OrElse(isDbNull, isNull);
            else if (valueInfo.CanBeDbNull)
                return isDbNull;
            else if (valueInfo.CanBeNull)
                return isNull;

            return null;
        }

        private Expression GetValueReaderProxy(IBindingValueInfo valueInfo)
        {
            Type sourceType = valueInfo.SourceType ?? typeof(object);
            Type targetType = valueInfo.TargetType ?? typeof(object);

            if (valueInfo.Value.Type == typeof(object) && sourceType == typeof(object))
                return this.GetObjectReaderProxy(valueInfo);
            else if (targetType == typeof(XDocument))
                return this.GetXDocumentReaderProxy(valueInfo);

            Expression value = valueInfo.Value;

            if (valueInfo.Value.Type == typeof(object) && sourceType != typeof(object))
                value = this.GetConvertExpression(valueInfo.Metadata, value, sourceType);

            Type structLeft = targetType.IsValueType ? Nullable.GetUnderlyingType(targetType) ?? targetType : null;
            Type structRight = value.Type.IsValueType ? Nullable.GetUnderlyingType(value.Type) ?? value.Type : null;

            if (this.IsNumberType(structLeft) && this.IsNumberType(structRight) && structLeft != structRight)
                value = this.GetConvertCheckedExpression(valueInfo.Metadata, value, targetType);
            else if (structLeft == typeof(bool) && this.IsNumberType(structRight))
                value = Expression.NotEqual(valueInfo.Value, Expression.Default(value.Type));

            if (targetType != value.Type)
                value = this.GetConvertExpression(valueInfo.Metadata, value, valueInfo.Metadata.Type);

            if (valueInfo.CanBeDbNull || (valueInfo.CanBeNull && this.IsNotNullableValueType(targetType)))
            {
                Expression isNull = this.GetIsNullExpression(valueInfo);
                Expression defaultValue = Expression.Default(targetType);

                value = Expression.Condition(isNull, defaultValue, value);
            }

            if (!targetType.IsAssignableFrom(value.Type))
                value = this.GetConvertExpression(valueInfo.Metadata, value, targetType);

            return value;
        }

        private Expression GetConvertCheckedExpression(IBindingMetadata metadata, Expression value, Type type)
        {
            try
            {
                return Expression.ConvertChecked(value, type);
            }
            catch (Exception ex)
            {
                throw BindingException.InvalidCast(metadata, value.Type, type, ex);
            }
        }

        private Expression GetConvertExpression(IBindingMetadata metadata, Expression value, Type type)
        {
            try
            {
                return Expression.Convert(value, type);
            }
            catch (Exception ex)
            {
                throw BindingException.InvalidCast(metadata, value.Type, type, ex);
            }
        }

        private bool IsNotNullableValueType(Type type) => (type.IsValueType && Nullable.GetUnderlyingType(type) == null);
        private MethodInfo GetRecordMethod(string methodName) => typeof(IDataRecord).GetMethod(methodName, new[] { typeof(int) });

        private MethodInfo GetRecordReaderMethod(IBindingColumnInfo bindingInfo)
        {
            Type columnType = this.GetUnwrappedType(bindingInfo.Column.Type);

            if (columnType == typeof(bool))
                return this.GetRecordMethod(nameof(IDataReader.GetBoolean));
            else if (columnType == typeof(byte))
                return this.GetRecordMethod(nameof(IDataReader.GetByte));
            else if (columnType == typeof(float))
                return this.GetRecordMethod(nameof(IDataReader.GetFloat));
            else if (columnType == typeof(double))
                return this.GetRecordMethod(nameof(IDataReader.GetDouble));
            else if (columnType == typeof(decimal))
                return this.GetRecordMethod(nameof(IDataReader.GetDecimal));
            else if (columnType == typeof(short))
                return this.GetRecordMethod(nameof(IDataReader.GetInt16));
            else if (columnType == typeof(int))
                return this.GetRecordMethod(nameof(IDataReader.GetInt32));
            else if (columnType == typeof(long))
                return this.GetRecordMethod(nameof(IDataReader.GetInt64));
            else if (columnType == typeof(DateTime))
                return this.GetRecordMethod(nameof(IDataReader.GetDateTime));
            else if (columnType == typeof(Guid))
                return this.GetRecordMethod(nameof(IDataReader.GetGuid));
            else if (columnType == typeof(char))
                return this.GetRecordMethod(nameof(IDataReader.GetChar));
            else if (columnType == typeof(string))
                return this.GetRecordMethod(nameof(IDataReader.GetString));

            return null;
        }
    }
}
