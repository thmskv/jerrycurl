using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Jerrycurl.Collections;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Reflection;
using Jerrycurl.Cqs.Queries.Internal.IO;
using Jerrycurl.Cqs.Queries.Internal.Caching;
using Jerrycurl.Cqs.Queries.Internal.IO.Writers;
using Jerrycurl.Cqs.Queries.Internal.IO.Readers;
using Jerrycurl.Cqs.Queries.Internal.IO.Targets;

namespace Jerrycurl.Cqs.Queries.Internal.Compilation
{
    internal class QueryCompiler
    {
        private delegate void ListInternalWriter(IDataReader dataReader, ElasticArray lists, ElasticArray aggregates, ElasticArray helpers, ISchema schema);
        private delegate void ListInternalInitializer(ElasticArray lists);
        private delegate object AggregateInternalReader(ElasticArray lists, ElasticArray aggregates, ISchema schema);
        private delegate TItem EnumerateInternalReader<TItem>(IDataReader dataReader, ElasticArray helpers, ISchema schema);

        private ListFactory CompileBuffer(ListResult result, Expression initialize, Expression writeOne, Expression writeAll)
        {
            ParameterExpression[] initArgs = new[] { Arguments.Lists };
            ParameterExpression[] writeArgs = new[] { Arguments.DataReader, Arguments.Lists, Arguments.Aggregates, Arguments.Helpers, Arguments.Schema };

            ListInternalInitializer initializeFunc = this.Compile<ListInternalInitializer>(initialize, initArgs);
            ListInternalWriter writeOneFunc = this.Compile<ListInternalWriter>(writeOne, writeArgs);
            ListInternalWriter writeAllFunc = this.Compile<ListInternalWriter>(writeAll, writeArgs);

            ElasticArray helpers = this.GetHelperBuffer(result.Helpers);
            ISchema schema = result.Schema;

            if (result.QueryType == QueryType.Aggregate)
            {
                AggregateAttribute[] header = result.Aggregates.Select(a => a.Attribute).NotNull().ToArray();

                return new ListFactory()
                {
                    Initialize = buf =>
                    {
                        buf.AggregateHeader.AddRange(header);

                        initializeFunc(buf.ListData);
                    },
                    WriteOne = (buf, dr) => writeOneFunc(dr, buf.ListData, buf.AggregateData, helpers, schema),
                    WriteAll = (buf, dr) =>
                    {
                        buf.AggregateHeader.AddRange(header);

                        writeAllFunc(dr, buf.ListData, buf.AggregateData, helpers, schema);
                    },
                };
            }
            else
            {
                return new ListFactory()
                {
                    WriteAll = (buf, dr) => writeAllFunc(dr, buf.ListData, buf.AggregateData, helpers, schema),
                    WriteOne = (buf, dr) => writeOneFunc(dr, buf.ListData, buf.AggregateData, helpers, schema),
                    Initialize = buf => initializeFunc(buf.ListData),
                };
            }
        }

        public ListFactory Compile(ListResult result)
        {
            List<ParameterExpression> variables = new List<ParameterExpression>();

            List<Expression> initList = new List<Expression>();
            List<Expression> oneList = new List<Expression>();
            List<Expression> allList = new List<Expression>();

            List<Expression> body = new List<Expression>();

            foreach (ListTarget target in result.Targets)
            {
                Expression prepareBuffer = this.GetPrepareBufferExpression(target);
                Expression prepareVariable = this.GetPrepareVariableExpression(target);
                Expression prepareBufferAndVariable = this.GetPrepareBufferAndVariableExpression(target);

                initList.Add(prepareBuffer);
                oneList.Add(prepareVariable);
                allList.Add(prepareBufferAndVariable);

                variables.Add(target.Variable);
            }

            foreach (HelperWriter writer in result.Helpers)
            {
                Expression writeExpression = this.GetWriterExpression(writer);

                oneList.Add(writeExpression);
                allList.Add(writeExpression);

                variables.Add(writer.Variable);
            }

            foreach (AggregateWriter writer in result.Aggregates.Where(a => a.Attribute.ListIndex == null))
                body.Add(this.GetWriterExpression(writer));

            foreach (TargetWriter writer in result.Writers)
                body.Add(this.GetWriterExpression(writer));

            oneList.AddRange(body);
            allList.Add(this.GetDataReaderLoopExpression(body));

            Expression initialize = this.GetBlockOrExpression(initList);
            Expression writeOne = this.GetBlockOrExpression(oneList, variables);
            Expression writeAll = this.GetBlockOrExpression(allList, variables);

            return this.CompileBuffer(result, initialize, writeOne, writeAll);
        }

