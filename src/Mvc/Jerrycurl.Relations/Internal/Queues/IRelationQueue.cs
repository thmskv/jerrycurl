using System;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Queues
{
    internal interface IRelationQueue : IDisposable
    {
        bool Read();
        IRelationMetadata Metadata { get; }
    }
}
