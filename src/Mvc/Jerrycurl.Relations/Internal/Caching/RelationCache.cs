using System.Collections.Concurrent;
using Jerrycurl.Relations.Internal.Compilation;
using Jerrycurl.Relations.Internal.Queues;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Relations.Internal.Parsing;

namespace Jerrycurl.Relations.Internal.Caching
{
    internal static class RelationCache
    {
        private readonly static ConcurrentDictionary<RelationCacheKey, BufferWriter> cache = new ConcurrentDictionary<RelationCacheKey, BufferWriter>();

        public static RelationBuffer CreateBuffer(IRelation relation)
        {
            BufferWriter writer = GetWriter(relation.Source.Identity.Metadata, relation.Header);

            return new RelationBuffer()
            {
                Writer = writer,
                Queues = new IRelationQueue[writer.Queues.Length],
                Fields = new IField[relation.Header.Attributes.Count],
                Model = relation.Source.Model,
                Source = relation.Source,
            };
        }

        private static BufferWriter GetWriter(MetadataIdentity source, IRelationHeader header)
        {
            RelationCacheKey key = new RelationCacheKey(source, header);

            return cache.GetOrAdd(key, _ =>
            {
                BufferParser parser = new BufferParser();
                BufferTree tree = parser.Parse(source, header);
                RelationCompiler compiler = new RelationCompiler();

                return compiler.Compile(tree);
            });
        }
    }
}