        public AggregateFactory Compile(AggregateResult result)
        {
            Expression body = Expression.Default(result.Metadata.Type);

            if (result.Value != null)
                body = this.GetAggregateExpression(result.Value, result.Target);

            ParameterExpression[] arguments = new[] { Arguments.Lists, Arguments.Aggregates, Arguments.Schema };
            AggregateInternalReader reader = this.Compile<AggregateInternalReader>(body, arguments);

            ISchema schema = result.Schema;

            return buf => reader(buf.ListData, buf.AggregateData, schema);
        }

        public EnumerateFactory<TItem> Compile<TItem>(EnumerateResult result)
        {
            List<Expression> body = new List<Expression>();

            if (result.Value == null)
                return _ => default;

            foreach (HelperWriter writer in result.Helpers)
                body.Add(this.GetWriterExpression(writer));

            body.Add(this.GetReaderExpression(result.Value));

            ParameterExpression[] arguments = new[] { Arguments.DataReader, Arguments.Helpers, Arguments.Schema };
            EnumerateInternalReader<TItem> reader = this.Compile<EnumerateInternalReader<TItem>>(body, arguments);

            ElasticArray helpers = this.GetHelperBuffer(result.Helpers);
            ISchema schema = result.Schema;

            return dr => reader(dr, helpers, schema);
        }

        #region " Aggregate "
        public Expression GetAggregateExpression(BaseReader reader, AggregateTarget target)
        {
            if (target == null)
                return this.GetReaderExpression(reader);

            ParameterExpression variable = Expression.Variable(target.NewList.Type);

            Expression value = this.GetReaderExpression(reader);
            Expression assignList = Expression.Assign(variable, target.NewList);
            Expression addValue = Expression.Call(variable, target.AddMethod, value);

            return this.GetBlockOrExpression(new[] { assignList, addValue, variable }, new[] { variable });
        }
        #endregion

        #region " Prepare "
        private Expression GetPrepareBufferAndVariableExpression(ListTarget target)
        {
            Expression bufferIndex = this.GetElasticIndexExpression(Arguments.Lists, target.Index);
            Expression newExpression = Expression.Assign(target.Variable, target.NewTarget);
            Expression setNew = Expression.Assign(bufferIndex, newExpression);
            Expression setOld = Expression.Assign(target.Variable, Expression.Convert(bufferIndex, target.Variable.Type));
            Expression notNull = this.GetIsNotNullExpression(bufferIndex);

            return Expression.IfThenElse(notNull, setOld, setNew);
        }

        private Expression GetPrepareVariableExpression(ListTarget target)
        {
            Expression bufferIndex = this.GetElasticIndexExpression(Arguments.Lists, target.Index);
            Expression bufferValue = this.GetConvertOrExpression(bufferIndex, target.Variable.Type);

            return Expression.Assign(target.Variable, bufferValue);
        }

        private Expression GetPrepareBufferExpression(ListTarget target)
        {
            Expression bufferIndex = this.GetElasticIndexExpression(Arguments.Lists, target.Index);

            return this.GetElasticGetOrSetExpression(bufferIndex, target.NewTarget, convertValue: false);
        }

