using System;
using System.Collections.Generic;
using Jerrycurl.Mvc.Internal;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Mvc;

public class ProcLookup : IProcLookup
{
    private readonly Dictionary<ProcLookupKey, string> nameMap = [];
    private readonly Dictionary<string, int> prefixCount = [];

    private string FromKey(ProcLookupKey key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (this.nameMap.TryGetValue(key, out string alias))
            return alias;

        this.prefixCount.TryGetValue(key.Prefix, out int prefixes);
        this.prefixCount[key.Prefix] = prefixes + 1;

        return this.nameMap[key] = key.Prefix + prefixes;
    }

    public string Custom(string prefix, ProjectionIdentity identity = null, MetadataIdentity metadata = null, IField field = null) => this.FromKey(new ProcLookupKey(prefix, identity, metadata, field));

    public string Parameter(ProjectionIdentity identity, IField field) => this.Custom("JP", identity, field: field);
    public string Parameter(ProjectionIdentity identity, MetadataIdentity metadata) => this.Custom("JP", identity, metadata: metadata);
    public string Table(ProjectionIdentity identity, MetadataIdentity metadata) => this.Custom("JT", identity, metadata);
    public string Variable(ProjectionIdentity identity, IField field) => this.Custom("JV", identity, field: field);
}
