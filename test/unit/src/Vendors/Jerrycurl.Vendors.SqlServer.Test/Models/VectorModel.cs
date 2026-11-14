using Jerrycurl.Cqs.Metadata.Annotations;
using Microsoft.Data.SqlTypes;

namespace Jerrycurl.Vendors.SqlServer.Test.Models;

[Table("jerry_vectors")]
public class VectorModel
{
    public int Id { get; set; }
    public SqlVector<float> Embedding { get; set; }
    public SqlVector<float>? NullableEmbedding { get; set; }
    public SqlVector<float>? NullEmbedding { get; set; }
}