        #endregion

        #region " Writers "
        private Expression GetWriterExpression(TargetWriter writer)
        {
            Expression value = this.GetReaderExpression(writer.Source);
            Expression buffer = Arguments.Lists;
            Expression bufferIndex = this.GetElasticIndexExpression(buffer, writer.List.Index);
            Expression bufferNotNull = this.GetIsNotNullExpression(buffer);

            if (writer.Join != null)
            {
                buffer = writer.Join.Buffer;
                bufferIndex = this.GetElasticIndexExpression(buffer, writer.Join.Index);
                bufferNotNull = this.GetIsNotNullExpression(buffer);
            }

            Expression body;

            if (writer.List.NewList == null && writer.Join?.NewList == null)
            {
                Expression convertValue = this.GetConvertOrExpression(value, typeof(object));

                body = Expression.Assign(bufferIndex, convertValue);

                if (writer.Join != null)
                    body = Expression.IfThen(bufferNotNull, body);
            }
            else if (writer.Join == null)
                body = Expression.Call(writer.List.Variable, writer.List.AddMethod, value);
            else
            {
                Expression listValue = this.GetElasticGetOrSetExpression(bufferIndex, writer.Join.NewList);
                Expression addValue = Expression.Call(listValue, writer.Join.AddMethod, value);

                body = Expression.IfThen(bufferNotNull, addValue);
            }

            return this.GetKeyBlockExpression(new[] { writer.PrimaryKey }, new[] { writer.Join }.Concat(writer.ForeignJoins), body);
        }
        
        private Expression GetWriterExpression(HelperWriter writer)
        {
            Expression helperIndex = this.GetElasticIndexExpression(Arguments.Helpers, writer.BufferIndex);
            Expression castValue = Expression.Convert(helperIndex, writer.Variable.Type);

            return Expression.Assign(writer.Variable, castValue);
        }

        private Expression GetWriterExpression(AggregateWriter writer, bool useTryCatch = true)
        {
            Expression bufferIndex = this.GetElasticIndexExpression(Arguments.Aggregates, writer.Attribute.AggregateIndex.Value);
            Expression isDbNull = this.GetIsDbNullExpression(writer.Value);
            Expression value = writer.Value.Variable;

            if (value == null)
            {
                value = this.GetValueExpression(writer.Value);
                value = this.GetConvertExpression(writer.Value, value);

                if (useTryCatch)
                    value = this.GetTryCatchExpression(writer.Value, value);
            }

            Expression objectValue = Expression.Convert(value, typeof(object));
            Expression nullBlock = Expression.Condition(isDbNull, Expression.Constant(null, typeof(object)), objectValue);

            return Expression.Assign(bufferIndex, nullBlock);
        }

        #endregion

        #region " Keys "
        private Expression GetKeyInitializeValueExpression(DataReader reader, bool useTryCatch = true)
        {
            Expression value = this.GetValueExpression(reader);
            Expression convertedValue = this.GetConvertExpression(reader, value);

            if (useTryCatch)
                convertedValue = this.GetTryCatchExpression(reader, convertedValue);

            if (reader.CanBeDbNull)
            {
                Expression isDbNull = this.GetIsDbNullExpression(reader);
                Expression assignNull = Expression.Assign(reader.IsDbNull, isDbNull);

                convertedValue = Expression.Condition(assignNull, Expression.Default(convertedValue.Type), convertedValue);
            }

            return Expression.Assign(reader.Variable, convertedValue);
        }

