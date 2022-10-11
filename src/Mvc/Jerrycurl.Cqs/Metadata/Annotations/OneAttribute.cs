using System;

namespace Jerrycurl.Cqs.Metadata.Annotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class OneAttribute : Attribute
    {

    }
}
