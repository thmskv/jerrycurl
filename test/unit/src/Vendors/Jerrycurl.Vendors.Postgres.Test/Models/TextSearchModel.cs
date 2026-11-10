using Jerrycurl.Cqs.Metadata.Annotations;
using NpgsqlTypes;

namespace Jerrycurl.Vendors.Postgres.Test.Models;

[Table("jerry_textsearch")]
public class TextSearchModel
{
    public NpgsqlTsVector Document { get; set; }
    public NpgsqlTsVector NullDocument { get; set; }
    public NpgsqlTsQuery Query { get; set; }
}
