using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Mvc.Metadata.Annotations;
using Jerrycurl.Cqs.Metadata.Annotations;
using Jerrycurl.Collections;

namespace Jerrycurl.Mvc.Metadata
{
    public class ProjectionMetadataBuilder : IMetadataBuilder<IProjectionMetadata>
    {
        public IProjectionMetadata GetMetadata(IMetadataBuilderContext context) => this.GetMetadata(context, context.Relation);

        private IProjectionMetadata GetMetadata(IMetadataBuilderContext context, IRelationMetadata relation)
        {
            IProjectionMetadata parent = context.GetMetadata<IProjectionMetadata>(relation.Parent.Identity.Name) ?? this.GetMetadata(context, relation.Parent);

            if (parent == null)
                return null;
            else if (parent.Item != null && parent.Item.Identity.Equals(relation.Identity))
                return parent.Item;

            return parent.Properties.FirstOrDefault(m => m.Identity.Equals(relation.Identity));
        }

        public void Initialize(IMetadataBuilderContext context) => this.GetMetadata(context, context.Relation, null);

        private Lazy<IReadOnlyList<TItem>> CreateLazy<TItem>(Func<IEnumerable<TItem>> factory) => new Lazy<IReadOnlyList<TItem>>(() => factory().ToList());

        private IEnumerable<ProjectionMetadata> CreateProperties(IMetadataBuilderContext context, ProjectionMetadata parent)
        {
            foreach (IRelationMetadata property in parent.Relation.Properties)
                yield return this.GetMetadata(context, property, parent);
        }

        private ProjectionMetadata CreateItem(IMetadataBuilderContext context, ProjectionMetadata parent)
        {
            if (parent.Relation.Item != null)
            {
                ProjectionMetadata metadata = this.GetMetadata(context, parent.Relation.Item, parent);

                metadata.List = parent;

                return metadata;
            }

            return null;
        }

        private ProjectionMetadata GetMetadata(IMetadataBuilderContext context, IRelationMetadata relation, IProjectionMetadata parent)
        {
            ProjectionMetadata metadata = new ProjectionMetadata(relation);

            context.AddMetadata<IProjectionMetadata>(metadata);

            metadata.Properties = this.CreateLazy(() => this.CreateProperties(context, metadata));
            metadata.Item = this.CreateItem(context, metadata);
            metadata.Flags = this.GetFlags(metadata);

            this.CreateTableMetadata(metadata);
            this.CreateInputMetadata(context, metadata, parent);

            return metadata;
        }

        private void CreateInputMetadata(IMetadataBuilderContext context, ProjectionMetadata metadata, IProjectionMetadata parent)
        {
            if (parent?.Reference != null)
            {
                IEnumerable<IReference> references = parent.Reference.References.Where(r => r.HasFlag(ReferenceFlags.Foreign) && !r.HasFlag(ReferenceFlags.Self));

                foreach (IReference reference in references.OrderBy(r => r.Priority))
                {
                    int valueIndex = reference.Key.Properties.IndexOf(m => m.Identity.Equals(metadata.Identity));

                    if (valueIndex > -1 && !reference.Other.Metadata.Relation.HasFlag(RelationMetadataFlags.Recursive))
                    {
                        IReferenceMetadata valueMetadata = reference.Other.Key.Properties[valueIndex];

                        metadata.Input = new Lazy<IProjectionMetadata>(() => this.GetMetadata(context, valueMetadata.Relation));
                        metadata.Flags |= ProjectionMetadataFlags.Cascade;

                        return;
                    }
                }
            }

            metadata.Input = new Lazy<IProjectionMetadata>(() => metadata);
        }

        private void CreateTableMetadata(ProjectionMetadata metadata)
        {
            ITableMetadata table = metadata.Identity.Lookup<ITableMetadata>();

            if (table != null)
            {
                metadata.Table = table.HasFlag(TableMetadataFlags.Table) ? table : table.Owner;
                metadata.Column = table.HasFlag(TableMetadataFlags.Column) ? table : null;
            }
        }

        private ProjectionMetadataFlags GetFlags(ProjectionMetadata metadata)
        {
            IdAttribute id = metadata.Relation.Annotations?.OfType<IdAttribute>().FirstOrDefault();
            OutAttribute out0 = metadata.Relation.Annotations?.OfType<OutAttribute>().FirstOrDefault();
            InAttribute in0 = metadata.Relation.Annotations?.OfType<InAttribute>().FirstOrDefault();

            IReferenceMetadata reference = metadata.Identity.Lookup<IReferenceMetadata>();
            ProjectionMetadataFlags flags = ProjectionMetadataFlags.None;

            if (id != null)
                flags |= ProjectionMetadataFlags.Identity;

            if (in0 != null || out0 != null)
            {
                flags |= in0 != null ? ProjectionMetadataFlags.Input : ProjectionMetadataFlags.None;
                flags |= out0 != null ? ProjectionMetadataFlags.Output : ProjectionMetadataFlags.None;
            }
            else if (id != null)
                flags |= ProjectionMetadataFlags.Output;
            else if (reference != null && reference.HasAnyFlag(ReferenceMetadataFlags.Key))
                flags |= ProjectionMetadataFlags.Input | ProjectionMetadataFlags.Output;
            else
                flags |= ProjectionMetadataFlags.Input;

            return flags;
        }
    }
}
