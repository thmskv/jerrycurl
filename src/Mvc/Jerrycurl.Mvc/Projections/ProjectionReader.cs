using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;
using System;
using System.Collections.Generic;

namespace Jerrycurl.Mvc.Projections
{
    internal class ProjectionReader : IDisposable
    {
        public IField Source { get; }
        public IEnumerable<IProjectionMetadata> Header { get; }

        private RelationReader innerReader;
        private List<int> indexHeader;

        public ProjectionReader(IField source, IEnumerable<IProjectionMetadata> header)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.Header = header ?? throw new ArgumentNullException(nameof(header));
        }

        public static IEnumerable<IProjectionData> Lookup(IField source, IEnumerable<IProjectionMetadata> header)
        {
            using ProjectionReader reader = new ProjectionReader(source, header);

            if (reader.Read())
            {
                foreach (IProjectionData data in reader.GetData())
                    yield return data;
            }
        }

        public IEnumerable<IProjectionData> GetData()
        {
            ITuple data = this.innerReader;

            for (int i = 0; i < this.indexHeader.Count; i += 3)
                yield return new ProjectionData(data[this.indexHeader[i]], data[this.indexHeader[i + 1]], data[this.indexHeader[i + 2]]);
        }

        private RelationReader CreateReader()
        {
            Dictionary<MetadataIdentity, int> indexMap = new Dictionary<MetadataIdentity, int>();

            List<IRelationMetadata> header = new List<IRelationMetadata>();

            this.indexHeader = new List<int>();

            foreach (IProjectionMetadata attribute in this.Header)
            {
                AddAttribute(attribute);
                AddAttribute(attribute.Input);
                AddAttribute(attribute.Output);
            }

            Relation body = new Relation(this.Source, new RelationHeader(this.Source.Identity.Schema, header));

            return body.GetReader();

            void AddAttribute(IProjectionMetadata metadata)
            {
                if (indexMap.TryGetValue(metadata.Identity, out int valueIndex))
                    this.indexHeader.Add(valueIndex);
                else
                {
                    header.Add(metadata.Relation);
                    this.indexHeader.Add(indexMap.Count);

                    indexMap.Add(metadata.Identity, indexMap.Count);
                }
            }
        }

        public bool Read()
        {
            if (this.innerReader == null)
                this.innerReader = this.CreateReader();

            return this.innerReader.Read();
        }

        public void Dispose()
        {
            this.innerReader?.Dispose();
        }
    }
}
