using System;

namespace Jerrycurl.Razor.Metadata.Annotations;

/// <summary>
/// Renders the annotated page inside the template page <see cref="PageType"/>, which writes it through <c>RenderBody()</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class TemplateAttribute : Attribute
{
    public Type PageType { get; }

    public TemplateAttribute(Type pageType)
    {
        this.PageType = pageType ?? throw new ArgumentNullException(nameof(pageType));
    }
}
