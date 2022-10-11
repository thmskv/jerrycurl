using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Language
{
    public sealed class RelationHeader<TSource> : RelationHeader
    {
        public IRelationMetadata Source { get; }

        public RelationHeader(ISchema schema)
            : base(schema, Array.Empty<IRelationMetadata>())
        {
            this.Source = schema.Model;
        }

        public RelationHeader(ISchema schema, IReadOnlyList<IRelationMetadata> attributes)
            : base(schema, attributes)
        {
            this.Source = schema.Model;
        }

        public RelationHeader(IRelationMetadata source, IReadOnlyList<IRelationMetadata> attributes)
            : base(source?.Schema, attributes)
        {
            this.Source = source;
        }

        public RelationHeader<TSource> Select() => this.Select(m => m);
        public RelationHeader<TSource> Select<TTarget>(Expression<Func<TSource, TTarget>> expression)
        {
            MetadataIdentity newIdentity = this.Source.Identity.Push(this.Schema.Notation.Lambda(expression));
            IRelationMetadata metadata = newIdentity.Lookup<IRelationMetadata>();

            return new RelationHeader<TSource>(this.Source, this.Add(metadata));
        }

        public RelationHeader<TSource> SelectAll(Func<IRelationMetadata, bool> selector) => this.SelectAll(m => m, selector);
        public RelationHeader<TSource> SelectAll() => this.SelectAll(m => m);
        public RelationHeader<TSource> SelectAll<TTarget>(Expression<Func<TSource, TTarget>> expression) => this.SelectAll(expression, m => true);
        public RelationHeader<TSource> SelectAll<TTarget>(Expression<Func<TSource, TTarget>> expression, Func<IRelationMetadata, bool> selector)
        {
            MetadataIdentity sourceIdentity = this.Source.Identity.Push(this.Schema.Notation.Lambda(expression));
            IReadOnlyList<IRelationMetadata> metadata = sourceIdentity.Lookup<IRelationMetadata>().Properties;

            return new RelationHeader<TSource>(this.Source, this.Add(metadata.Where(selector)));
        }

        public RelationHeader<TTarget> Join<TTarget>(Expression<Func<TSource, IEnumerable<TTarget>>> expression)
        {
            MetadataIdentity newIdentity = this.Source.Identity.Push(this.Schema.Notation.Lambda(expression));
            IRelationMetadata metadata = newIdentity.Lookup<IRelationMetadata>();

            return new RelationHeader<TTarget>(metadata.Item, this.Attributes);
        }

        private IReadOnlyList<IRelationMetadata> Add(IRelationMetadata attribute)
            => this.Attributes.Append(attribute).ToList();

        private IReadOnlyList<IRelationMetadata> Add(IEnumerable<IRelationMetadata> attributes)
            => this.Attributes.Concat(attributes).ToList();
    }
}
