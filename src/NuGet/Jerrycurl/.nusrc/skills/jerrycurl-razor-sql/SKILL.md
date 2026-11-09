---
name: jerrycurl-razor-sql
description: Syntax and semantics of Jerrycurl Razor SQL (.cssql) files: directives (@model, @result, @import, @project, @inject, @template, @namespace, @class), C# code blocks, escaping of @ { } in SQL, _imports.cssql, the M/R projections, and how expressions turn into SQL text or parameters. Use whenever reading, writing, reviewing or debugging any .cssql file. For the full projection method list see references/projection-api.md.
---

# Razor SQL (.cssql)

A `.cssql` file is plain SQL with Razor-style C# mixed in. At build time a source generator turns each file into a class deriving from `Jerrycurl.Mvc.ProcPage<TModel, TResult>`. At runtime the page executes and writes into a buffer, which produces the final SQL text **and its parameters**.

```sql
-- Queries/Blogs/GetAll.cssql
@result BlogView
@model BlogFilter
@{
    var p = R.Open(m => m.Posts);
}

SELECT      @R.Map()
FROM        @R.Tbl()
WHERE       @R.Col(m => m.CreatedOn) >= @M.Par(m => m.FromDate)

SELECT      @p.Map()
FROM        @p.Tbl()
INNER JOIN  @R.Tbl() ON @R.Col(m => m.Id) = @p.Col(m => m.BlogId)
WHERE       @R.Col(m => m.CreatedOn) >= @M.Par(m => m.FromDate)
```

Generated SQL (ISO/SQLite quoting, condensed):

```sql
SELECT  JT0."Id" AS "Item.Id", JT0."Title" AS "Item.Title", JT0."CreatedOn" AS "Item.CreatedOn"
FROM    "dbo"."Blog" JT0
WHERE   JT0."CreatedOn" >= @JP0

SELECT  JT1."Id" AS "Item.Posts.Item.Id", JT1."BlogId" AS "Item.Posts.Item.BlogId", ...
FROM    "dbo"."BlogPost" JT1
INNER JOIN "dbo"."Blog" JT0 ON JT0."Id" = JT1."BlogId"
WHERE   JT0."CreatedOn" >= @JP0
```

Table aliases (`JT0`, `JT1`, ...), parameter names (`@JP0`, or `:JP0` on Postgres and Oracle) and identifier quoting (`"x"`, or `` `x` `` on MySQL) come from the projection and the configured dialect. Never hard-code them. Column aliases like `"Item.Posts.Item.Id"` are property paths that tell the query engine where each value goes.

## Directives

Directives take the rest of the line as their argument. By convention they go at the top of the file.

