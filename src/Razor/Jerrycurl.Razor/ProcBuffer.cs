using System.Collections.Generic;
using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Queries;

namespace Jerrycurl.Razor;

public class ProcBuffer : SqlBuffer, ISqlSerializer<Command>, ISqlSerializer<Query>
{
    IEnumerable<Command> ISqlSerializer<Command>.Serialize(ISqlOptions options)
    {
        foreach (ISqlContent content in this.Read(options))
        {
            yield return new Command()
            {
                CommandText = content.Text,
                Parameters = [.. content.Parameters],
                Bindings = [.. content.Bindings],
            };
        }

    }

    IEnumerable<Query> ISqlSerializer<Query>.Serialize(ISqlOptions options)
    {
        foreach (ISqlContent content in this.Read(options))
        {
            yield return new Query()
            {
                QueryText = content.Text,
                Parameters = [.. content.Parameters],
            };
        }
    }
}
