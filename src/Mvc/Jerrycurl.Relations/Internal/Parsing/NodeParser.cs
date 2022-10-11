using System;
using System.Linq;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Parsing
{
    internal static class NodeParser
    {
        public static NodeTree Parse(MetadataIdentity source, IRelationHeader header)
        {
            NodeTree tree = new NodeTree();

            IRelationMetadata sourceMetadata = GetMetadata(source);

            tree.Source = AddSourceNode(tree, sourceMetadata);

            for (int i = 0; i < header.Degree; i++)
                AddNode(tree, header.Attributes[i], index: i);

            if (tree.Unreachable.Any())
                throw RelationException.Unreachable(source, header, tree.Unreachable);

            return tree;
        }

        private static IRelationMetadata GetMetadata(MetadataIdentity identity)
        {
            IRelationMetadata metadata = identity.Lookup<IRelationMetadata>();

            if (metadata == null)
                throw new InvalidOperationException(identity.Name + " not found");

            return metadata;
        }

        private static Node AddSourceNode(NodeTree tree, IRelationMetadata metadata)
        {
            Node sourceNode = new Node(metadata);

            tree.Nodes.Add(sourceNode);

            return sourceNode;
        }
        private static Node AddNode(NodeTree tree, IRelationMetadata metadata, int? index = null)
        {
            Node thisNode = tree.FindNode(metadata);

            if (thisNode == null && metadata.Parent == null)
                return null;
            else if (thisNode == null)
            {
                Node parentNode = tree.FindNode(metadata.Parent) ?? AddNode(tree, metadata.Parent);

                if (parentNode != null)
                {
                    thisNode = new Node(metadata);

                    if (thisNode.Metadata.HasFlag(RelationMetadataFlags.Item))
                        parentNode.Item = thisNode;
                    else
                        parentNode.Properties.Add(thisNode);

                    tree.Nodes.Add(thisNode);

                    if (thisNode.Metadata.HasFlag(RelationMetadataFlags.Recursive | RelationMetadataFlags.Item))
                        AddNode(tree, thisNode.Metadata.Recursor);
                }
                else if (index != null)
                    tree.Unreachable.Add(metadata);
            }

            if (thisNode != null && index != null)
                thisNode.Index.Add(index.Value);

            return thisNode;
        }
    }
}
