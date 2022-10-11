using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Collections;
using Jerrycurl.Cqs.Commands.Internal;
using Jerrycurl.Cqs.Commands.Internal.Caching;
using Jerrycurl.Cqs.Commands.Internal.Compilation;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Commands
{
    public sealed class CommandBuffer
    {
        private readonly Dictionary<IField, FieldBuffer> fieldBuffers = new Dictionary<IField, FieldBuffer>();
        private readonly Dictionary<string, FieldBuffer> columnHeader = new Dictionary<string, FieldBuffer>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FieldBuffer> paramHeader = new Dictionary<string, FieldBuffer>(StringComparer.OrdinalIgnoreCase);

        public ISchemaStore Store { get; }

        public CommandBuffer()
        {

        }

        public CommandBuffer(ISchemaStore store)
        {
            this.Store = store;
        }

        public async Task UpdateAsync(DbDataReader dataReader, CancellationToken cancellationToken = default)
        {
            Action<IDataReader> updateAction = this.GetUpdateAction(dataReader);

            while (await dataReader.ReadAsync(cancellationToken))
                updateAction(dataReader);
        }

        public void Update(IDataReader dataReader)
        {
            Action<IDataReader> updateAction = this.GetUpdateAction(dataReader);

            while (dataReader.Read())
                updateAction(dataReader);
        }

        private Action<IDataReader> GetUpdateAction(IDataReader dataReader)
        {
            List<ColumnName> names = new List<ColumnName>();
            List<FieldBuffer> bufferList = new List<FieldBuffer>();

            for (int i = 0; i < this.GetFieldCount(dataReader); i++)
            {
                string columnName = dataReader.GetName(i);

                if (this.columnHeader.TryGetValue(columnName, out FieldBuffer buffer))
                {
                    MetadataIdentity metadata = buffer.Target.Identity.Metadata;
                    ColumnMetadata columnInfo = GetColumnInfo(i);

                    names.Add(new ColumnName(metadata, columnInfo));
                    bufferList.Add(buffer);

                    buffer.Column.Metadata = columnInfo;
                }
            }

            BufferWriter writer = CommandCache.GetWriter(names);
            FieldBuffer[] buffers = bufferList.ToArray();

            return dr => writer(dr, buffers);

            ColumnMetadata GetColumnInfo(int i) => new ColumnMetadata(dataReader.GetName(i), dataReader.GetFieldType(i), dataReader.GetDataTypeName(i), i);
        }

        private void FlushParameters()
        {
            this.paramHeader.Clear();
        }

        public IList<IDbDataParameter> Prepare(IDbCommand adoCommand)
            => this.Prepare(() => adoCommand.CreateParameter());

        public IList<IDbDataParameter> Prepare(Func<IDbDataParameter> parameterFactory)
        {
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            HashSet<string> paramSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (FieldBuffer buffer in this.paramHeader.Values)
            {
                IDbDataParameter adoParam = buffer.Parameter.AdoParameter = parameterFactory();

                adoParam.ParameterName = buffer.Parameter.Name;
                buffer.Parameter.Parameter?.Build(adoParam);

                if (buffer.Column != null)
                    SetParameterDirection(adoParam, ParameterDirection.Input);
                else if (buffer.Parameter.HasSource && buffer.Parameter.HasTarget)
                    SetParameterDirection(adoParam, ParameterDirection.InputOutput);
                else if (buffer.Parameter.HasTarget)
                {
                    adoParam.Value = DBNull.Value;

                    SetParameterDirection(adoParam, ParameterDirection.Output);
                }

                if (this.TryReadValue(buffer.Parameter.Parameter?.Source, out object newValue))
                    adoParam.Value = newValue;

                if (!paramSet.Contains(adoParam.ParameterName))
                {
                    parameters.Add(adoParam);
                    paramSet.Add(adoParam.ParameterName);
                }
            }

            this.FlushParameters();

            return parameters;

            static void SetParameterDirection(IDbDataParameter adoParameter, ParameterDirection direction)
            {
                try
                {
                    adoParameter.Direction = direction;
                }
                catch (ArgumentException) { }
            }
        }

        private bool TryReadValue(IField field, out object value)
        {
            value = null;

            if (field == null)
                return false;
            else if (this.fieldBuffers.TryGetValue(field, out FieldBuffer buffer))
                return buffer.Read(out value);

            return false;
        }

        internal FieldBuffer GetBuffer(IField target) => this.fieldBuffers.GetValueOrDefault(target);
        internal IEnumerable<IFieldSource> GetSources(IField target) => this.GetBuffer(target)?.GetSources() ?? Array.Empty<IFieldSource>();
        internal IEnumerable<IFieldSource> GetChanges(IField target) => this.GetBuffer(target)?.GetChanges() ?? Array.Empty<IFieldSource>();

        private int GetFieldCount(IDataReader dataReader)
        {
            try { return dataReader.FieldCount; }
            catch { return 0; }
        }

        public void Commit()
        {
            foreach (FieldBuffer buffer in this.fieldBuffers.Values)
                buffer.Bind();
        }

        public void Add(IParameter parameter, IField target)
        {
            this.Add(parameter);
            this.Add(new ParameterBinding(target, parameter.Name));
        }

        public void Add(IEnumerable<IParameter> parameters)
        {
            foreach (IParameter parameter in parameters ?? Array.Empty<IParameter>())
                this.Add(parameter);
        }
        public void Add(IParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            FieldBuffer buffer = this.paramHeader.GetOrAdd(parameter.Name);

            buffer.Parameter ??= new ParameterSource();
            buffer.Parameter.Parameter = parameter;
            buffer.Parameter.Name = parameter.Name;

            this.paramHeader.TryAdd(parameter.Name, buffer);
        }

        public void Add(IUpdateBinding binding)
        {
            switch (binding)
            {
                case ColumnBinding columnBinding:
                    this.Add(columnBinding);
                    break;
                case ParameterBinding paramBinding:
                    this.Add(paramBinding);
                    break;
                case CascadeBinding cascadeBinding:
                    this.Add(cascadeBinding);
                    break;
                case null:
                    throw new ArgumentNullException(nameof(binding));
                default:
                    throw CommandException.InvalidBindingType(binding);
            }
        }

        public void Add(CascadeBinding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            FieldBuffer buffer = this.fieldBuffers.GetOrAdd(binding.Target);

            buffer.Cascade = new CascadeSource(binding, this);
            buffer.Target = binding.Target;
        }

        public void Add(ColumnBinding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            FieldBuffer buffer = this.fieldBuffers.GetOrAdd(binding.Target);

            buffer.Column ??= new ColumnSource();
            buffer.Target = binding.Target;

            this.columnHeader.TryAdd(binding.ColumnName, buffer);
        }

        public void Add(ParameterBinding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            FieldBuffer buffer = this.fieldBuffers.GetOrAdd(binding.Target);
            FieldBuffer paramBuffer = this.paramHeader.GetOrAdd(binding.ParameterName);

            if (buffer != paramBuffer)
                this.paramHeader[binding.ParameterName] = buffer;

            buffer.Parameter = paramBuffer.Parameter ?? new ParameterSource();
            buffer.Parameter.HasTarget = true;
            buffer.Parameter.Name = binding.ParameterName;
            buffer.Target = binding.Target;
        }
    }
}