        private Expression GetKeyInitializeBufferExpression(JoinTarget join)
        {
            Expression dictKey, dictKey2;

            if (join.Key.Variable != null)
            {
                Expression newKey = this.GetNewCompositeKeyExpression(join.Key);

                dictKey = Expression.Assign(join.Key.Variable, newKey);
                dictKey2 = join.Key.Variable;
            }
            else
                dictKey = dictKey2 = join.Key.Values[0].Variable;
            
            Expression tryGet = this.GetDictionaryTryGetExpression(join.List.Variable, dictKey, join.Buffer);
            Expression newBuffer = Expression.New(typeof(ElasticArray));
            Expression bufferIndex = this.GetElasticIndexExpression(join.Buffer, join.Index);
            Expression assignBuffer = Expression.Assign(join.Buffer, newBuffer);
            Expression addArray = this.GetDictionaryAddExpression(join.List.Variable, dictKey, assignBuffer);
            Expression getOrAdd = Expression.IfThen(Expression.Not(tryGet), addArray);

            IEnumerable<ParameterExpression> isNullVars = join.Key.Values.Where(v => v.CanBeDbNull).Select(v => v.IsDbNull).ToList();

            if (isNullVars.Any())
            {
                Expression isNull = this.GetAndConditionExpression(isNullVars);
                Expression setNull = Expression.Assign(join.Buffer, Expression.Constant(null, typeof(ElasticArray)));

                getOrAdd = Expression.IfThenElse(isNull, setNull, getOrAdd);
            }

            return getOrAdd;
        }

        private Expression GetKeyBlockExpression(IEnumerable<KeyReader> primaryKeys, IEnumerable<JoinTarget> joins, Expression body)
        {
            List<Expression> expressions = new List<Expression>();
            List<ParameterExpression> variables = new List<ParameterExpression>();

            foreach (DataReader reader in joins.NotNull().SelectMany(k => k.Key.Values).Distinct())
            {
                expressions.Add(this.GetKeyInitializeValueExpression(reader));

                if (reader.CanBeDbNull)
                    variables.Add(reader.IsDbNull);

                variables.Add(reader.Variable);
            }

            foreach (JoinTarget join in joins.NotNull())
            {
                expressions.Add(this.GetKeyInitializeBufferExpression(join));
                variables.Add(join.Buffer);

                if (join.Key.Variable != null)
                    variables.Add(join.Key.Variable);
            }
                
            expressions.Add(body);

            Expression block = this.GetBlockOrExpression(expressions, variables);

            if (primaryKeys.NotNull().Any())
            {
                IEnumerable<DataReader> values = primaryKeys.NotNull().SelectMany(k => k.Values).Distinct();

                Expression isNotNull = this.GetAndConditionExpression(values, this.GetIsNotDbNullExpression);

                return Expression.Condition(isNotNull, block, Expression.Default(block.Type));
            }

            return block;
        }

        #endregion

        #region " Readers "

        private Expression GetReaderExpression(BaseReader reader) => reader switch
        {
            ColumnReader r => this.GetReaderExpression(r, r.IsDbNull, r.Variable, r.CanBeDbNull),
            AggregateReader r => this.GetReaderExpression(r, r.IsDbNull, r.Variable, r.CanBeDbNull),
            NewReader r => this.GetReaderExpression(r),
            JoinReader r => this.GetReaderExpression(r),
            DynamicReader r => this.GetReaderExpression(r),
            _ => throw new InvalidOperationException(),
        };

        private Expression GetReaderExpression(JoinReader reader)
        {
            Expression bufferIndex = this.GetElasticIndexExpression(reader.Target.Buffer, reader.Target.Index);
            Expression bufferNotNull = this.GetIsNotNullExpression(reader.Target.Buffer);

            if (reader.Target.NewList == null)
            {
                Expression convertedValue = this.GetConvertOrExpression(bufferIndex, reader.Metadata.Type);
                Expression ifThen = Expression.Condition(bufferNotNull, convertedValue, Expression.Default(convertedValue.Type));

                return this.GetConvertOrExpression(ifThen, reader.Metadata.Type);
            }
            else
            {
                Expression getOrSetList = this.GetElasticGetOrSetExpression(bufferIndex, reader.Target.NewList);

                return Expression.Condition(bufferNotNull, getOrSetList, reader.Target.NewList);
            }
        }

