---
name: jerrycurl-models
description: How to define Jerrycurl model classes and views: [Table], [Column], [Id], [Key], [Ref], [Json], [In]/[Out] annotations, composite keys, foreign-key references that drive one-to-many and one-to-one result binding, list and One<T> navigation properties, and view classes that extend table classes. Use when creating or changing entity/view classes used by .cssql pages, or when results come back with empty lists, missing children or wrong column sets.
---

# Jerrycurl models

Models are plain C# classes. Annotations from `Jerrycurl.Cqs.Metadata.Annotations` tell projections which **table and columns** a class maps to and which **keys** relate classes to each other. `[In]` / `[Out]` come from `Jerrycurl.Mvc.Metadata.Annotations`. Pages never hard-code table or column names: they read them from these annotations through projections (see **jerrycurl-razor-sql**).

## Table classes

```csharp
using Jerrycurl.Cqs.Metadata.Annotations;

[Table("dbo", "Blog")]                 // name parts: schema, table. [Table] alone uses the class name
public class Blog
{
    [Id, Key("PK_Blog")]               // identity column and primary key named "PK_Blog"
    public int Id { get; set; }
    public string Title { get; set; }  // column "Title"
    [Column("rating_value")]           // custom column name
    public int Rating { get; set; }
}

[Table("dbo", "BlogPost")]
public class BlogPost
{
    [Id, Key("PK_BlogPost")]
    public int Id { get; set; }
    [Ref("PK_Blog")]                   // foreign key -> the key named "PK_Blog"
    public int BlogId { get; set; }
    public string Headline { get; set; }
}
```

| Annotation | Effect |
|---|---|
| `[Table(params string[] parts)]` | Class maps to a table. Parts are joined with the dialect qualifier (`"dbo"."Blog"`). Inherited by subclasses. |
| `[Column]` / `[Column("name")]` | Property is a column, optionally renamed. Only needed for renames or for properties *not* declared on the `[Table]` class. |
| `[Id]` | Database-generated identity. Excluded from `In()` (inserts/updates) and included in `Out()`; `Id()` returns it. |
| `[Key]`, `[Key("Name")]`, `[Key("Name", index)]` | Member of a key. The name defaults to the **property name**. Same name plus `Index` makes a composite key. `IsPrimary` defaults to `true`, so set `IsPrimary = false` for alternate/candidate keys. `Key()` selects primary-key columns. |
| `[Ref]`, `[Ref("KeyName")]`, `[Ref("KeyName", index, "FkName")]` | Foreign key to the key whose name **equals** `KeyName` (ordinal). `KeyName` defaults to the property name. `FkName` distinguishes several references to the same key. |
| `[Json]` | Property (or class) is stored as JSON in one column. Requires `options.UseJson()` (Jerrycurl.Extensions.Json) or `options.UseNewtonsoftJson()` in the domain. |
| `[In]` / `[Out]` | Override input/output classification: `[Out]` alone excludes a column from INSERT/UPDATE but keeps it readable (e.g. DB defaults). `[In, Out]` makes an `[Id]` insertable. |

**Which properties are columns:** properties declared on the class that carries `[Table]`, plus anything marked `[Column]`. Properties added by a subclass (a view) are **not** columns. Navigation properties are never columns.

## Keys and references: how matching works

Child rows are attached to parents by matching a child's `[Ref]` to a parent's `[Key]` **by name** (and by `Index` for composites):

```csharp
[Key("PK_Blog")] int Id          <->  [Ref("PK_Blog")] int BlogId       // explicit names (recommended)
[Key] int NaturalId              <->  [Ref] int NaturalId               // both default to "NaturalId"
[Key] int Id                     <->  [Ref] int BlogId                  // NO MATCH: "Id" != "BlogId"
```

Composite keys:

```csharp
public class Order
{
    [Key("PK_Order", 1)] public string Region { get; set; }
    [Key("PK_Order", 2)] public int Number { get; set; }
    public IList<OrderLine> Lines { get; set; }
}
public class OrderLine
{
    [Ref("PK_Order", 1)] public string OrderRegion { get; set; }
    [Ref("PK_Order", 2)] public int OrderNumber { get; set; }
}
```

A property can be both key and reference (a 1:1 extension table sharing its parent's key):

```csharp
[Table("dbo", "BlogAuthor")]
public class BlogAuthor
{
    [Key("PK_BlogAuthor"), Ref("PK_Blog")]
    public int BlogId { get; set; }
    public string Name { get; set; }
}
```

Self-references (e.g. `[Ref("PK_Category")] int? ParentId` on `Category`) are supported.

## Views: composing result shapes

A view extends a table class and adds navigation and computed properties. The table mapping is inherited.

```csharp
public class BlogView : Blog
{
    public BlogAuthor Author { get; set; }        // one-to-one: map in the same row via R.Map(m => m.Author)
    public IList<BlogPostView> Posts { get; set; } // one-to-many: separate result set via R.Open(m => m.Posts)
    public int PostCount { get; set; }            // computed: alias with AS @R.Prop(m => m.PostCount)

    public class BlogPostView : BlogPost
    {
        public IList<BlogComment> Comments { get; set; }   // nested further: p.Open(m => m.Comments)
    }
}
```

| Navigation type | Filled from | Empty value |
|---|---|---|
| `T` (object) | Columns in the **same row** (`R.Map(m => m.Nav)` + join) | `null` |
| `IList<T>`, `List<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `IEnumerable<T>` | A **separate result set** joined by `[Key]`/`[Ref]` | Empty list |
| `One<T>` (`Jerrycurl.Relations`) | A separate result set, at most one item | `One<T>` with `HasValue == false` |

Separate result sets require the parent to have a `[Key]` and the child a matching `[Ref]`. Without them, children cannot be attached and lists stay empty.

Non-table classes (filters, parameter bags, DTOs without `[Table]`) work fine as `@model` for `Par()` / `ParList()`. They just have no `Tbl()` / `Col()`.

## Generating models

The `dotnet-jerry` CLI can generate `[Table]` classes with `[Id]`, `[Key]` and `[Ref]` annotations (named after the database constraints) from an existing schema. Run `jerry help` for commands. Keep views in separate files so that regenerating the models doesn't overwrite them.

## Checklist

- [ ] Every table class has `[Table]`, and every identity column has `[Id]`.
- [ ] Every parent of a list has a named `[Key]`, and the child's FK has `[Ref]` with the **same name**.
- [ ] Views inherit table classes rather than redeclaring columns.
- [ ] Extra view properties are mapped with `Prop()` in queries, not added as `[Column]`. A `[Column]` property becomes part of `In()`, so inserts would include it.
- [ ] `[Json]` properties have a JSON serializer registered in the domain.
