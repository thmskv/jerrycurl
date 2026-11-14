using Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Vendors.MySql.Test.Models;

[Table("jerry_vectors")]
public class VectorModel
{
    public int Id { get; set; }
    public float[] Embedding { get; set; }
    public float[] NullEmbedding { get; set; }
}
