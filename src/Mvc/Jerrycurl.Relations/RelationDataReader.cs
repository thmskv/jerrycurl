using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Jerrycurl.Relations
{
    internal class RelationDataReader : DbDataReader
    {
        public RelationReader InnerReader { get; }
        public IReadOnlyList<string> Header { get; }

        private Dictionary<string, int> headerMap;

        public RelationDataReader(RelationReader innerReader, IEnumerable<string> header)
        {
            this.InnerReader = innerReader ?? throw new ArgumentNullException(nameof(innerReader));
            this.Header = header?.ToList() ?? throw new ArgumentNullException(nameof(header));

            this.InitializeHeader();
        }

        private void InitializeHeader()
        {
            if (this.Header.Count != this.InnerReader.Degree)
                throw RelationException.InvalidDataReaderHeader(this.InnerReader.Relation.Header, this.Header);

            this.headerMap = new Dictionary<string, int>();

            for (int i = 0; i < this.Header.Count; i++)
            {
                if (this.Header[i] == null)
                    throw RelationException.DataHeaderCannotBeNull(this.InnerReader.Relation.Header, i);

                if (this.headerMap.ContainsKey(this.Header[i]))
                    throw RelationException.DataHeaderCannotHaveDupes(this.InnerReader.Relation.Header, this.Header, i);

                this.headerMap.Add(this.Header[i], i);
            }
        }

        public override int Depth => 0;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override int FieldCount => this.InnerReader.Degree;

        public override object this[string name] => this[this.GetOrdinal(name)];
        public override object this[int i]
        {
            get
            {
                if (this.InnerReader[i].Type == FieldType.Missing)
                    return DBNull.Value;

                return this.InnerReader[i].Snapshot ?? DBNull.Value;
            }
        }

        public override void Close() { }
        public override bool NextResult() => false;
        public override bool Read() => this.InnerReader.Read();
        public override bool HasRows => true;

        public override string GetDataTypeName(int i) => null;
        public override Type GetFieldType(int i) => this.InnerReader.Relation.Header.Attributes[i].Type;
        public override string GetName(int i) => this.Header[i];
        public override int GetOrdinal(string name) => this.headerMap[name];
        public override T GetFieldValue<T>(int i)
        {
            if (this.IsDBNull(i))
                throw new InvalidOperationException("Data is null.");

            return (T)this[i];
        }

        public override bool IsDBNull(int i)
        {
            if (this.InnerReader[i].Type == FieldType.Missing)
                return true;
            else if (this.InnerReader[i].Snapshot == null)
                return true;

            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                this.InnerReader?.Dispose();
        }
        #region " Get methods "

        public override float GetFloat(int i) => this.GetFieldValue<float>(i);
        public override Guid GetGuid(int i) => this.GetFieldValue<Guid>(i);
        public override short GetInt16(int i) => this.GetFieldValue<short>(i);
        public override int GetInt32(int i) => this.GetFieldValue<int>(i);
        public override long GetInt64(int i) => this.GetFieldValue<long>(i);
        public override string GetString(int i) => this.GetFieldValue<string>(i);
        public override object GetValue(int i) => this[i];
        public override bool GetBoolean(int i) => this.GetFieldValue<bool>(i);
        public override byte GetByte(int i) => this.GetFieldValue<byte>(i);
        public override char GetChar(int i) => this.GetFieldValue<char>(i);
        public override DateTime GetDateTime(int i) => this.GetFieldValue<DateTime>(i);
        public override decimal GetDecimal(int i) => this.GetFieldValue<decimal>(i);
        public override double GetDouble(int i) => this.GetFieldValue<double>(i);
        public override int GetValues(object[] values)
        {
            int maxLength = Math.Min(values.Length, this.FieldCount);

            for (int i = 0; i < maxLength; i++)
                values[i] = this[i];

            return maxLength;
        }

        #endregion

        #region " Not supported "
        public override DataTable GetSchemaTable() => throw new NotSupportedException();
        public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public override IEnumerator GetEnumerator() => throw new NotSupportedException();
        #endregion
    }
}
