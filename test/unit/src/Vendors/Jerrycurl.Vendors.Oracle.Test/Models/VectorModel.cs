using Jerrycurl.Cqs.Metadata.Annotations;
using Oracle.ManagedDataAccess.Types;

namespace Jerrycurl.Vendors.Oracle.Test.Models;

[Table("jerry_vectors")]
public class VectorModel
{
    public int Id { get; set; }
    public OracleVector Embedding { get; set; }
    public OracleVector NullEmbedding { get; set; }
}
