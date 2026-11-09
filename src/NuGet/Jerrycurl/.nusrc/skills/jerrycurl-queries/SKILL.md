---
name: jerrycurl-queries
description: How to write Jerrycurl query pages (.cssql files under Queries/) and the accessor methods that call them. Covers SELECT mapping with R.Map(), joins, one-to-one and one-to-many (multiple result sets joined by [Key]/[Ref]), filters and IN lists with parameters, optional WHERE clauses, scalar and computed columns, Aggregate<T> for multi-list objects, paging layouts, subqueries and streaming. Use when adding or changing any read operation in a Jerrycurl project.
---

# Jerrycurl queries

A query is a `.cssql` page under `Queries/` plus an accessor method calling `Query<T>`, `One<T>`, `Aggregate<T>` or `Enumerate<T>`. The page writes SQL whose **column aliases are property paths**. The query engine uses those paths to build objects, so always produce aliases with `R.Map()` / `R.Prop()`. Read **jerrycurl-razor-sql** first for syntax and escaping rules.

## Workflow

1. Choose the result type `T` (a `[Table]` class or a view composed from them; see **jerrycurl-models**).
2. Create `Queries/<AccessorPrefix>/<MethodName>.cssql` with `@result T` and, if it takes input, `@model TFilter`.
3. Add the accessor method: `public IList<T> MethodName(...) => this.Query<T>(model: filter);`
4. Map columns with `@R.Map()` (or `@x.Map()` for navigated projections) and reference tables and columns only through projections.

## Basic list

```sql
-- Queries/Blogs/GetAll.cssql
@result Blog
SELECT  @R.Map()
FROM    @R.Tbl()
ORDER BY @R.Col(m => m.Title)
```

```csharp
public IList<Blog> GetAll() => this.Query<Blog>();
```

## Parameters and filters

Use a filter class as `@model` so lambdas are typed. Values become parameters; nothing is string-concatenated.

```csharp
public class BlogFilter
{
    public int? MinRating { get; set; }
    public IList<int> Ids { get; set; }
    public string Title { get; set; }
}
```

```sql
-- Queries/Blogs/Search.cssql
@result Blog
@model BlogFilter
SELECT  @R.Map()
FROM    @R.Tbl()
WHERE   1 = 1
@if (Model.MinRating != null)
{
    AND @R.Col(m => m.Rating) >= @M.Par(m => m.MinRating)
}
@if (Model.Ids?.Any() == true)
{
    AND @R.Col(m => m.Id) IN (@M.ParList(m => m.Ids))
}
@if (Model.Title != null)
{
    AND @R.Col(m => m.Title) LIKE '%' || @Model.Title || '%'
}
```

