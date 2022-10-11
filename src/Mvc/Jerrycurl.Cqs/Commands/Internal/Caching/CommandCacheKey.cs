using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Diagnostics;
using Jerrycurl.Relations.Metadata;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Cqs.Commands.Internal.Caching
{
    internal class CommandCacheKey : IEquatable<CommandCacheKey>
    {
        public IReadOnlyList<ColumnName> Columns { get; }

        public CommandCacheKey(IEnumerable<ColumnName> columns)
        {
            this.Columns = columns?.ToList() ?? throw new ArgumentNullException(nameof(columns));
        }

        public CommandCacheKey(MetadataIdentity metadata, ColumnMetadata column)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            this.Columns = new List<ColumnName>() { new ColumnName(metadata, column) };
        }

        public bool Equals(CommandCacheKey other) => Equality.CombineAll(this.Columns, other?.Columns);
        public override bool Equals(object obj) => (obj is CommandCacheKey other && this.Equals(other));
        public override int GetHashCode() => HashCode.CombineAll(this.Columns);
    }
}
