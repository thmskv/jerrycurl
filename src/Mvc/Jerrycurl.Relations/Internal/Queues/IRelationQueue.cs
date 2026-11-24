using System;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Queues;

internal interface IRelationQueue : IDisposable
{
    public bool Read();
    public IRelationMetadata Metadata { get; }
}
