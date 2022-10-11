using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Language
{
    public static class RelationExtensions
    {
        public static RelationHeader Select(this ISchema schema, IEnumerable<string> header)
        {
            if (header == null)
                throw new ArgumentException(nameof(header));

            IReadOnlyList<IRelationMetadata> attributes = header.Select(a => schema.Require<IRelationMetadata>(a)).ToList();

            return new RelationHeader(schema, attributes);
        }

        public static RelationHeader Select(this ISchema schema, params string[] header)
            => schema.Select((IEnumerable<string>)header);

        public static IRelation Select(this IField source, IEnumerable<string> header)
            => new Relation(source, source.Identity.Schema.Select(header));

        public static IRelation Select(this IField source, params string[] header)
            => new Relation(source, source.Identity.Schema.Select(header));

        public static ITuple Lookup(this IField source, IEnumerable<string> header)
            => source.Select(header).Body.FirstOrDefault();

        public static ITuple Lookup(this IField source, params string[] header)
            => source.Select(header).Body.FirstOrDefault();

        public static IField Lookup(this IField source, string attributeName)
        {
            IRelation relation = source.Select(attributeName);

            using IRelationReader reader = relation.GetReader();

            if (reader.Read())
                return reader[0];

            return null;
        }

        public static IRelation From(this RelationHeader header, object model)
            => header.From(new Model(header.Schema, model));

        public static IRelation From(this RelationHeader header, IField source)
            => new Relation(source, header);

        public static IField From<TModel>(this ISchemaStore store, TModel model)
            => new Model(store.GetSchema(typeof(TModel)), model);

        public static RelationHeader<TModel> As<TModel>(this ISchema schema)
            => new RelationHeader<TModel>(schema);

        public static ISchema GetSchema<TModel>(this ISchemaStore store)
            => store.GetSchema(typeof(TModel));

        public static RelationHeader<TModel> For<TModel>(this ISchemaStore store)
            => new RelationHeader<TModel>(store.GetSchema(typeof(TModel)));

        public static void Update<T>(this IField field, Func<T, T> valueFactory)
            => field.Update(valueFactory((T)field.Snapshot));

        public static void Update<T>(this IField field, T value)
            => field.Update(value);

        public static TMetadata Lookup<TModel, TMetadata>(this ISchemaStore store)
            where TMetadata : IMetadata
            => store.GetSchema(typeof(TModel)).Lookup<TMetadata>();

        public static TMetadata Lookup<TModel, TMetadata>(this ISchemaStore store, string name)
            where TMetadata : IMetadata
            => store.GetSchema(typeof(TModel)).Lookup<TMetadata>(name);

        public static IField Lookup<TModel>(this ISchemaStore store, TModel model, string attributeName)
            => new Model(store.GetSchema(typeof(TModel)), model).Lookup(attributeName);

        public static ITuple Lookup<TModel>(this ISchemaStore store, TModel model, IEnumerable<string> header)
            => new Model(store.GetSchema(typeof(TModel)), model).Lookup(header);

        public static ITuple Lookup<TModel>(this ISchemaStore store, TModel model, params string[] header)
            => new Model(store.GetSchema(typeof(TModel)), model).Lookup(header);
    }
}