| Directive | Meaning |
|---|---|
| `@model TModel` | Compile-time type of the input projection `M` and the `Model` property. Default: `dynamic`. |
| `@result TResult` | Compile-time type of the output projection `R`. Default: `object`. For `Query<T>` this is `T`, the item type, not `IList<T>`. |
| `@import Some.Namespace` | Adds `using Some.Namespace;`. |
| `@project Type name` | Declares a property `IProjection<Type> name`, a standalone projection of `Type` for referencing other tables (e.g. `@name.TblName()`). |
| `@inject Type name` | Declares a property resolved from the DI container. Requires an `IServiceProvider` (see jerrycurl-mvc). |
| `@template "LayoutName"` | Wraps this page in a layout page (takes a C# string expression). The layout writes `@Render.Body()` where this page's SQL goes. |
| `@namespace X.Y` / `@class Name` | Override the generated namespace (default: root namespace + folder path) or class name (default: `FileName_cssql`). Changing these breaks name-based page lookup, so avoid them. |

Default usings in every page: `System`, `System.Collections.Generic`, `System.Linq.Expressions`, `Jerrycurl.Mvc`, `Jerrycurl.Mvc.Projections`, `Jerrycurl.Mvc.Sql`. **`System.Linq` is not imported**: add `@import System.Linq` (ideally in `_imports.cssql`) before using LINQ operators. Vendor-specific helpers need their namespace too, e.g. `@import Jerrycurl.Mvc.Sql.SqlServer` (`Tvp`, `Json`), `Jerrycurl.Mvc.Sql.Oracle` (`Refcursor`, `Json`), `Jerrycurl.Mvc.Sql.MySql` (`Json`).

### `_imports.cssql`

A file named `_imports.cssql` generates no page. Its `@import`, `@project` and `@inject` directives are prepended to **every page in the same folder and all subfolders**. Its `@template` applies to pages that have none of their own. Several `_imports.cssql` files stack, outermost first. `@model`, `@result`, `@namespace`, `@class` and SQL content in an imports file are ignored.

```sql
-- _imports.cssql at the project root
@import System.Linq
@import MyApp.Data.Database
@import MyApp.Data.Views
```

## Code constructs

| Syntax | Meaning |
|---|---|
| `@R.Col(m => m.Id)` | Inline expression. Continues through `.member`, `(...)`, `[...]`, `<...>` and stops at whitespace or any other character. |
| `@(a + b)` | Explicit expression, for anything with operators or spaces. |
| `@{ var x = ...; }` | C# statement block. Its contents are **pure C#**, not SQL. |
| `@if (...) { SQL }`, `@foreach`, `@for`, `@while`, `@using` | Control flow. The text between `{` and `}` is **SQL** (with nested Razor). |
| `@else { ... }`, `@else if (...) { ... }` | **The `@` is required.** A bare `else` after `}` is emitted into the SQL as the literal text `else`. |
| `@* comment *@` | Razor comment, removed from output. SQL comments (`--`, `/* */`) are kept in the output. |

Inside a block body you are in SQL, so nested control flow needs `@` again:

```sql
@model Blog
@foreach (var v in M.Vals())
{
    UPDATE @v.TblName()
    SET    @v.ColName(m => m.Rating) = @v.Par(m => m.Rating)
    @if (v.HasVal(m => m.Title))
    {
           , @v.ColName(m => m.Title) = @v.Par(m => m.Title)
    }
    @else
    {
           , @v.ColName(m => m.Title) = 'Untitled'
    }
    WHERE  @v.Key().ColNames().IsEq().Pars();
}
```

`HasVal` tests the current value for null. `HasId()` and `Any()` test **metadata** (does the type have an `[Id]` / any attributes), not data.

## Escaping: `@`, `{` and `}` are special in SQL text

A single `{` or `}` anywhere in SQL text is parsed as a C# block delimiter, and `@` followed by an identifier is parsed as code. Double them to get a literal character:

| You want in SQL | Write |
|---|---|
| `@localVar` (T-SQL variable) | `@@localVar` |
| `@@ROWCOUNT`, `@@IDENTITY` | `@@@@ROWCOUNT` |
| `'{"a":1}'` (JSON literal) | `'{{"a":1}}'` |
| `}` | `}}` |

This applies inside SQL string literals and SQL comments too. A stray `{` in a comment or string causes confusing compile errors in the generated code.

## Page members

| Member | Description |
|---|---|
| `M` | `IProjection<TModel>`, the input projection over the model passed by the accessor. |
| `R` | `IProjection<TResult>`, the output projection describing the result shape. It has metadata but no data. |
| `Model` | The raw model object (typed `TModel`). Use it for C# decisions (`@if (Model.IncludeDrafts)`), not to emit values. |
| `Render.Body()` | Only valid in a layout page. Writes the wrapped page's SQL. |
| `Context` | `IProcContext` (domain options, dialect, buffer). Rarely needed. |
| `WriteLiteral(string)` | Appends **raw, unescaped** SQL. Only use it for trusted, code-controlled text. |

## What `@expr` writes

- **Projections and attributes** (`IProjection`, `IProjectionAttribute`, and `ISqlContent` from `Subquery`/`Render.Body`) write their SQL fragment plus any attached parameters and bindings.
- **Any other value** (`@Model.Name`, `@someInt`, `@DateTime.Now`) becomes a **parameter** (`@JP0`), not inline text. Interpolating values is therefore injection-safe, and **you cannot build identifiers or SQL keywords from strings this way**. Branch with `@if`/`@switch`-style C#, or use projections, instead.
- `@M.Lit(m => m.X)` inlines numbers and NULL as literals. All other types silently fall back to a parameter.

## Projections in one minute

A projection (`IProjection<T>`) is an **immutable** list of attributes (one per mapped property) bound to table/column metadata, plus data for `M`. Methods either **navigate** (return a new projection or attribute) or **append** SQL to each attribute's buffer. Chains compose left to right, and writing the result emits all attributes separated by `, ` (or ` AND ` after `IsEq`).

| Intent | Example | Output (approx.) |
|---|---|---|
| Table with alias | `@R.Tbl()` | `"dbo"."Blog" JT0` |
| Table name only | `@R.TblName()` | `"dbo"."Blog"` |
| Alias only | `@R.Ali()` | `JT0` |
| Qualified column | `@R.Col(m => m.Id)` | `JT0."Id"` |
| Unqualified column | `@R.ColName(m => m.Id)` | `"Id"` |
| All columns | `@R.Cols()` / `@R.ColNames()` | `JT0."Id", JT0."Title"` / `"Id", "Title"` |
| Select-and-map all columns | `@R.Map()` (same as `Cols().As().Props()`, also `Star()`) | `JT0."Id" AS "Item.Id", ...` |
| Map a single value | `... AS @R.Prop(m => m.PostCount)` | `"Item.PostCount"` |
| Parameter from the model | `@M.Par(m => m.FromDate)` | `@JP0` |
| IN list | `IN (@M.ParList(m => m.Ids))` | `@JP0, @JP1, @JP2` |
| Navigate to a list item | `R.Open(m => m.Posts)` | new projection over `BlogPost` |
| Navigate to a property | `R.For(m => m.Author)` | new projection over `BlogAuthor` |
| Iterate model values | `@foreach (var v in M.Vals())` | one projection per item |
| Filter attributes | `.In()`, `.Out()`, `.Key()`, `.NonKey()`, `.Id()`, `.Filter(a => ...)` | subset |
| Combine | `@v.Key().ColNames().IsEq().Pars()` | `"Id" = @JP0 AND "Sub" = @JP1` |

Table aliases are generated per table path, so `@R.Tbl()` and `@R.Col(...)` always agree. Pass `tblAlias: "X"` to use your own alias, and pass the same alias to `Tbl` and `Col`/`Cols`/`Map`.

Full method reference: [references/projection-api.md](references/projection-api.md).

## Layouts (templates) and partials

```sql
-- Queries/Shared/Paged.cssql   (layout)
@model PageFilter
SELECT * FROM (
    @Render.Body()
) X
ORDER BY 1 OFFSET @M.Par(m => m.Skip) ROWS FETCH NEXT @M.Par(m => m.Take) ROWS ONLY
```

```sql
-- Queries/Blogs/Search.cssql
@template "Paged"
@result Blog
@model PageFilter
SELECT @R.Map() FROM @R.Tbl()
```

- The layout receives the same `M` and `R` as the page. Layouts can have their own `@template`, which forms a chain.
- There is **no cycle detection**. A layout that ends up with itself as its template (e.g. via an `_imports.cssql` `@template` covering the layout's own folder) overflows the stack. Keep layouts in a folder not covered by that imports file.
- `@R.Subquery("Name", model)` renders another query page inline, with the current `R` (or `R.For(...)`) as its result and `model` as its `M`.
- `@M.Subcommand("Name")` renders another command page inline with the current `M` (or `M.For(...)`) as its model.
- Partial and template names are resolved from the calling page's namespace, then the nearest `Queries`/`Commands` root, then its `Shared` folder.

## Pitfalls checklist

- [ ] `@else`, never bare `else`.
- [ ] No unescaped `{`, `}` or `@identifier` in SQL text, strings or comments.
- [ ] `@model` / `@result` match what the accessor passes (`Query<T>` means `@result T`; `Execute(list)` uses the list's item type as `@model` when you iterate `M.Vals()`).
- [ ] `@import System.Linq` before using LINQ.
- [ ] Never hand-write column aliases for mapped properties. Use `Map()` / `Prop()`.
- [ ] Values go through `Par`/`Pars`/`ParList` (or plain `@value`), never string building.
