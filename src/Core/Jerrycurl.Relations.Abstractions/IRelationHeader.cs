using System;
using System.Collections.Generic;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations;

public interface IRelationHeader : IEquatable<IRelationHeader>
{
    public ISchema Schema { get; }
    public IReadOnlyList<IRelationMetadata> Attributes { get; }
    public int Degree { get; }
}
