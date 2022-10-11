using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Vendors.SqlServer.Internal;
using BindingException = Jerrycurl.Cqs.Metadata.BindingException;
#if NET20_BASE
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;
#else
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
#endif

namespace Jerrycurl.Vendors.SqlServer
{
    public class TableValuedParameter : IParameter
    {
        public string Name { get; }
        public IRelation Relation { get; }

        IField IParameter.Source => null;

        public TableValuedParameter(string name, IRelation relation)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Relation = relation ?? throw new ArgumentNullException(nameof(relation));
        }

        public void Build(IDbDataParameter adoParameter)
        {
            SqlParameter tableParam = adoParameter as SqlParameter ?? throw new InvalidOperationException("Table-valued parameters are only supported on SqlParameter instances.");

            tableParam.ParameterName = this.Name;

            Action<SqlParameter, IRelation> binder = TvpCache.Binders.GetOrAdd(this.Relation.Header, key =>
            {
                GetHeadingMetadata(key, out IBindingMetadata[] bindingMetadata, out ITableMetadata[] columnMetadata);

                ITableMetadata tableMetadata = columnMetadata[0].HasFlag(TableMetadataFlags.Table) ? columnMetadata[0] : columnMetadata[0].Owner;

                string tvpName = string.Join(".", tableMetadata.TableName);
                string[] columnNames = columnMetadata.Select(m => m.ColumnName).ToArray();
                BindingParameterConverter[] converters = bindingMetadata.Select(m => m?.Parameter?.Convert).ToArray();

                return (sp, r) => BindParameter(sp, tvpName, columnNames,  converters, bindingMetadata, r);
            });

            binder(tableParam, this.Relation);
        }

        private static void GetHeadingMetadata(IRelationHeader header, out IBindingMetadata[] bindingMetadata, out ITableMetadata[] columnMetadata)
        {
            bindingMetadata = new IBindingMetadata[header.Attributes.Count];
            columnMetadata = new ITableMetadata[header.Attributes.Count];

            for (int i = 0; i < header.Attributes.Count; i++)
            {
                IBindingMetadata bindingEntry = header.Attributes[i].Identity.Require<IBindingMetadata>();
                ITableMetadata tableEntry = header.Attributes[i].Identity.Require<ITableMetadata>();

                bindingMetadata[i] = bindingEntry;
                columnMetadata[i] = tableEntry;
            }

            if (bindingMetadata.Length == 0)
                throw new InvalidOperationException("No columns found.");
        }

        private static void BindParameter(SqlParameter tableParam, string tvpName, string[] columnNames, BindingParameterConverter[] converters, IBindingMetadata[] metadata, IRelation relation)
        {
            SqlMetaData[] tableHeader = InferSqlMetaDataHeader(metadata, relation, columnNames);

            if (tableHeader == null)
                tableParam.Value = null;
            else
            {
                SqlDataRecord dataRecord = new SqlDataRecord(tableHeader);

                tableParam.Value = valueEnumerator();

                IEnumerable<SqlDataRecord> valueEnumerator()
                {
                    using IRelationReader reader = relation.GetReader();

                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.Degree; i++)
                        {
                            object value = reader[i].Snapshot;

                            if (converters[i] != null)
                                value = converters[i](value);

                            dataRecord.SetValue(i, value);
                        }

                        yield return dataRecord;
                    }
                }
            }

            tableParam.SqlDbType = SqlDbType.Structured;
            tableParam.TypeName = tvpName;
        }

        private static SqlMetaData[] InferSqlMetaDataHeader(IBindingMetadata[] metadata, IRelation relation, string[] columnNames)
        {
            using IRelationReader reader = relation.GetReader();

            if (reader.Read())
            {
                SqlMetaData[] header = new SqlMetaData[reader.Degree];

                for (int i = 0; i < metadata.Length; i++)
                {
                    Parameter param = new Parameter("P", reader[i]);
                    SqlParameter sqlParam = new SqlParameter();

                    param.Build(sqlParam);

                    header[i] = InferSqlMetaData(metadata[i], sqlParam, columnNames[i]);
                }

                return header;
            }

            return null;
        }

        private static SqlMetaData InferSqlMetaData(IBindingMetadata metadata, SqlParameter sqlParam, string columnName)
        {
            switch (sqlParam.SqlDbType)
            {
                case SqlDbType.Bit:
                case SqlDbType.TinyInt:
                case SqlDbType.Float:
                case SqlDbType.SmallInt:
                case SqlDbType.Int:
                case SqlDbType.BigInt:
                case SqlDbType.Real:
                case SqlDbType.Date:
                case SqlDbType.Xml:
                case SqlDbType.Variant:
                    return new SqlMetaData(columnName, sqlParam.SqlDbType);
                case SqlDbType.NVarChar:
                case SqlDbType.VarChar:
                case SqlDbType.VarBinary:
                case SqlDbType.NChar:
                case SqlDbType.Text:
                case SqlDbType.NText:
                case SqlDbType.Char:
                    return new SqlMetaData(columnName, sqlParam.SqlDbType, -1);
                case SqlDbType.Decimal:
                    return new SqlMetaData(columnName, SqlDbType.Decimal, 38, 19);
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.DateTimeOffset:
                case SqlDbType.Time:
                    return new SqlMetaData(columnName, sqlParam.SqlDbType, 0, 7);
                default:
                    {
                        try
                        {
                            return new SqlMetaData(columnName, sqlParam.SqlDbType);
                        }
                        catch (Exception ex)
                        {
                            throw BindingException.Create(metadata, $"Cannot create TVP value from type {sqlParam.SqlDbType}.", ex);
                        }
                    }
            }
        }
    }
}