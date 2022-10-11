using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Jerrycurl.Diagnostics;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Relations
{
    public class Relation : IRelation
    {
        public IRelationHeader Header { get; }
        public IField Model => this.Source.Model;
        public IField Source { get; }

        IRelationReader IRelation.GetReader() => this.GetReader();

        public Relation(IField source, IRelationHeader header)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.Header = header ?? throw new ArgumentNullException(nameof(header));
        }

        public RelationReader GetReader() => new RelationReader(this);
        public DbDataReader GetDataReader(IEnumerable<string> header) => new RelationDataReader(this.GetReader(), header);
        public DbDataReader GetDataReader() => this.GetDataReader(this.Header.Attributes.Select(a => a.Identity.Name));

        public IEnumerable<ITuple> Body
        {
            get
            {
                using RelationReader reader = this.GetReader();

                while (reader.Read())
                {
                    IField[] buffer = new IField[reader.Degree];

                    reader.CopyTo(buffer, buffer.Length);

                    yield return new Tuple(buffer);
                }
            }
        }

        public override string ToString() => this.Header.ToString();


        #region " Equality "

        public bool Equals(IField other) => Equality.Combine(this.Source, other, m => m.Identity, m => m.Model);
        public override bool Equals(object obj) => (obj is IField other && this.Equals(other));
        public override int GetHashCode() => HashCode.Combine(this.Source.Identity, this.Source.Model);

        #endregion
    }
}