        private Expression GetReaderExpression(DataReader reader, Expression isDbNull, Expression value, bool canBeDbNull, bool useTryCatch = true)
        {
            isDbNull ??= this.GetIsDbNullExpression(reader);

            if (value == null)
            {
                value = this.GetValueExpression(reader);
                value = this.GetConvertExpression(reader, value);

                if (useTryCatch)
                    value = this.GetTryCatchExpression(reader, value);

                if (canBeDbNull)
                {
                    Expression defaultValue = Expression.Default(reader.Metadata.Type);
                    Expression matchedValue = value;

                    if (value.Type != defaultValue.Type)
                        matchedValue = Expression.Convert(matchedValue, reader.Metadata.Type);

                    value = Expression.Condition(isDbNull, Expression.Default(reader.Metadata.Type), matchedValue);
                }
            }
            else if (canBeDbNull && value.Type != reader.Metadata.Type)
            {
                Expression defaultValue = Expression.Default(reader.Metadata.Type);
                Expression matchedValue = Expression.Convert(value, reader.Metadata.Type);

                value = Expression.Condition(isDbNull, defaultValue, matchedValue);
            }
            else if (value.Type != reader.Metadata.Type)
                value = Expression.Convert(value, reader.Metadata.Type);

            return value;
        }

        private Expression GetReaderExpression(NewReader reader)
        {
            NewExpression newExpression = reader.Metadata.Composition.Construct;

            if (newExpression == null)
                throw BindingException.InvalidConstructor(reader.Metadata);

            Expression memberInit = Expression.MemberInit(newExpression, reader.Properties.Select(r =>
            {
                if (!r.Metadata.HasFlag(BindingMetadataFlags.Writable))
                    throw BindingException.IsReadOnly(r.Metadata);

                Expression value = this.GetReaderExpression(r);

                return Expression.Bind(r.Metadata.Member, value);
            }));

            return this.GetKeyBlockExpression(new[] { reader.PrimaryKey }, reader.Joins, memberInit);
        }

        private Expression GetReaderExpression(DynamicReader reader)
        {
            ParameterExpression variable = Expression.Variable(reader.Metadata.Composition.Construct.Type);
            NewExpression newExpression = reader.Metadata.Composition.Construct;

            List<Expression> body = new List<Expression>()
            {
                Expression.Assign(variable, newExpression),
            };

            foreach (BaseReader propertyReader in reader.Properties)
            {
                string propertyName = propertyReader.Identity.Schema.Notation.Member(propertyReader.Identity.Name);

                Expression propertyValue = this.GetReaderExpression(propertyReader);
                Expression objectValue = propertyValue.Type.IsValueType ? Expression.Convert(propertyValue, typeof(object)) : propertyValue;
                Expression addDynamic = Expression.Call(variable, reader.Metadata.Composition.AddDynamic, Expression.Constant(propertyName), objectValue);

                body.Add(addDynamic);
            }

            body.Add(variable);

            return Expression.Block(new[] { variable }, body);
        }

        #endregion

        #region  " IsDbNull "

        private Expression GetIsNotDbNullExpression(DataReader reader)
            => Expression.Not(this.GetIsDbNullExpression(reader));

        private Expression GetIsDbNullExpression(DataReader reader) => reader switch
        {
            ColumnReader r => this.GetIsDbNullExpression(r),
            AggregateReader r => this.GetIsDbNullExpression(r),
            _ => throw new InvalidOperationException(),
        };

        private Expression GetIsDbNullExpression(ColumnReader reader)
        {
            MethodInfo isNullMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), new[] { typeof(int) });

