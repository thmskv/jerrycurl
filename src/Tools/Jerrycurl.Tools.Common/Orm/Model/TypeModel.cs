namespace Jerrycurl.Tools.Orm.Model
{
    public class TypeModel
    {
        public string DbName { get; }
        public string ClrName { get; }
        public bool IsValueType { get; }

        public TypeModel(string dbName, string clrName, bool isValueType)
        {
            this.DbName = dbName;
            this.ClrName = clrName;
            this.IsValueType = isValueType;
        }
    }
}
