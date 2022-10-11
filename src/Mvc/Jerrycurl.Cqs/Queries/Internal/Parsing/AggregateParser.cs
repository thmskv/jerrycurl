using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Cqs.Queries.Internal.IO;
using Jerrycurl.Cqs.Queries.Internal.Caching;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Cqs.Queries.Internal.IO.Targets;

namespace Jerrycurl.Cqs.Queries.Internal.Parsing
{
    internal class AggregateParser : BaseParser
    {
        public AggregateParser(ISchema schema)
            : base(schema)
        {
            
        }

        public AggregateResult Parse(IEnumerable<AggregateAttribute> header)
        {
            NodeTree nodeTree = NodeParser.Parse(this.Schema, header);

            Node valueNode = nodeTree.Items.FirstOrDefault(this.IsResultNode);
            Node itemNode = nodeTree.Items.FirstOrDefault(this.IsResultListNode);

            AggregateResult result = new AggregateResult(this.Schema);

            if (itemNode != null)
            {
                result.Value = this.CreateReader(result, itemNode);
                result.Target = new AggregateTarget()
                {
                    AddMethod = itemNode.Metadata.Parent.Composition.Add,
                    NewList = itemNode.Metadata.Parent.Composition.Construct,
                };
            }
            else
                result.Value = this.CreateReader(result, valueNode);

            return result;
        }
    }
}
