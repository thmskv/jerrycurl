using System;

namespace Jerrycurl.Razor.Metadata.Annotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class OutAttribute : Attribute
{

}
