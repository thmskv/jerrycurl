using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Mvc;

public interface IProcLookup
{
    public string Parameter(ProjectionIdentity identity, IField value);
    public string Parameter(ProjectionIdentity identity, MetadataIdentity metadata);
    public string Variable(ProjectionIdentity identity, IField value);
    public string Table(ProjectionIdentity identity, MetadataIdentity metadata);

    public string Custom(string prefix, ProjectionIdentity identity = null, MetadataIdentity metadata = null, IField value = null);
}
