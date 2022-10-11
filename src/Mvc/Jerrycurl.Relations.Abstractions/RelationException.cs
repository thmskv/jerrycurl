using Jerrycurl.Relations.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Jerrycurl.Relations
{
    [Serializable]
    public class RelationException : Exception
    {
        public RelationException()
        {

        }

        public RelationException(string message)
            : base(message)
        {

        }

        public RelationException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected RelationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        #region " Exception helpers "

        public static RelationException From(ISchema schema, string message = null, Exception innerException = null)
        {
            string fullMessage = $"Error in relation {schema}.";

            if (message != null || innerException != null)
                fullMessage += $" {message ?? innerException.Message}";

            return new RelationException(fullMessage, innerException);
        }

        public static RelationException From(IRelationHeader header, string message = null, Exception innerException = null)
        {
            string attributeList = string.Join(", ", header.Attributes.Select(a => $"\"{a.Identity.Name}\""));
            string fullMessage = $"Error in relation {header.Schema}({attributeList}).";

            if (message != null || innerException != null)
                fullMessage += $" {message ?? innerException.Message}";

            return new RelationException(fullMessage, innerException);
        }

        internal static RelationException AttributeDoesNotBelongToSchema(ISchema schema, IRelationMetadata attribute, int index)
            => From(schema, $"Attribute {attribute.Identity} at index {index} does not belong to {schema}.");

        internal static RelationException InvalidDataReaderHeader(IRelationHeader header, IEnumerable<string> dataHeader)
        {
            string headerString = string.Join(", ", dataHeader.Select(a => $"\"{a}\""));

            return From(header, $"Degree does not match IDataReader({headerString}).");
        }

        internal static RelationException IndexOutOfRange(IRelationHeader header, int index)
            => From(header, $"Index {index} is out of range.");

        internal static RelationException NoDataAvailable(IRelationHeader header)
            => From(header, $"No data available.");

        internal static RelationException NoDataAvailableCallRead(IRelationHeader header)
            => From(header, $"No data available. Call Read() to start reading data.");

        internal static RelationException AttributeCannotBeNull(ISchema schema, int emptyIndex)
            => From(schema, $"Attribute at index {emptyIndex} cannot be null.");

        internal static RelationException DataHeaderCannotBeNull(IRelationHeader header, int emptyIndex)
            => From(header, $"Attribute name at index {emptyIndex} cannot be null.");

        internal static RelationException DataHeaderCannotHaveDupes(IRelationHeader header, IReadOnlyList<string> dataHeader, int dupeIndex)
            => From(header, $"Attribute \"{dataHeader[dupeIndex]}\" at index {dupeIndex} is already specified.");

        internal static RelationException CannotForwardQueue(IRelation relation, MetadataIdentity identity, Exception innerException)
            => From(relation.Header, $"Cannot move cursor for '{identity}'.", innerException);

        internal static RelationException Unreachable(MetadataIdentity source, IRelationHeader header, IList<IRelationMetadata> attributes)
        {
            string attributeNames = string.Join(", ", attributes.Select(a => $"\"{a.Identity.Name}\""));

            if (attributes.Count == 1)
                return From(header, $"Attribute {attributeNames} is unreachable from source \"{source.Name}\".");
            else
                return From(header, $"Attributes {attributeNames} are unreachable from source \"{source.Name}\".");
        }
            
        #endregion
    }
}
