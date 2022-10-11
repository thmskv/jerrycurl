using Jerrycurl.Reflection;
using Jerrycurl.Relations.Metadata;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Jerrycurl.Cqs.Metadata
{
    [Serializable]
    public class BindingException : Exception
    {
        public BindingException()
        {

        }

        public BindingException(string message)
            : base(message)
        {

        }

        public BindingException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected BindingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }


        public static BindingException Create(IBindingMetadata metadata, string message = null, Exception innerException = null)
        {
            message ??= innerException?.Message;

            if (message != null)
                return new BindingException($"Cannot bind to {metadata.Identity}: {message}", innerException);

            return new BindingException($"Cannot bind to {metadata.Identity}.", innerException);
        }

        internal static BindingException IsReadOnly(IBindingMetadata metadata)
            => Create(metadata, message: "Data is read-only.");

        internal static BindingException InvalidCast(IBindingMetadata metadata, Exception innerException)
            => Create(metadata, innerException: innerException);

        internal static BindingException InvalidCast(IBindingMetadata metadata, Type sourceType, Type targetType, Exception innerException)
            => Create(metadata, message: $"Cannot convert type '{sourceType.GetSanitizedName()}' to '{targetType.GetSanitizedName()}'.", innerException: innerException);

        internal static BindingException InvalidConstructor(IBindingMetadata metadata)
            => Create(metadata, message: "No valid constructor found.");

        internal static BindingException NoReferenceFound(MetadataIdentity from, MetadataIdentity to)
            => new BindingException($"No valid reference found between {from} and {to}. Include matching [Key] and [Ref] attributes to map across one-to-many boundaries.");

        internal static BindingException NoReferenceFound(IBindingMetadata metadata)
        {
            if (metadata.HasFlag(BindingMetadataFlags.Item))
                return NoReferenceFound(metadata.Identity, metadata.Parent.Parent.Identity);
            else
                return NoReferenceFound(metadata.Identity, metadata.Parent.Identity);
        }

        internal static BindingException InvalidReference(IReference reference)
        {
            string leftTuple = string.Join(", ", reference.Key.Properties.Select(m => m.Type.GetSanitizedName()));
            string rightTuple = string.Join(", ", reference.Other.Key.Properties.Select(m => m.Type.GetSanitizedName()));

            return new BindingException($"Cannot join {reference.Metadata.Identity}: ({leftTuple}) is incompatible with {rightTuple}.");
        }

    }
}
