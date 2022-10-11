using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Queries.Internal.IO;
using Jerrycurl.Cqs.Queries.Internal.Caching;
using Jerrycurl.Cqs.Queries.Internal.Extensions;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Collections;
using Jerrycurl.Cqs.Queries.Internal.IO.Readers;
using Jerrycurl.Cqs.Queries.Internal.IO.Writers;
using Jerrycurl.Cqs.Queries.Internal.IO.Targets;

namespace Jerrycurl.Cqs.Queries.Internal.Parsing
{
    internal class ListParser : BaseParser
    {
        public BufferCache Buffer { get; }
        public QueryType QueryType { get; }

        public ListParser(BufferCache cache, QueryType queryType)
            : base(cache?.Schema)
        {
            this.Buffer = cache ?? throw new ArgumentException(nameof(cache));
            this.QueryType = queryType;
        }

        public ListResult Parse(IEnumerable<ColumnAttribute> header)
        {
            NodeTree nodeTree = NodeParser.Parse(this.Schema, header);
            ListResult result = new ListResult(this.Schema, this.QueryType);

            this.AddWriters(result, nodeTree);
            this.AddAggregates(result, nodeTree);

            return result;
        }

        private void AddAggregates(ListResult result, NodeTree nodeTree)
        {
            foreach (Node node in nodeTree.Nodes.Where(n => n.Data != null && this.IsAggregateValue(n.Metadata)))
            {
                int bufferIndex = this.Buffer.GetAggregateIndex(node.Identity);

                AggregateWriter writer = new AggregateWriter(node)
                {
                    Attribute = new AggregateAttribute(node.Identity.Name, aggregateIndex: bufferIndex, listIndex: null),
                    Value = this.CreateDataReader(result, node)
                };

                result.Aggregates.Add(writer);
            }

            foreach (Node node in nodeTree.Items.Where(n => this.IsAggregateList(n.Metadata)))
            {
                int bufferIndex = this.Buffer.GetListIndex(node.Metadata);

                AggregateWriter writer = new AggregateWriter(node)
                {
                    Attribute = new AggregateAttribute(node.Metadata.Parent.Identity.Name, aggregateIndex: null, listIndex: bufferIndex),
                };

                result.Aggregates.Add(writer);
            }
        }

        private bool IsWriterList(IBindingMetadata metadata)
        {
            if (this.QueryType == QueryType.Aggregate)
            {
                if (metadata.HasFlag(BindingMetadataFlags.Model))
                    return false;
                else if (metadata.HasFlag(BindingMetadataFlags.Item) && metadata.Parent.HasFlag(BindingMetadataFlags.Model))
                    return false;
            }

            return true;
        }

        private bool IsAggregateList(IBindingMetadata metadata)
        {
            if (this.QueryType != QueryType.Aggregate)
                return false;
            else if (metadata.Relation.Depth == 1 && !metadata.Owner.Parent.HasFlag(BindingMetadataFlags.Model))
                return true;
            else if (metadata.Relation.Depth == 2 && metadata.Owner.Parent.Owner.Parent.HasFlag(BindingMetadataFlags.Model))
                return true;

            return false;
        }

        private bool IsAggregateValue(IBindingMetadata metadata)
        {
            if (this.QueryType != QueryType.Aggregate)
                return false;
            else if (metadata.Relation.Depth == 0)
                return true;
            else if (metadata.Relation.Depth == 1 && metadata.Owner.Parent.HasFlag(BindingMetadataFlags.Model))
                return true;

            return false;
        }

        private void AddWriters(ListResult result, NodeTree nodeTree)
        {
            foreach (Node node in nodeTree.Items.Where(n => this.IsWriterList(n.Metadata)).OrderByDescending(n => n.Depth))
            {
                TargetWriter writer = new TargetWriter()
                {
                    Source = this.CreateReader(result, node),
                };

                this.AddPrimaryKey(writer);
                this.AddChildKey(result, writer);

                result.Writers.Add(writer);
            }

            TargetWriter resultWriter = result.Writers.FirstOrDefault(w => w.Source.Metadata.Relation.Depth == 0);
            TargetWriter resultItemWriter = result.Writers.FirstOrDefault(w => w.Source.Metadata.Relation.Depth == 1);

            if (resultWriter != null && resultItemWriter != null)
                result.Writers.Remove(resultWriter);
        }

