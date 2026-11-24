---
name: jerrycurl-pages
description: Architecture and project conventions for Jerrycurl, the .NET ORM built on typed Razor SQL (.cssql) pages. Use when setting up Jerrycurl in a project, adding a domain (IDomain), making a page callable with @query/@command, calling pages from C# (Query/QueryAsync/Aggregate/Enumerate/Execute), choosing page visibility (@visibility, JerrycurlPageVisibility), wrapping pages in an accessor class for a library, wiring dependency injection, or debugging JC diagnostics and "Domain not found" errors. Start here before the jerrycurl-razor-sql, jerrycurl-queries, jerrycurl-commands and jerrycurl-models skills.
---

# Jerrycurl pages

A `.cssql` file is its own runnable unit, like an ASP.NET Razor Page. A Roslyn source generator compiles each file into a class with static methods you call directly:

```csharp
IList<BlogView> blogs = await Queries.Blogs.GetAll.QueryAsync(new() { FromDate = since });
Commands.Blogs.Create.Execute(newBlogs);
```

| Part | What it is | Where it lives |
|---|---|---|
| **Models** | POCO classes mapped to tables (`[Table]`, `[Id]`, `[Key]`, `[Ref]`) and *views* composed from them | Anywhere (e.g. `Database/`, `Views/`) |
| **Pages** | `.cssql` Razor SQL files compiled into C# classes at build time | Anywhere. `Queries/` (reads) and `Commands/` (writes) is the recommended layout |
| **Domain** | Class implementing `IDomain` that configures connection and dialect | A parent namespace of the pages |

Related skills: **jerrycurl-razor-sql** (file syntax and projection API), **jerrycurl-queries**, **jerrycurl-commands**, **jerrycurl-models**.

## Installation

```shell
dotnet add package Jerrycurl                    # Razor SQL runtime + .cssql source generator
dotnet add package Jerrycurl.Vendors.SqlServer  # or .Sqlite, .Postgres, .MySql, .Oracle
```

The project must be SDK-style. Every `**/*.cssql` file is included automatically as a `JerryFile` item and compiled by a Roslyn source generator. No extra build step is needed. To exclude a file:

```xml
<ItemGroup>
  <JerryFile Remove="Queries\Legacy\Old.cssql" />
</ItemGroup>
```

## Page kinds: `@query`, `@command` or partial

> Add `@query` or `@command` to make a page callable from C#. Without one, it's a building block for other pages.

| Page declares | Generated static API |
|---|---|
| `@query` | `Query`, `QueryAsync`, `Aggregate`, `AggregateAsync`, `Enumerate`, `EnumerateAsync`, each also as `<T>` |
| `@command` | `Execute`, `ExecuteAsync` (with a typed model, also `Execute<TList>(list)`) |
| neither | Nothing. The page is a **partial**, usable only through `@template` or `Subquery<T>` / `Subcommand<T>` |

- The kind must be on the page itself. `@query`/`@command` in `_imports.cssql` is an error (JC0003). Both on one page is an error (JC0001).
- A `@command` can't declare `@output`/`@result` (JC0002). Commands write results back into the model.
- A page can't be named after its own entry points (`Query.cssql` with `@query`, `Execute.cssql` with `@command`) (JC0006). Rename it or use `@class`.

## Layout and naming

```
MyApp.Data/                      (RootNamespace = MyApp.Data)
├── BlogDomain.cs                namespace MyApp.Data            : IDomain
├── _imports.cssql               shared @import / @project / @inject / @visibility directives
├── Database/Blog.cs             namespace MyApp.Data.Database   : [Table] models
├── Views/BlogView.cs            namespace MyApp.Data.Views      : composed result types
├── Queries/Blogs/GetAll.cssql   -> class MyApp.Data.Queries.Blogs.GetAll
├── Commands/Blogs/Create.cssql  -> class MyApp.Data.Commands.Blogs.Create
└── Queries/Shared/Paged.cssql   -> partial used with @template Shared.Paged
```

A `.cssql` file compiles to a `partial class` named after the **file name** (non-identifier characters become `_`, so `Get.Oracle.cssql` is `Get_Oracle`) in namespace `RootNamespace` + **folder path**. The framework itself ignores folder names: `Queries/` and `Commands/` are only a convention. Because the class is `partial`, you can add members next to it in a `.cs` file. Don't give that part a base class.

## Calling pages

Each entry point takes the model, an optional `configure` callback for `SqlOptions`, an optional `IProcEngine`, and (async) a `CancellationToken`:

```csharp
// @query, @input DateTime FromDate, @result BlogView
IList<BlogView> all  = GetAll.Query(new() { FromDate = since });
BlogView first       = (await GetAll.QueryAsync(new() { FromDate = since }, cancellationToken: ct)).FirstOrDefault();
IList<BlogSummary> s = GetAll.Query<BlogSummary>(new() { FromDate = since }); // different result type
Dashboard d          = GetDashboard.Aggregate();

// @command, @model Blog
Create.Execute(blog);                                 // one item
Create.Execute(blogs);                                // or a list: the page iterates M.Vals()
Create.Execute(blogs, o => o.UseTransaction());       // all batches in one transaction
```

| Method | Returns | Use for |
|---|---|---|
| `Query` / `QueryAsync` | `IList<TResult>` (buffered) | Lists of rows or of composed views. Every result set feeds the same list. |
| `Aggregate` / `AggregateAsync` | `TResult` | One object built from several result sets (e.g. a dashboard with multiple lists). |
| `Enumerate` / `EnumerateAsync` | `IEnumerable<TResult>` / `IAsyncEnumerable<TResult>` | Unbuffered streaming of large, flat results. |
| `Execute` / `ExecuteAsync` | `void` / `Task` | Commands. Output columns are written back into `model`. |

