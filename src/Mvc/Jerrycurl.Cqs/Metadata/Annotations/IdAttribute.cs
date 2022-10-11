using System;

namespace Jerrycurl.Cqs.Metadata.Annotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class IdAttribute : Attribute
    {

    }
}