        protected override BaseReader CreateReader(BaseResult result, Node node)
        {
            BaseReader reader = base.CreateReader(result, node);

            if (reader is NewReader newReader)
                this.AddParentKeys((ListResult)result, newReader);

            return reader;
        }

        private void AddPrimaryKey(TargetWriter writer)
        {
            if (writer.Source is NewReader newReader)
            {
                writer.PrimaryKey = newReader.PrimaryKey;
                newReader.PrimaryKey = null;

                writer.ForeignJoins.AddRange(newReader.Joins);
                newReader.Joins.Clear();
            }
        }

        private ParameterExpression GetListVariable(IBindingMetadata metadata, KeyReader joinKey)
        {
            if (joinKey != null)
            {
                Type dictType = typeof(Dictionary<,>).MakeGenericType(joinKey.KeyType, typeof(ElasticArray));

                return this.GetNamedVariable(dictType, metadata.Identity);
            }
            else if (metadata.HasFlag(BindingMetadataFlags.Item))
            {
                Type listType = metadata.Parent.Composition?.Construct?.Type ?? throw BindingException.InvalidConstructor(metadata.Parent);

                return this.GetNamedVariable(listType, metadata.Identity);
            }

            return null;
        }

        private ListTarget GetListTarget(ListResult result, IBindingMetadata metadata, KeyReader joinKey)
        {
            int bufferIndex = this.Buffer.GetListIndex(metadata, joinKey?.Reference);
            metadata = joinKey?.Target ?? metadata;

            ListTarget target = result.Targets.FirstOrDefault(t => t.Index == bufferIndex);

            if (target != null)
                return target;

            target = new ListTarget()
            {
                Index = bufferIndex,
                Variable = this.GetListVariable(metadata, joinKey),
            };

            if (joinKey == null && metadata.HasFlag(BindingMetadataFlags.Item))
            {
                target.NewList = target.NewTarget = metadata.Parent.Composition?.Construct ?? throw BindingException.InvalidConstructor(metadata.Parent);
                target.AddMethod = metadata.Parent.Composition.Add;
            }

            if (joinKey != null)
                target.NewTarget = Expression.New(target.Variable.Type);

            if (target.NewTarget != null)
                result.Targets.Add(target);

            return target;
        }

        private JoinTarget GetJoinTarget(ListResult result, KeyReader joinKey)
        {
            if (joinKey == null)
                return null;

            ListTarget list = this.GetListTarget(result, joinKey.Target, joinKey);
            JoinTarget target = new JoinTarget()
            {
                Key = joinKey,
                Buffer = Expression.Variable(typeof(ElasticArray), "_joinbuf"),
                Index = this.Buffer.GetJoinIndex(joinKey.Reference),
                List = list,
            };

            if (joinKey.Target.HasFlag(BindingMetadataFlags.Item))
            {
                target.NewList = joinKey.Target.Parent.Composition.Construct;
                target.AddMethod = joinKey.Target.Parent.Composition.Add;
            }

            return target;
        }

        private void AddParentKeys(ListResult result, NewReader reader)
        {
            IEnumerable<KeyReader> joinKeys = this.GetParentReferences(reader.Metadata).Select(r => this.FindParentKey(reader, r));
            IEnumerable<KeyReader> validKeys = joinKeys.NotNull().Where(k => this.IsValidJoinKey(k));
            IEnumerable<KeyReader> newKeys = validKeys.Where(k => !this.HasJoinKeyOverride(reader, k));

            foreach (KeyReader joinKey in newKeys.DistinctBy(k => k.Target.Identity))
            {
                this.InitializeKey(joinKey);

                JoinReader join = new JoinReader(joinKey.Reference)
                {
                    Target = this.GetJoinTarget(result, joinKey),
                };

                reader.Joins.Add(join.Target);
                reader.Properties.Add(join);
            }
        }

        private void AddChildKey(ListResult result, TargetWriter writer)
        {
            IList<IReference> references = this.GetChildReferences(writer.Source.Metadata).ToList();
            KeyReader childKey = references.Select(r => this.FindChildKey(writer.Source, r)).NotNull().FirstOrDefault();

            if (childKey != null && this.IsValidJoinKey(childKey, throwOnInvalid: true))
                this.InitializeKey(childKey);

            if (childKey == null && this.RequiresReference(writer.Source.Metadata))
                throw BindingException.NoReferenceFound(writer.Source.Metadata);

            writer.List = this.GetListTarget(result, writer.Source.Metadata, childKey);
            writer.Join = this.GetJoinTarget(result, childKey);
        }

