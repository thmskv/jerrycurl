using System;
using Jerrycurl.IO;

namespace Jerrycurl.CodeAnalysis.Razor.ProjectSystem;

public class RazorProjectItem
{
    public string ProjectPath { get; set; }
    public string FullPath { get; set; }
    public string Content { get; set; }
    public string Checksum { get; set; }

    public static RazorProjectItem Create(string path, string projectDirectory = null, string content = null, string checksum = null)
    {
        if (string.IsNullOrEmpty(projectDirectory))
            return new RazorProjectItem() { FullPath = path };

        PathHelper.MakeRelativeAndAbsolutePath(projectDirectory, path, out var absolutePath, out var relativePath);

        return new RazorProjectItem()
        {
            ProjectPath = relativePath,
            FullPath = absolutePath,
            Content = content,
            Checksum = checksum,
        };
    }
}
