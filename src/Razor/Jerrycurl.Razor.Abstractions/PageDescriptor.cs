using System;

namespace Jerrycurl.Razor;

public sealed class PageDescriptor
{
    public Type PageType { get; internal set; }
    public Type DomainType { get; internal set; }

    internal PageDescriptor()
    {

    }

    public PageDescriptor(Type pageType, Type domainType = null)
    {
        this.PageType = pageType ?? throw new ArgumentNullException(nameof(pageType));
        this.DomainType = domainType;
    }
}
