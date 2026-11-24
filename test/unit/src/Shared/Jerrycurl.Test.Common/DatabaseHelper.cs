using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Filters;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Queries;
using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Test.Profiling;
using Jerrycurl.Vendors.Sqlite.Metadata;
using Microsoft.Data.Sqlite;
using Jerrycurl.Cqs.Language;
using System.Collections.Generic;

namespace Jerrycurl.Test;

public class DatabaseHelper
{
    public const string TestDbConnectionString = "DATA SOURCE=testdb.db";

    public static DatabaseHelper Default { get; } = new DatabaseHelper();

    public SchemaStore Store { get; set; }
    public SchemaStore SqliteStore { get; set; }
    public QueryOptions QueryOptions { get; set; }
    public CommandOptions CommandOptions { get; set; }

    public DatabaseHelper()
    {
        this.Store = this.GetStore();
        this.SqliteStore = this.GetStore(contracts: [new SqliteBindingConvention()]);
        this.QueryOptions = this.GetQueryOptions();
        this.CommandOptions = this.GetCommandOptions();
    }

    public SchemaStore GetStore(DotNotation notation = null, IEnumerable<object> contracts = null)
    {
        BindingMetadataBuilder bindingBuilder = new();
        ReferenceMetadataBuilder referenceBuilder = new ReferenceMetadataBuilder();
        TableMetadataBuilder tableBuilder = new TableMetadataBuilder();

        SchemaStore store = new SchemaStore(notation ?? new DotNotation(), bindingBuilder, referenceBuilder, tableBuilder);

        if (contracts != null)
        {
            foreach (var contract in contracts)
            {
                if (contract is IRelationConvention relationConvention)
                    store.Use(relationConvention);

                if (contract is IBindingConvention bindingConvention)
                    store.Use(bindingConvention);

                if (contract is ITableNamingPolicy namingPolicy)
                    store.Use(namingPolicy);
            }
        }

        return store;
    }

    public QueryOptions GetQueryOptions(SchemaStore store = null)
    {
        return new QueryOptions()
        {
            ConnectionFactory = () => new ProfilingConnection(new SqliteConnection(TestDbConnectionString)),
            Store = store ?? this.SqliteStore,
        };
    }

    public CommandOptions GetCommandOptions(params IFilter[] filters)
    {
        return new CommandOptions()
        {
            ConnectionFactory = () => new ProfilingConnection(new SqliteConnection(TestDbConnectionString)),
            Filters = filters ?? [],
        };
    }

    public QueryEngine Queries => new QueryEngine(this.QueryOptions);
    public CommandEngine Commands => new CommandEngine(this.CommandOptions);
}