- **Model parameter type:** `@input ...` gives `Input model`, `@model T` gives `T model`, and neither gives `object model`. Every parameter defaults to `default`, so `GetAll.Query()` works.
- **Result type:** `@output ...` gives `Output`, `@result T` gives `T`, and neither gives `object`. The `<T>` overloads let the caller pick another type. Incompatible types fail at runtime.
- **Single row:** there is no `One()`. Use `Query(...).SingleOrDefault()` or `FirstOrDefault()`.
- **Transactions:** `configure: o => o.UseTransaction()` (or `UseTransaction(IsolationLevel)`, `UseTransactionScope()`).

## Visibility

Generated page classes are **`internal` by default**. A public page commits its nested `Input`/`Output` types and its static methods to your public API, so exposing one is opt-in. The first match wins:

| Source | Example | Scope |
|---|---|---|
| Page | `@visibility public` | that page |
| Nearest `_imports.cssql` | `@visibility public` | the folder and its subfolders |
| MSBuild | `<JerrycurlPageVisibility>public</JerrycurlPageVisibility>` | the project |
| Default | `internal` | |

Only `public` and `internal` are valid (JC0005). Tests in another assembly use `InternalsVisibleTo`.

## Wrapping pages in an accessor (libraries)

There is no accessor base class. For a library, the recommended shape is a plain public class that calls internal pages. It gives domain-shaped signatures, hides the generated `Input`/`Output` types, composes several pages, and can hold a per-instance engine:

```csharp
public class BlogAccessor(IProcEngine engine = null)
{
    public async Task<Blog> GetBlogAsync(int id, CancellationToken ct = default)
        => (await Queries.Blogs.GetById.QueryAsync(new() { Id = id }, engine: engine, cancellationToken: ct)).FirstOrDefault();

    public void Create(IList<Blog> blogs) => Commands.Blogs.Create.Execute(blogs, engine: engine);
}
```

A public method can't return an internal page's nested `Output`, so either map it or declare `@result MyLib.Models.Blog` with a public model type.

## Domain

```csharp
namespace MyApp.Data;

public class BlogDomain : IDomain
{
    public void Configure(DomainOptions options)
    {
        options.UseSqlServer("SERVER=.;DATABASE=blogdb;TRUSTED_CONNECTION=true");
        // options.UseJson(myOptions);      // [Json] uses System.Text.Json by default; this replaces its options
        // options.Sql.Filters.Add(...);    // default filters for every query/command
    }
}
```

- A page's domain is found by walking **up** from the page's namespace. The first namespace containing exactly one `IDomain` wins. Two domains in the same namespace is an error.
- `UseSqlServer`, `UseSqlite`, `UsePostgres`, `UseMySql` and `UseOracle` live in namespace `Jerrycurl.Razor` and come from the vendor packages. They set the connection factory, dialect and vendor defaults such as max parameters per batch.
- `DomainOptions` also exposes `ConnectionFactory`, `Dialect`, `Schemas` and `Sql` (`SqlOptions`: `Filters`, `MaxParameters`, `MaxSql`).

## Dependency injection

`@inject` directives in pages, and domains with constructor parameters, need an `IServiceProvider`:

```csharp
ProcEngine.Services = app.Services;                       // ambient default, simplest

ProcEngine.Default = new ProcEngine(app.Services);        // or replace the default engine

GetAll.Query(model, engine: new ProcEngine(scope.ServiceProvider)); // or pass an engine per call
```

Without a provider, a page with `@inject` throws `NotSupportedException`. `@project` directives don't need DI.

## Diagnostics

The generator reports these at the `.cssql` file and line:

| Id | Meaning |
|---|---|
| JC0001 | Page has both `@query` and `@command`. |
| JC0002 | `@output` / `@result` on a `@command`. |
| JC0003 | `@query` / `@command` in `_imports.cssql`. |
| JC0004 | Invalid directive syntax: arguments to `@query`/`@command`, an `@input`/`@output` property without type and name, or a string `@template "..."` (it takes a type now). |
| JC0005 | `@visibility` (or `JerrycurlPageVisibility`) isn't `public` or `internal`. |
| JC0006 | Page name clashes with a generated entry point. |
| JC0100 | Info: page has no `@query`/`@command` and is only usable as a partial. |

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `GetAll` / `GetAll.Query` doesn't exist | The page has no `@query`/`@command`, the file isn't a `JerryFile`, or you're calling an `internal` page from another assembly. |
| Domain not found | No `IDomain` in the page's namespace or any parent namespace. |
| `Multiple domains found in namespace` | Move one domain to a different namespace. |
| `Template cycle detected` | Two layouts use each other as `@template`. A layout that inherits *itself* from `_imports.cssql` is skipped automatically. |
| Generated SQL contains `else` or stray text | Write `@else`, not `else`. See jerrycurl-razor-sql. |
| Lambda errors such as "cannot use lambda with dynamic" | Page lacks `@model`/`@input` or `@result`/`@output`, so `M`/`R` are `dynamic`/`object`. |
| Values missing from result objects | Column aliases don't match property paths. Use `@R.Map()` or `AS @R.Prop(...)`, never hand-written aliases. |
| Database exception during `Query`/`Execute` | The stack trace includes a frame in the `.cssql` file (`...cssql:line N`) of the page that produced the SQL. |
| Source generator problems | Look for generated `jerry+<guid>.log` sources (they contain the exception). Set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` to inspect the generated code (one `<Folder>_<File>_cssql.g.cs` per page). |

## Tooling

The separate `dotnet-jerry` global tool (`dotnet tool install --global dotnet-jerry`) can generate `[Table]` model classes from an existing database schema and transpile `.cssql` files outside of MSBuild. Run `jerry help` for its current commands.
