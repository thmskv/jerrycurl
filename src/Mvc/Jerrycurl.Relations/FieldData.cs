namespace Jerrycurl.Relations
{
    internal class FieldData : IFieldData
    {
        public object Relation { get; }
        public int Index { get; }
        public object Parent => null;
        public object Value { get; }

        public FieldData(object relation, int index)
        {
            this.Relation = relation;
            this.Index = index;
        }

        public FieldData(object value)
        {
            this.Relation = this.Value = value;
        }
    }
}