        private bool RequiresReference(IBindingMetadata metadata)
        {
            if (metadata.HasFlag(BindingMetadataFlags.Model))
                return false;
            else if (metadata.HasFlag(BindingMetadataFlags.Item) && metadata.Parent.HasFlag(BindingMetadataFlags.Model))
                return false;
            else if (this.IsAggregateList(metadata))
                return false;

            return true;
        }

        private void InitializeKey(KeyReader key)
        {
            if (key.Reference != null)
            {
                foreach (DataReader value in key.Values)
                    value.KeyType = value.Metadata.Type.GetKeyType();

                key.KeyType = CompositeKey.Create(key.Values.Select(v => v.KeyType));

                if (key.Values.Count > 1)
                    key.Variable = Expression.Variable(key.KeyType);

                if (key.Reference.HasFlag(ReferenceFlags.Self))
                    key.Reference = this.GetRecursiveReference(key.Reference);
            }

            foreach (DataReader value in key.Values)
            {
                value.IsDbNull ??= this.GetNamedVariable(typeof(bool), value, "_isnull");
                value.Variable ??= this.GetNamedVariable(value.KeyType, value);
            }
        }

        private ParameterExpression GetNamedVariable(Type type, MetadataIdentity identity, string suffix = "")
            => Expression.Variable(type, "_" + identity.Name.ToLower() + suffix);

        private ParameterExpression GetNamedVariable(Type type, BaseReader reader, string suffix = "")
            => this.GetNamedVariable(type, reader.Identity, suffix);

        private bool HasJoinKeyOverride(NewReader reader, KeyReader joinKey)
        {
            IReference childReference = joinKey.Reference.Find(ReferenceFlags.Child);
            IReferenceMetadata targetMetadata = childReference.List ?? childReference.Metadata;

            return reader.Properties.Any(r => r.Metadata.Identity.Equals(targetMetadata.Identity));
        }

        private bool IsValidJoinKey(KeyReader joinKey, bool throwOnInvalid = false)
        {
            IReferenceKey parentKey = joinKey.Reference.FindParentKey();
            IReferenceKey childKey = joinKey.Reference.FindChildKey();

            foreach (var (childValue, parentValue) in childKey.Properties.Zip(parentKey.Properties))
            {
                Type parentType = parentValue.Type.GetKeyType();
                Type childType = childValue.Type.GetKeyType();

                if (parentType != childType && throwOnInvalid)
                    throw BindingException.InvalidReference(joinKey.Reference);
                else if (parentType != childType)
                    return false;
            }

            return true;
        }

        private IReference GetRecursiveReference(IReference reference)
        {
            IReferenceMetadata metadata = reference.Metadata;

            foreach (IReference otherReference in metadata.References.Where(r => r.HasFlag(ReferenceFlags.Child) && !r.HasFlag(ReferenceFlags.Self)))
            {
                if (reference.Other.Key.Equals(otherReference.Key) || reference.Key.Equals(otherReference.Key))
                    return otherReference;
            }

            return null;
        }

        private IEnumerable<IReference> GetParentReferences(IBindingMetadata metadata)
            => this.GetValidReferences(metadata).Where(r => r.HasFlag(ReferenceFlags.Parent));

        private IEnumerable<IReference> GetChildReferences(IBindingMetadata metadata)
            => this.GetValidReferences(metadata).Where(r => r.HasFlag(ReferenceFlags.Child) && !r.HasFlag(ReferenceFlags.Self));

        private IEnumerable<IReference> GetValidReferences(IBindingMetadata metadata)
        {
            IReferenceMetadata referenceMetadata = metadata.Identity.Lookup<IReferenceMetadata>();

            if (referenceMetadata != null)
                return referenceMetadata.References.Where(IsValid).OrderBy(r => r.Priority);

            return Array.Empty<IReference>();

            bool IsValid(IReference reference)
            {
                if (!reference.HasFlag(ReferenceFlags.Many) && !reference.Other.HasFlag(ReferenceFlags.Many))
                    return false;

                IBindingMetadata metadata = reference.Find(ReferenceFlags.Child).Metadata.Identity.Lookup<IBindingMetadata>();

                if (this.QueryType == QueryType.Aggregate && this.IsAggregateList(metadata))
                    return false;

                return true;
            }
        }
    }
}
