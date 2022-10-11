using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Queries.Internal.Caching;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Queries.Internal.Parsing
{
    internal static class NodeParser
    {
        public static NodeTree Parse(ISchema schema, IEnumerable<DataAttribute> header)
        {
            NodeTree tree = new NodeTree(schema);

            foreach (DataAttribute attribute in header)
                AddDataNode(tree, attribute);

            return tree;
        }

        private static void AddDataNode(NodeTree tree, DataAttribute attribute)
        {
            MetadataIdentity identity = new MetadataIdentity(tree.Schema, attribute.Name);
            IBindingMetadata metadata = identity.Lookup<IBindingMetadata>() ?? FindDynamicMetadata(identity);

            if (metadata != null)
            {
                Node node;

                if (metadata.HasFlag(BindingMetadataFlags.Dynamic))
                    node = AddDynamicNode(tree, identity, metadata);
                else
                    node = AddStaticNode(tree, metadata);

                if (node != null)
                {
                    node.Data = attribute;

                    tree.Data.Add(node);
                }
                    
            }
        }

        private static Node AddDynamicNode(NodeTree tree, MetadataIdentity identity, IBindingMetadata metadata)
        {
            AddStaticNode(tree, metadata);

            Node thisNode = tree.FindNode(identity);
            MetadataIdentity parentIdentity = identity.Pop();

            if (thisNode != null)
                return thisNode;
            else if (parentIdentity != null)
            {
                Node parentNode = tree.FindNode(parentIdentity) ?? AddDynamicNode(tree, parentIdentity, metadata);

                if (parentNode != null)
                {
                    thisNode = new Node(identity, metadata)
                    {
                        IsDynamic = true,
                    };

                    parentNode.Properties.Add(thisNode);
                    tree.Nodes.Add(thisNode);
                }
            }

            return thisNode;
        }

        private static Node AddStaticNode(NodeTree tree, IBindingMetadata metadata)
        {
            Node thisNode = tree.FindNode(metadata);

            if (thisNode != null)
                return thisNode;
            else if (metadata.HasFlag(BindingMetadataFlags.Item) || HasValidReference(metadata))
            {
                thisNode = new Node(metadata)
                {
                    IsDynamic = metadata.HasFlag(BindingMetadataFlags.Dynamic),
                };

                tree.Nodes.Add(thisNode);
                tree.Items.Add(thisNode);
            }
            else if (metadata.HasFlag(BindingMetadataFlags.Model))
            {
                thisNode = new Node(metadata)
                {
                    IsDynamic = metadata.HasFlag(BindingMetadataFlags.Dynamic),
                };

                tree.Nodes.Add(thisNode);
                tree.Items.Add(thisNode);
            }
            else
            {
                Node parentNode = tree.FindNode(metadata.Parent) ?? AddStaticNode(tree, metadata.Parent);

                if (parentNode != null)
                {
                    thisNode = new Node(metadata)
                    {
                        IsDynamic = metadata.HasFlag(BindingMetadataFlags.Dynamic),
                    };

                    parentNode.Properties.Add(thisNode);
                    tree.Nodes.Add(thisNode);
                }
            }

            return thisNode;
        }

        private static bool HasValidReference(IBindingMetadata metadata)
        {
            IReferenceMetadata referenceMetadata = metadata.Identity.Lookup<IReferenceMetadata>();

            if (referenceMetadata != null)
            {
                IEnumerable<IReference> childReferences = referenceMetadata.References.Where(r => r.HasFlag(ReferenceFlags.Child));

                return childReferences.Any(r => r.HasFlag(ReferenceFlags.Many) || r.Other.HasFlag(ReferenceFlags.Many));
            }

            return false;
        }

        private static IBindingMetadata FindDynamicMetadata(MetadataIdentity identity)
        {
            IBindingMetadata metadata = identity.Lookup<IBindingMetadata>();

            while (metadata == null && (identity = identity.Pop()) != null)
                metadata = identity.Lookup<IBindingMetadata>();

            if (metadata != null && metadata.HasFlag(BindingMetadataFlags.Dynamic))
                return metadata;

            return null;
        }
    }
}
