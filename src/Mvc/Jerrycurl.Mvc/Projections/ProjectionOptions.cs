using System;

namespace Jerrycurl.Mvc.Projections;

public class ProjectionOptions : IProjectionOptions
{
    public string Separator { get; set; } = "," + Environment.NewLine;
    public static IProjectionOptions Default { get; } = new ProjectionOptions();

    public ProjectionOptions()
    {

    }

    public ProjectionOptions(IProjectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.Separator = options.Separator;
    }
}