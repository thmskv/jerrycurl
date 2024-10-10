using System;
using System.Collections.Generic;
using System.Linq;
using static Jerrycurl.Tools.Orm.Model.SchemaModel;

namespace Jerrycurl.Tools.Orm.Model;

public class SchemaBuilder
{
    public SchemaModel Model { get; } = new SchemaModel();
    public OrmToolOptions Options { get; }

    public SchemaBuilder(OrmToolOptions options)
    {
        this.Options = options;

        this.MergeFlags();
    }

    private void MergeFlags()
    {
        if (this.Options.Flags != null)
        {
            foreach (var kvp in this.Options.Flags)
                this.SetFlag(kvp.Key, kvp.Value);
        }
    }

    public TableModel AddTable(string tableSchema, string tableName, bool ignore = false)
    {
        if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));

        TableModel table = this.FindTable(tableSchema, tableName);

        if (table != null)
            return table;

        this.Model.Tables.Add(table = new TableModel()
        {
            Name = tableName,
            Schema = tableSchema,
            Ignore = ignore,
        });

        return table;
    }

    public void SetFlag(string flag, string value, bool overwrite = true)
    {
        this.Model.Flags ??= [];

        if (overwrite || !this.Model.Flags.ContainsKey(flag))
            this.Model.Flags[flag] = value;
    }

    public TypeModel AddType(string dbName, string clrName, bool isNullable)
    {
        this.Model.Types ??= [];

        TypeModel newType = new TypeModel() { DbName = dbName, ClrName = clrName, IsNullable = isNullable };

        this.Model.Types.Add(newType);

        return newType;
    }

    public ColumnModel AddColumn(string tableSchema, string tableName, string columnName, string typeName = null, bool isNullable = true, bool isIdentity = false, bool ignore = false, bool ignoreTable = false)
    {
        if (columnName == null)
            throw new ArgumentNullException(nameof(columnName));

        TableModel table = this.FindTable(tableSchema, tableName);
        ColumnModel column = this.FindColumn(table, columnName);

        if (column != null)
            return column;
        else if (table == null)
            table = this.AddTable(tableSchema, tableName, ignoreTable);

        table.Columns.Add(column = new ColumnModel()
        {
            Name = columnName,
            TypeName = typeName,
            IsNullable = isNullable,
            IsIdentity = isIdentity,
            Ignore = ignore,
        });

        return column;
    }

    public KeyModel AddKey(string tableSchema, string tableName, string columnName, string keyName, int keyIndex)
    {
        TableModel table = this.FindTable(tableSchema, tableName);
        ColumnModel column = this.FindColumn(table, columnName);
        KeyModel key = this.FindKey(column, keyName, keyIndex);

        if (key != null)
            return key;
        else if (column != null)
            column = this.AddColumn(tableSchema, tableName, columnName);

        column.Keys.Add(key = new KeyModel()
        {
            Name = keyName,
            Index = keyIndex,
        });

        return key;
    }

    public ReferenceModel AddReference(string tableSchema, string tableName, string columnName, string referenceName, string keyName, int keyIndex)
    {
        TableModel table = this.FindTable(tableSchema, tableName);
        ColumnModel column = this.FindColumn(table, columnName);
        ReferenceModel reference = this.FindReference(column, referenceName, keyIndex);

        if (reference != null)
            return reference;
        else if (column != null)
            column = this.AddColumn(tableSchema, tableName, columnName);

        column.References.Add(reference = new ReferenceModel()
        {
            Name = referenceName,
            KeyName = keyName,
            KeyIndex = keyIndex,
        });

        return reference;
    }

    protected TableModel FindTable(string tableSchema, string tableName) => this.Model.Tables.FirstOrDefault(t => string.Equals(t.Schema, tableSchema) && t.Name.Equals(tableName));
    protected ColumnModel FindColumn(TableModel table, string columnName) => table?.Columns.FirstOrDefault(c => c.Name.Equals(columnName));
    protected KeyModel FindKey(ColumnModel column, string keyName, int keyIndex) => column?.Keys.FirstOrDefault(k => k.Name.Equals(keyName) && k.Index == keyIndex);
    protected ReferenceModel FindReference(ColumnModel column, string referenceName, int keyIndex) => column?.References.FirstOrDefault(r => r.Name.Equals(referenceName) && r.KeyIndex == keyIndex);
}
