using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jerrycurl.Collections;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;

namespace Jerrycurl.Mvc.Sql
{
    public static class ValueExtensions
    {
        public static IProjectionValues<TModel> Vals<TModel>(this IProjection<TModel> projection, int batchIndex = -1)
        {
            if (projection.Data == null)
                throw ProjectionException.ValueNotFound(projection.Metadata);
            else if (projection.Data.Source.Snapshot == null)
            {
                IEnumerable<IProjection<TModel>> emptyItems = Array.Empty<IProjection<TModel>>();

                return new ProjectionValues<TModel>(projection.Context, projection.Identity, emptyItems, batchIndex);
            }
                
            IProjectionMetadata[] header = new[] { projection.Metadata }.Concat(projection.Header.Select(a => a.Metadata)).ToArray();
            IProjectionAttribute[] attributes = header.Skip(1).Select(m => new ProjectionAttribute(projection.Identity, projection.Context, m, data: null)).ToArray();

            return new ProjectionValues<TModel>(projection.Context, projection.Identity, innerReader(), batchIndex);

            IEnumerable<IProjection<TModel>> innerReader()
            {
                using ProjectionReader reader = new ProjectionReader(projection.Data.Source, header);

                while (reader.Read())
                {
                    IProjectionData[] dataSet = reader.GetData().ToArray();

                    if (dataSet[0].Source.Snapshot != null)
                    {
                        IEnumerable<IProjectionAttribute> valueHeader = attributes.Zip(dataSet.Skip(1)).Select(t => t.First.With(data: t.Second));

                        yield return projection.With(data: dataSet[0], header: valueHeader);
                    }
                }
            }
        }

        public static IProjection Val(this IProjection projection)
        {
            if (projection.Data == null)
                throw ProjectionException.ValueNotFound(projection.Metadata);

            IProjectionData newData = ProjectionData.Resolve(projection.Data, projection.Metadata);

            if (newData.Source.Snapshot == null)
                throw ProjectionException.ValueNotFound(newData.Source);

            return projection.With(data: newData);
        }

        public static IProjectionAttribute ValList(this IProjection projection, Func<IProjectionAttribute, IProjectionAttribute> itemFactory)
        {
            if (projection.Data == null)
                throw ProjectionException.ValueNotFound(projection.Metadata);

            IProjectionMetadata itemMetadata = projection.Metadata?.Item ?? projection.Metadata;

            using ProjectionReader reader = new ProjectionReader(projection.Data.Source, new[] { itemMetadata });

            IProjectionAttribute attribute = new ProjectionAttribute(projection.Identity, projection.Context, itemMetadata, data: null);

            if (reader.Read())
            {
                IProjectionData data = reader.GetData().First();

                attribute = itemFactory(attribute.With(data: data));
            }

            while (reader.Read())
            {
                IProjectionData data = reader.GetData().First();

                attribute = attribute.Append(", ");
                attribute = itemFactory(attribute.With(data: data));
            }

            return attribute;
        }

        public static IEnumerable<IProjection> Vals(this IProjection projection, int batchIndex = -1)
            => projection.Cast<object>().Vals(batchIndex);
        public static IProjectionValues<TItem> Vals<TModel, TItem>(this IProjection<TModel> projection, Expression<Func<TModel, IEnumerable<TItem>>> expression, int batchIndex = -1)
            => projection.Open(expression).Vals(batchIndex);

        public static IProjection<TProperty> Val<TModel, TProperty>(this IProjection<TModel> projection, Expression<Func<TModel, TProperty>> expression)
            => projection.For(expression).Val();
        public static IProjection<TModel> Val<TModel>(this IProjection<TModel> projection)
            => ((IProjection)projection).Val().Cast<TModel>();
    }
}
