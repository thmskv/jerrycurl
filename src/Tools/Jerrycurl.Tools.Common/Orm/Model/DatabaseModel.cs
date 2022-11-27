using System.Collections.Generic;

namespace Jerrycurl.Tools.Orm.Model
{
    public class DatabaseModel
    {
        public IList<TableModel> Tables { get; set; } = new List<TableModel>();
        public IList<TypeModel> Types { get; set; } = new List<TypeModel>();
        public IList<string> Imports { get; set; } = new List<string>();
        public Dictionary<string, string> Flags { get; set; }

        public class TableModel
        {
            public string Schema { get; set; }
            public string Name { get; set; }
            public bool Ignore { get; set; }
            public ClassModel Clr { get; set; }

            public IList<ColumnModel> Columns { get; set; } = new List<ColumnModel>();
        }

        public class ColumnModel
        {
            public string Name { get; set; }
            public string TypeName { get; set; }
            public bool IsNullable { get; set; }
            public bool IsIdentity { get; set; }
            public bool Ignore { get; set; }
            public PropertyModel Clr { get; set; }

            public IList<KeyModel> Keys { get; set; } = new List<KeyModel>();
            public IList<ReferenceModel> References { get; set; } = new List<ReferenceModel>();
        }

        public class KeyModel
        {
            public string Name { get; set; }
            public int Index { get; set; }
        }

        public class ReferenceModel
        {
            public string Name { get; set; }
            public string KeyName { get; set; }
            public int KeyIndex { get; set; }
        }

        public class PropertyModel
        {
            public string[] Modifiers { get; set; }
            public string TypeName { get; set; }
            public string Name { get; set; }
            public bool IsInput { get; set; }
            public bool IsOutput { get; set; }
            public bool IsJson { get; set; }
        }

        public class ClassModel
        {
            public string Namespace { get; set; }
            public string[] Modifiers { get; set; }
            public string[] BaseTypes { get; set; }
            public bool IsStruct { get; set; }
            public string Name { get; set; }
        }

        public class TypeModel
        {
            public string DbName { get; set; }
            public string ClrName { get; set; }
            public bool IsNullable { get; set; }

            public TypeModel()
            {

            }
            public TypeModel(string dbName, string clrName, bool isNullable)
            {
                this.DbName = dbName;
                this.ClrName = clrName;
                this.IsNullable = isNullable;
            }
        }
    }
}
