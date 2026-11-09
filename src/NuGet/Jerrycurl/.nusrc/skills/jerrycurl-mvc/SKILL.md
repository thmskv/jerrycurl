---
name: jerrycurl-mvc
description: Architecture and project conventions for Jerrycurl, the .NET ORM and MVC framework built on Razor SQL (.cssql) files. Use when setting up Jerrycurl in a project, adding a domain (IDomain), writing accessors (Accessor subclasses calling Query/Execute), deciding where .cssql files go, wiring dependency injection, or debugging "Page not found" / "Domain not found" errors. Start here before the jerrycurl-razor-sql, jerrycurl-queries, jerrycurl-commands and jerrycurl-models skills.
---

# Jerrycurl MVC

Jerrycurl separates data access into four layers, located by **namespace conventions** rather than registration:

| Layer | What it is | Where it lives |
|---|---|---|
| **Model** | POCO classes mapped to tables (`[Table]`, `[Id]`, `[Key]`, `[Ref]`) and *views* composed from them | Anywhere (e.g. `Database/`, `Views/`) |
| **View** (queries/commands) | `.cssql` Razor SQL pages compiled into C# classes at build time | `Queries/` (reads) and `Commands/` (writes) |
| **Controller** (accessor) | Class deriving from `Jerrycurl.Mvc.Accessor` that runs a page with a model | `Accessors/` |
| **Domain** | Class implementing `IDomain` that configures connection and dialect | A parent namespace shared by the accessors |

Related skills: **jerrycurl-razor-sql** (file syntax and projection API), **jerrycurl-queries**, **jerrycurl-commands**, **jerrycurl-models**.

## Installation

```shell
dotnet add package Jerrycurl                    # MVC runtime + .cssql source generator
dotnet add package Jerrycurl.Vendors.SqlServer  # or .Sqlite, .Postgres, .MySql, .Oracle
```

The project must be SDK-style. Every `**/*.cssql` file is included automatically as a `JerryFile` item and compiled by a Roslyn source generator. No extra build step is needed. To exclude a file:

```xml
<ItemGroup>
  <JerryFile Remove="Queries\Legacy\Old.cssql" />
</ItemGroup>
```

## Recommended layout

```
MyApp.Data/                      (RootNamespace = MyApp.Data)
├── BlogDomain.cs                namespace MyApp.Data            : IDomain
├── _imports.cssql               shared @import / @project / @inject directives
├── Database/Blog.cs             namespace MyApp.Data.Database   : [Table] models
├── Views/BlogView.cs            namespace MyApp.Data.Views      : composed result types
├── Accessors/BlogsAccessor.cs   namespace MyApp.Data.Accessors  : Accessor
├── Queries/Blogs/GetAll.cssql   -> class MyApp.Data.Queries.Blogs.GetAll_cssql
├── Commands/Blogs/Create.cssql  -> class MyApp.Data.Commands.Blogs.Create_cssql
└── Queries/Shared/...           pages reachable from every accessor
```

A `.cssql` file compiles to a class named after the **file name** (non-identifier characters become `_`, and `_cssql` is appended), in namespace `RootNamespace` + **folder path**. `Queries/Blogs/GetAll.cssql` becomes `<Root>.Queries.Blogs.GetAll_cssql`. The folder structure therefore matters.

## Domain

```csharp
namespace MyApp.Data;

public class BlogDomain : IDomain
{
    public void Configure(DomainOptions options)
    {
        options.UseSqlServer("SERVER=.;DATABASE=blogdb;TRUSTED_CONNECTION=true");
        // options.UseJson();               // Jerrycurl.Extensions.Json: enables [Json] properties
        // options.Sql.Filters.Add(...);    // default filters for every query/command
    }
}
```

- The domain is found by walking **up** from the accessor's namespace. The first namespace containing exactly one `IDomain` wins. Two domains in the same namespace is an error.
- `UseSqlServer`, `UseSqlite`, `UsePostgres`, `UseMySql` and `UseOracle` live in namespace `Jerrycurl.Mvc` and come from the vendor packages. They set the connection factory, dialect and vendor defaults such as max parameters per batch.
- `DomainOptions` also exposes `ConnectionFactory`, `Dialect`, `Schemas` and `Sql` (`SqlOptions`: `Filters`, `MaxParameters`, `MaxSql`).

## Accessors

```csharp
namespace MyApp.Data.Accessors;

public class BlogsAccessor : Accessor
{
    // -> Queries/Blogs/GetAll.cssql (page name = calling method name)
    public IList<BlogView> GetAll(DateTime fromDate)
        => this.Query<BlogView>(model: new BlogFilter { FromDate = fromDate });

    // "Async" suffix is stripped: -> Queries/Blogs/GetById.cssql
    public Task<BlogView> GetByIdAsync(int id, CancellationToken ct = default)
        => this.OneAsync<BlogView>(new BlogFilter { Id = id }, cancellationToken: ct);

    // -> Commands/Blogs/Create.cssql ; generated ids are written back into `blogs`
    public void Create(IList<Blog> blogs) => this.Execute(blogs);

    // Explicit page name and a transaction
    public void Import(IList<Blog> blogs)
        => this.Execute(blogs, o => o.UseTransaction(), commandName: "Create");
}
```

