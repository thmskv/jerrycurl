using Jerrycurl.Cqs.Metadata.Annotations;
using Pgvector;

namespace Jerrycurl.Vendors.Postgres.Test.Models;

[Table("jerry_vectors")]
public class VectorModel
{
    public int Id { get; set; }
    public Vector Embedding { get; set; }
    public Vector NullEmbedding { get; set; }
    public HalfVector HalfEmbedding { get; set; }
    public SparseVector SparseEmbedding { get; set; }
}
