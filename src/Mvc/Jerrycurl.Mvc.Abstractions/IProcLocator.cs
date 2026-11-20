using System;

namespace Jerrycurl.Mvc;

public interface IProcLocator
{
    PageDescriptor Create(Type pageType);
    PageDescriptor FindPage(string procName, Type originType);
}
