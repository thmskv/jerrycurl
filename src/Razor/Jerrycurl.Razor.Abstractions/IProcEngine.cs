using Jerrycurl.Razor.Projections;
using System;

namespace Jerrycurl.Razor;

public delegate IProcResult ProcFactory(object model);
public delegate void PageFactory(IProjection model, IProjection result);
public delegate void BodyFactory();

public interface IProcEngine
{
    public PageDescriptor Describe(Type pageType);
    public ProcFactory Proc(PageDescriptor descriptor, ProcArgs args);
    public PageFactory Page(Type pageType);
    public IDomainOptions Options(Type domainType);
}