- `Model` is the raw filter object for C# decisions. `M` is its projection for emitting parameters.
- `ParList` expands to one parameter per item. Guard against empty lists, because `IN ()` is invalid SQL.
- `@Model.Title` (any plain C# value) is emitted as a parameter.
- `Any()` needs `@import System.Linq` (put it in `_imports.cssql`). Remember `@else`, not `else`.

## One-to-one in the same row

```csharp
public class AuthoredBlog : Blog { public BlogAuthor Author { get; set; } }
```

```sql
@result AuthoredBlog
SELECT     @R.Map(),
           @R.Map(m => m.Author)
FROM       @R.Tbl()
LEFT JOIN  @R.Tbl(m => m.Author) ON @R.Col(m => m.Author.BlogId) = @R.Col(m => m.Id)
```

`R.Tbl(m => m.Author)` gets its own alias, so self-joins and repeated tables don't collide. When the LEFT JOIN finds no row, `Author` is `null`.

## One-to-many: one result set per list

Lists are filled from **separate SELECT statements** in the same page. The engine joins each child row to its parent through matching key metadata: `[Key("PK_Blog")]` on the parent and `[Ref("PK_Blog")]` on the child's foreign key (see **jerrycurl-models**).

```csharp
public class BlogView : Blog
{
    public int PostCount { get; set; }
    public IList<BlogPost> Posts { get; set; }
}
```

```sql
-- Queries/Blogs/GetViews.cssql
@result BlogView
@model BlogFilter
@{
    var p = R.Open(m => m.Posts);   // projection over BlogPost, the list item
}
SELECT  @R.Map(),
        (SELECT COUNT(*) FROM @p.TblName() X WHERE X.BlogId = @R.Col(m => m.Id)) AS @R.Prop(m => m.PostCount)
FROM    @R.Tbl()
WHERE   @R.Col(m => m.Rating) >= @M.Par(m => m.MinRating);

SELECT  @p.Map()
FROM    @p.Tbl()
INNER JOIN @R.Tbl() ON @R.Col(m => m.Id) = @p.Col(m => m.BlogId)
WHERE   @R.Col(m => m.Rating) >= @M.Par(m => m.MinRating);
```

- The child SELECT must include the `[Ref]` column. `@p.Map()` does that automatically.
- Repeat the parent's filter in the child query (via the join) so you don't fetch children of parents you never selected.
- Parents without children get an **empty list**, not `null`.
- Nest further with `p.Open(m => m.Comments)` and a third SELECT.
- `One<T>` properties (from `Jerrycurl.Relations`) work like a list of at most one item: `R.Open(m => m.Author)` plus a separate SELECT. Read with `.HasValue` / `.Value` / `.ValueOrDefault`.
- Separate statements with `;` when the database requires it. On Oracle, use `OPEN @R.Refcursor() FOR ...` (import `Jerrycurl.Mvc.Sql.Oracle`).

## Extra and computed columns

`Map()` only covers mapped table columns. Alias anything else with `Prop`:

```sql
SELECT @R.Map(), COUNT(*) OVER () AS @R.Prop(m => m.TotalCount) FROM @R.Tbl()
```

## Scalars

```sql
-- Queries/Blogs/Count.cssql
@result int
@project Blog b
SELECT COUNT(*) AS @R.Prop() FROM @b.TblName()
```

```csharp
public int Count() => this.Query<int>().FirstOrDefault();
```

`@project Blog b` declares a projection of a type other than `R`/`M`, useful for referencing tables in scalar or utility queries.

## Single row

```csharp
public Blog GetById(int id) => this.One<Blog>(new Blog { Id = id });
```

```sql
@result Blog
@model Blog
SELECT @R.Map() FROM @R.Tbl() WHERE @R.Col(m => m.Id) = @M.Par(m => m.Id)
```

`One<T>` uses `SingleOrDefault`, so it **throws when more than one item** results. With one-to-many views, the result count is the number of *parents*, so `One<BlogView>` is fine.

## Aggregate: one object from several result sets

```csharp
public class Dashboard
{
    public IList<Blog> Blogs { get; set; }
    public IList<BlogPost> Posts { get; set; }
    public int TotalPosts { get; set; }
}

public Dashboard GetDashboard() => this.Aggregate<Dashboard>();
```

```sql
@result Dashboard
@{
    var b = R.Open(m => m.Blogs);
    var p = R.Open(m => m.Posts);
}
SELECT @b.Map() FROM @b.Tbl();
SELECT @p.Map() FROM @p.Tbl();
SELECT COUNT(*) AS @R.Prop(m => m.TotalPosts) FROM @p.TblName();
```

Use `Aggregate<T>` when the result is **one** object assembled from several result sets. Open each list with `R.Open(...)`, and map scalar members with `AS @R.Prop(m => m.X)`.

## Layouts (shared wrappers such as paging)

```sql
-- Queries/Shared/Paged.cssql  (SQL Server syntax)
@model PageFilter
SELECT * FROM (
    @Render.Body()
) X
ORDER BY 1
OFFSET @M.Par(m => m.Skip) ROWS FETCH NEXT @M.Par(m => m.Take) ROWS ONLY
```

```sql
-- Queries/Blogs/List.cssql
@template "Paged"
@model PageFilter
@result Blog
SELECT @R.Map() FROM @R.Tbl()
```

The layout and the page share the same `M` and `R`. The inner SELECT's aliases pass through `SELECT *`.

## Subqueries

`@R.Subquery("Name", model)` renders another query page inline, with the current result projection as its `R`. Use it to reuse filter fragments or CTE bodies. The partial's `@model` is the object you pass.

## Streaming and async

- `QueryAsync<T>`, `OneAsync<T>` and `AggregateAsync<T>` take an optional `CancellationToken`. The page name drops the `Async` suffix.
- `Enumerate<T>` / `EnumerateAsync<T>` stream items without buffering. Use them for large, flat results, and use `Query<T>` for views that assemble lists from several result sets.

## Review checklist

- [ ] `@result` matches the accessor's `T`, and `@model` matches the model's type.
- [ ] Every mapped column goes through `Map()`/`Prop()`. No hand-typed aliases or table names.
- [ ] Child result sets include the `[Ref]` column, and parent/child key metadata matches.
- [ ] Optional clauses use `@if` / `@else`, and empty lists are guarded before `ParList`.
- [ ] Parent filters are repeated in child SELECTs.
