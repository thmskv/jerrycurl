using System;

namespace Jerrycurl.Mvc;

public interface IProcLocator
{
    public PageDescriptor Create(Type pageType);
    public PageDescriptor FindPage(string procName, Type originType);
}
