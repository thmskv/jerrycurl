using System;
using System.Linq;
using System.Linq.Expressions;
using Jerrycurl.Relations.Internal.Queues;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Relations.Internal.IO;

namespace Jerrycurl.Relations.Internal.Parsing
{
    internal class BufferParser
    {
        public BufferTree Parse(MetadataIdentity source, IRelationHeader header)
        {
            NodeTree nodeTree = NodeParser.Parse(source, header);
            BufferTree tree = new BufferTree()
            {
                Notation = new DotNotation(),
                Header = header,
            };

            this.CreateSource(nodeTree.Source, tree);

            return tree;
        }

        private void CreateSource(Node node, BufferTree tree)
        {
            SourceReader reader = new SourceReader(node);

            tree.Source = reader;

            this.AddWritersAndProperties(node, reader, tree, null);
        }

        private PropertyReader CreateProperty(Node node, BufferTree tree, QueueIndex queue)
        {
            PropertyReader reader = new PropertyReader(node);

            this.AddWritersAndProperties(node, reader, tree, queue);

            return reader;
        }

        private QueueReader CreateQueue(Node node, BufferTree tree, QueueIndex queue)
        {
            QueueReader reader = new QueueReader(node)
            {
                Index = queue,
            };

            tree.Queues.Add(reader);

            this.AddWritersAndProperties(node, reader, tree, queue);

            return reader;
        }

        private QueueIndex CreateIndex(Node node, BufferTree tree)
        {
            if (node.Metadata.HasFlag(RelationMetadataFlags.Recursive))
            {
                QueueReader anchorReader = tree.Queues.FirstOrDefault(qr => qr.Index.Item.Equals(node.Metadata.Recursor));

                return anchorReader.Index;
            }
            else if (node.Item != null)
            {
                Type queueType = typeof(RelationQueue<,>).MakeGenericType(node.Metadata.Type, node.Item.Metadata.Type);

                return new QueueIndex()
                {
                    Buffer = tree.Queues.Count,
                    Variable = Expression.Parameter(queueType, $"_queue{tree.Queues.Count}"),
                    List = node.Metadata,
                    Item = node.Item.Metadata,
                };
            }

            return null;
        }

        private void AddWritersAndProperties(Node node, NodeReader reader, BufferTree tree, QueueIndex queue)
        {
            this.AddWriters(node, reader, tree, queue);
            this.AddProperties(node, reader, tree, queue);
        }

        private void AddProperties(Node node, NodeReader reader, BufferTree tree, QueueIndex queue)
        {
            reader.Properties = node.Properties.Select(n => this.CreateProperty(n, tree, queue)).ToList();
        }

        private void AddWriters(Node node, NodeReader reader, BufferTree tree, QueueIndex queue)
        {
            if (node.Index.Count > 0)
            {
                FieldWriter writer = new FieldWriter(node)
                {
                    BufferIndex = node.Index.ToArray(),
                    NamePart = this.GetNamePart(node, queue, tree),
                    Queue = queue,
                };

                reader.Writers.Add(writer);
                tree.Fields.Add(writer);

                if (queue != null && queue.Type == RelationQueueType.Cached)
                {
                    CacheWriter cacheWriter = new CacheWriter(node)
                    {
                        BufferIndex = writer.BufferIndex.First(),
                        CacheIndex = queue.Cache.Count,
                        Queue = queue,
                    };

                    queue.Cache.Add(cacheWriter);
                }
            }

            if (node.Item != null || node.Metadata.HasFlag(RelationMetadataFlags.List | RelationMetadataFlags.Recursive))
            {
                QueueIndex prevQueue = tree.Queues.LastOrDefault()?.Index;
                QueueIndex nextQueue = this.CreateIndex(node, tree);

                QueueWriter writer = new QueueWriter(node)
                {
                    NamePart = this.GetNamePart(node.Item ?? node, queue, tree),
                    Queue = queue,
                    Next = nextQueue,
                };

                if ((node.Item ?? node).Metadata.HasFlag(RelationMetadataFlags.Recursive))
                    nextQueue.Type = RelationQueueType.Recursive;
                else if (prevQueue != null && !prevQueue.List.Identity.Equals(nextQueue.List.Owner.Parent?.Identity))
                    nextQueue.Type = RelationQueueType.Cached;
                else if (prevQueue != null && prevQueue.Type == RelationQueueType.Cached)
                    nextQueue.Type = RelationQueueType.Cached;

                reader.Writers.Add(writer);

                if (node.Item != null)
                    this.CreateQueue(node.Item, tree, nextQueue);
            }
        }

        private string GetNamePart(Node node, QueueIndex queue, BufferTree tree)
        {
            if (node.Metadata.HasFlag(RelationMetadataFlags.Recursive | RelationMetadataFlags.List))
            {
                string itemName = tree.Notation.Combine(node.Metadata.Identity.Name, tree.Notation.Member(queue.Item.Identity.Name));

                return tree.Notation.Path(queue.Item.Identity.Name, itemName);
            }
            else if (queue != null)
                return tree.Notation.Path(queue.Item.Identity.Name, node.Metadata.Identity.Name);
            else
                return tree.Notation.Path(tree.Source.Metadata.Identity.Name, node.Metadata.Identity.Name);
        }
    }
}
