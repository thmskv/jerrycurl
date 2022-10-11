using System;
using Jerrycurl.Cqs.Metadata;

namespace Jerrycurl.Cqs.Commands.Internal
{
    internal class ColumnSource : IFieldSource
    {
        public ColumnMetadata Metadata { get; set; }
        public object Value { get; set; } = DBNull.Value;
        public bool HasChanged { get; set; }
    }
}