Protected methods on `Accessor` (all take `model`, an optional `configure` callback for `SqlOptions`, and a page name that defaults to the caller via `[CallerMemberName]`):

| Method | Returns | Use for |
|---|---|---|
| `Query<T>` / `QueryAsync<T>` | `IList<T>` (buffered) | Lists of rows or of composed views. Every result set feeds the same list. |
| `One<T>` / `OneAsync<T>` | `T` | Single row. Uses `SingleOrDefault`, so it **throws if more than one item** results. |
| `Aggregate<T>` / `AggregateAsync<T>` | `T` | One object built from several result sets (e.g. a dashboard with multiple lists). |
| `Enumerate<T>` / `EnumerateAsync<T>` | `IEnumerable<T>` / `IAsyncEnumerable<T>` | Unbuffered streaming of large results. |
| `Enumerate()` / `EnumerateAsync()` | `QueryReader` per result set | Low-level reading of heterogeneous result sets. |
| `Execute` / `ExecuteAsync` | `void` / `Task` | Commands. Output columns are written back into `model`. |

Rules:
- The type passed to `Query<T>` and the runtime type of `model` drive mapping at runtime. The page's `@result` and `@model` directives only provide compile-time typing for lambdas, so **keep them consistent** with what the accessor passes.
- `model` can be any object: an entity, a list of entities, a filter class, or an anonymous object. Anonymous objects work for parameters, but a page needs `@model SomeType` to use typed lambdas like `M.Par(m => m.Id)`.
- Use `configure: o => o.UseTransaction()` (or `UseTransaction(IsolationLevel)`, `UseTransactionScope()`) to wrap all generated batches in a transaction.

## How a page is located

For an accessor `<Ns>.Accessors.BlogsAccessor` (a trailing `.Accessors` segment is stripped to give `<Ns>`) and page name `GetAll`, the locator tries, in order (case-insensitive):

1. `<Ns>.Queries.Blogs.GetAll_cssql`
2. `<Ns>.Commands.Blogs.GetAll_cssql`
3. `<Ns>.Queries.Shared.GetAll_cssql`
4. `<Ns>.Commands.Shared.GetAll_cssql`
5. `<Ns>.Queries.GetAll_cssql`
6. `<Ns>.Commands.GetAll_cssql`

The `Blogs` segment comes from the accessor name minus the `Accessor` suffix. Page names can also be paths:

| Name form | Resolved relative to |
|---|---|
| `"GetAll"`, `"Sub/GetAll"` | The search list above |
| `"~/Queries/Blogs/GetAll"` | The domain's namespace |
| `"./GetAll"`, `"../Other/GetAll"` | The accessor's (or calling page's) namespace |
| `"/MyApp/Data/Queries/Blogs/GetAll"` | Absolute namespace path |

Dots are not separators, so `"GetAll.Oracle"` finds `GetAll.Oracle.cssql` (class `GetAll_Oracle_cssql`). A trailing `.cssql` in the name is tolerated. From inside a page (`Subquery`, `Subcommand`, `@template`), lookup searches the page's own namespace, then the nearest `Queries`/`Commands` root, then that root's `Shared` folder.

## Dependency injection

`@inject` directives in pages, and domains with constructor parameters, need an `IServiceProvider`:

```csharp
ProcEngine.Services = app.Services;   // global default, simplest

// or per accessor instance
accessor.Context = new AccessorContext(new ProcLocator(), new ProcEngine(serviceProvider));
```

Without a provider, a page with `@inject` throws `NotSupportedException`. `@project` directives do not need DI.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `PageNotFoundException: Page not found from procedure name 'X'. Looked in: ...` | File is in the wrong folder or namespace, accessor name lacks the `Accessor` suffix, method name differs from file name, or the file was removed from `JerryFile` items. Read the "Looked in" list. |
| Domain not found | No `IDomain` in the accessor's namespace or any parent namespace. |
| `Multiple domains found in namespace` | Move one domain to a different namespace. |
| Generated SQL contains `else` or stray text | Write `@else`, not `else`. See jerrycurl-razor-sql. |
| Lambda errors such as "cannot use lambda with dynamic" | Page lacks `@model` / `@result`, so `M` / `R` are `dynamic` / `object`. |
| Values missing from result objects | Column aliases do not match property paths. Use `@R.Map()` or `AS @R.Prop(...)`, never hand-written aliases. |
| Database exception during `Query`/`Execute` | The stack trace includes a frame in the `.cssql` file (`...cssql:line N`) of the page that produced the SQL. |
| Source generator problems | Look for generated `jerry+<guid>.log` sources (they contain the exception). Set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` to inspect the generated code (one `<Folder>_<File>_cssql.g.cs` per page). |

## Tooling

The separate `dotnet-jerry` global tool (`dotnet tool install --global dotnet-jerry`) can generate `[Table]` model classes from an existing database schema and transpile `.cssql` files outside of MSBuild. Run `jerry help` for its current commands.