            return Expression.Call(Arguments.DataReader, isNullMethod, Expression.Constant(reader.Column.Index));
        }

        private Expression GetIsDbNullExpression(AggregateReader reader)
        {
            Expression bufferIndex;

            if (reader.Attribute.AggregateIndex != null)
                bufferIndex = this.GetElasticIndexExpression(Arguments.Aggregates, reader.Attribute.AggregateIndex.Value);
            else if (reader.Attribute.ListIndex != null)
                bufferIndex = this.GetElasticIndexExpression(Arguments.Lists, reader.Attribute.ListIndex.Value);
            else
                throw new InvalidOperationException();

            return Expression.ReferenceEqual(bufferIndex, Expression.Constant(null));
        }

        #endregion

        #region " Values "
        private Expression GetValueExpression(BaseReader reader) => reader switch
        {
            ColumnReader r => this.GetValueExpression(r),
            AggregateReader r => this.GetValueExpression(r),
            _ => throw new InvalidOperationException(),
        };

        private Expression GetValueExpression(AggregateReader reader)
        {
            if (reader.Attribute.AggregateIndex != null)
                return this.GetElasticIndexExpression(Arguments.Aggregates, reader.Attribute.AggregateIndex.Value);
            else if (reader.Attribute.ListIndex != null)
                return this.GetElasticIndexExpression(Arguments.Lists, reader.Attribute.ListIndex.Value);
            else
                throw new InvalidOperationException();
        }   

        private Expression GetValueExpression(ColumnReader reader)
        {
            MethodInfo readMethod = this.GetValueReaderMethod(reader);

            Expression index = Expression.Constant(reader.Column.Index);
            Expression dataReader = Arguments.DataReader;

            if (readMethod.DeclaringType != typeof(IDataReader) && readMethod.DeclaringType != typeof(IDataRecord))
                dataReader = Expression.Convert(dataReader, readMethod.DeclaringType);

            return Expression.Call(dataReader, readMethod, index);
        }

        private MethodInfo GetValueReaderMethod(ColumnReader reader)
        {
            BindingColumnInfo bindingInfo = new BindingColumnInfo()
            {
                Metadata = reader.Metadata,
                Column = reader.Column,
            };

            MethodInfo readMethod = reader.Metadata.Value?.Read?.Invoke(bindingInfo);

            if (readMethod == null)
                readMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), new Type[] { typeof(int) });

            return readMethod;
        }

        #endregion

        #region " Convert "
        private Expression GetConvertExpression(BaseReader reader, Expression value) => reader switch
        {
            AggregateReader r => this.GetConvertExpression(r, value),
            ColumnReader r => this.GetConvertExpression(r, value),
            _ => throw new InvalidOperationException(),
        };

        private Expression GetConvertExpression(AggregateReader reader, Expression value)
            => this.GetConvertOrExpression(value, reader.Metadata.Type);

        private Expression GetConvertExpression(ColumnReader reader, Expression value)
        {
            Type targetType = reader.KeyType ?? reader.Metadata.Type;
            ParameterExpression variable = Expression.Variable(value.Type);

            BindingValueInfo valueInfo = new BindingValueInfo()
            {
                SourceType = value.Type,
                TargetType = targetType,
                CanBeNull = false,
                CanBeDbNull = false,
                Metadata = reader.Metadata,
                Value = variable,
                Helper = reader.Helper,
            };

            Expression convertedValue;

            try
            {
                convertedValue = reader.Metadata.Value?.Convert?.Invoke(valueInfo);
            }
            catch (BindingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw BindingException.InvalidCast(reader.Metadata, ex);
            }

            if (convertedValue == null || object.ReferenceEquals(convertedValue, variable))
                return value;
            else if (convertedValue is UnaryExpression ue)
            {
                if (ue.NodeType == ExpressionType.Convert && ue.Operand.Equals(variable))
                    return Expression.Convert(value, ue.Type);
                else if (ue.NodeType == ExpressionType.ConvertChecked && ue.Operand.Equals(variable))
                    return Expression.ConvertChecked(value, ue.Type);
            }

            Expression assignValue = Expression.Assign(variable, value);

            return Expression.Block(new[] { variable }, assignValue, convertedValue);
        }

        #endregion

        #region " Helpers "

        private Expression GetConvertOrExpression(Expression expression, Type type)
        {
            if (!expression.Type.IsValueType && type.IsValueType)
                return Expression.Convert(expression, type);
            else if (!type.IsValueType && expression.Type.IsValueType)
                return Expression.Convert(expression, type);
            else if (!type.IsAssignableFrom(expression.Type))
                return Expression.Convert(expression, type); 

            return expression;
        }

        private Expression GetBlockOrExpression(IList<Expression> expressions, IList<ParameterExpression> variables = null)
        {
            if (expressions.Count == 1 && (variables == null || !variables.Any()))
                return expressions[0];
            else if (expressions.Count == 0 && this.IsRunningNetFramework())
                return Expression.Block(Expression.Constant(0));
            else if (variables == null)
                return Expression.Block(expressions);
            else
                return Expression.Block(variables.NotNull(), expressions);
        }

        private TDelegate Compile<TDelegate>(IList<Expression> body, IList<ParameterExpression> arguments)
        {
            Expression block = this.GetBlockOrExpression(body);

            return Expression.Lambda<TDelegate>(block, arguments).Compile();
        }

        private TDelegate Compile<TDelegate>(Expression block, params ParameterExpression[] arguments)
            => this.Compile<TDelegate>(new[] { block }, arguments);

        private Expression GetDataReaderLoopExpression(IList<Expression> body)
        {
            LabelTarget label = Expression.Label();

            Expression callRead = Expression.Call(Arguments.DataReader, typeof(IDataReader).GetMethod(nameof(IDataReader.Read)));
            Expression ifRead = Expression.IfThenElse(callRead, this.GetBlockOrExpression(body), Expression.Break(label));

            return Expression.Loop(ifRead, label);
        }

        private Expression GetDictionaryAddExpression(Expression dictionary, Expression key, Expression value)
        {
            MethodInfo addMethod = dictionary.Type.GetMethod("Add");

            return Expression.Call(dictionary, addMethod, key, value);
        }

        private Expression GetDictionaryTryGetExpression(Expression dictionary, Expression key, Expression outVariable)
        {
            MethodInfo tryGetMethod = dictionary.Type.GetMethod("TryGetValue");

            return Expression.Call(dictionary, tryGetMethod, key, outVariable);
        }

        private Expression GetNewCompositeKeyExpression(KeyReader key)
        {
            if (key.Values.Count == 1)
                return key.Values[0].Variable;

            ConstructorInfo ctor = key.Variable.Type.GetConstructors()[0];

            return Expression.New(ctor, key.Values.Select(v => v.Variable));
        }

        private ElasticArray GetHelperBuffer(IEnumerable<HelperWriter> writers)
        {
            ElasticArray array = new ElasticArray();

            foreach (HelperWriter writer in writers)
                array[writer.BufferIndex] = writer.Object;

            return array;
        }

        private Expression GetIsNotNullExpression(Expression value)
            => Expression.ReferenceNotEqual(value, Expression.Constant(null));

        private Expression GetTryCatchExpression(BaseReader reader, Expression expression)
        {
            if (this.IsRunningNetFramework() && expression.Type.IsValueType)
                return expression;

            ParameterExpression ex = Expression.Variable(typeof(Exception));

            MethodInfo constructor = typeof(QueryCompiler).GetStaticMethod(nameof(QueryCompiler.GetInvalidCastException), typeof(ISchema), typeof(string), typeof(Exception));

            Expression newException = Expression.Call(constructor, Arguments.Schema, Expression.Constant(reader.Identity.Name), ex);
            CatchBlock catchBlock = Expression.Catch(ex, Expression.Throw(newException, expression.Type));

            return Expression.TryCatch(expression, catchBlock);
        }

        private Type GetDictionaryType(Type keyType)
            => typeof(Dictionary<,>).MakeGenericType(keyType, typeof(ElasticArray));

        private NewExpression GetNewDictionaryExpression(Type keyType)
        {
            Type dictType = this.GetDictionaryType(keyType);

            return Expression.New(dictType);
        }

        private Expression GetElasticGetOrSetExpression(Expression arrayIndex, Expression setExpression, bool convertValue = true)
        {
            Expression setIndex = Expression.Assign(arrayIndex, setExpression);
            Expression notNull = this.GetIsNotNullExpression(arrayIndex);
            Expression getOrSet = Expression.Condition(notNull, arrayIndex, setIndex);

            if (convertValue)
                return this.GetConvertOrExpression(getOrSet, setExpression.Type);

            return getOrSet;
        }

        private Expression GetElasticGetOrSetExpression(Expression arrayExpression, int index, Expression setExpression, bool convertValue = true)
        {
            Expression arrayIndex = this.GetElasticIndexExpression(arrayExpression, index);

            return this.GetElasticGetOrSetExpression(arrayIndex, setExpression, convertValue);
        }

        private Expression GetElasticIndexExpression(Expression arrayExpression, int index)
        {
            PropertyInfo indexer = arrayExpression.Type.GetProperty("Item");

            return Expression.Property(arrayExpression, indexer, Expression.Constant(index));
        }

        private Expression GetOrConditionExpression<T>(IEnumerable<T> values, Func<T, Expression> condition, Expression emptyValue = null)
            => this.GetConditionExpression(values, condition, Expression.OrElse, emptyValue);

        private Expression GetAndConditionExpression<T>(IEnumerable<T> values, Func<T, Expression> condition, Expression emptyValue = null)
            => this.GetConditionExpression(values, condition, Expression.AndAlso, emptyValue);

        private Expression GetOrConditionExpression(IEnumerable<Expression> conditions, Expression emptyValue = null)
            => this.GetConditionExpression(conditions, Expression.OrElse, emptyValue);

        private Expression GetAndConditionExpression(IEnumerable<Expression> conditions, Expression emptyValue = null)
            => this.GetConditionExpression(conditions, Expression.AndAlso, emptyValue);

        private Expression GetConditionExpression<T>(IEnumerable<T> values, Func<T, Expression> condition, Func<Expression, Expression, Expression> gateFactory, Expression emptyValue = null)
            => this.GetConditionExpression(values.Select(condition), gateFactory, emptyValue);

        private Expression GetConditionExpression(IEnumerable<Expression> conditions, Func<Expression, Expression, Expression> gateFactory, Expression emptyValue = null)
        {
            if (conditions == null || !conditions.Any())
                return emptyValue;

            Expression expr = conditions.First();

            foreach (Expression condition in conditions.Skip(1))
                expr = gateFactory(expr, condition);

            return expr;
        }

        private bool IsAssignableFrom(Type left, Type right) => left.IsAssignableFrom(right);

        private bool IsRunningNetFramework() => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");

        private static BindingException GetInvalidCastException(ISchema schema, string attributeName, Exception innerException)
        {
            IBindingMetadata metadata = schema.Require<IBindingMetadata>(attributeName);

            return BindingException.InvalidCast(metadata, innerException);
        }

        #endregion

        private static class Arguments
        {
            public static ParameterExpression DataReader { get; } = Expression.Parameter(typeof(IDataReader), "dataReader");
            public static ParameterExpression Lists { get; } = Expression.Parameter(typeof(ElasticArray), "lists");
            public static ParameterExpression Aggregates { get; } = Expression.Parameter(typeof(ElasticArray), "aggregates");
            public static ParameterExpression Helpers { get; } = Expression.Parameter(typeof(ElasticArray), "helpers");
            public static ParameterExpression Schema { get; } = Expression.Parameter(typeof(ISchema), "schema");
        }
    }
}
