---
name: jerrycurl-commands
description: How to write Jerrycurl command pages (.cssql files under Commands/) for INSERT, UPDATE, DELETE, upserts and DDL, executed with Accessor.Execute/ExecuteAsync. Covers iterating model values with M.Vals(), In()/Out()/Key() column sets, writing generated identity values back into the model (SELECT ... AS Prop(), OUTPUT/RETURNING), parent/child inserts with foreign-key cascade, batching limits and transactions. Use when adding or changing any write operation in a Jerrycurl project.
---

# Jerrycurl commands

A command is a `.cssql` page under `Commands/` executed by `this.Execute(model)` (or `ExecuteAsync`) in an accessor. The page iterates the model's values and writes one statement per value. Any result columns aliased with a value's `Prop()` are **written back into the model objects** after execution. Read **jerrycurl-razor-sql** first for syntax and escaping.

## Workflow

1. Create `Commands/<AccessorPrefix>/<MethodName>.cssql` with `@model TEntity`, the *item* type, even if the accessor passes a list.
2. Loop with `@foreach (var v in M.Vals()) { ... }`. This works for a single object or a list.
3. Build statements from `v` with `TblName()`, `In()`, `Key()`, `ColNames()`, `Pars()`.
4. If the database generates values (identity, defaults), select them back aliased with `Prop()`.
5. Accessor: `public void Create(IList<Blog> blogs) => this.Execute(blogs);`

## CRUD templates

```sql
-- Commands/Blogs/Create.cssql   (SQLite: identity via last_insert_rowid)
@model Blog
@foreach (var v in M.Vals())
{
    INSERT INTO @v.TblName() ( @v.In().ColNames() ) VALUES ( @v.In().Pars() );
    SELECT last_insert_rowid() AS @v.Id().Prop();
}
```

```sql
-- Commands/Blogs/Update.cssql
@model Blog
@foreach (var v in M.Vals())
{
    UPDATE @v.TblName()
    SET    @v.In().ColNames().Eq().Pars()
    WHERE  @v.Key().ColNames().IsEq().Pars();
}
```

```sql
-- Commands/Blogs/Delete.cssql
@model Blog
@foreach (var v in M.Vals())
{
    DELETE FROM @v.TblName() WHERE @v.Key().ColNames().IsEq().Pars();
}
```

Generated for two blogs (ISO quoting):

```sql
INSERT INTO "Blog" ( "Title", "Rating" ) VALUES ( @JP0, @JP1 );
SELECT last_insert_rowid() AS "Item[0].Id";
INSERT INTO "Blog" ( "Title", "Rating" ) VALUES ( @JP2, @JP3 );
SELECT last_insert_rowid() AS "Item[1].Id";
```

After `Execute(blogs)` returns, `blogs[0].Id` and `blogs[1].Id` hold the generated keys.

## Which columns each filter selects

| Filter | Contents (default rules) | Typical use |
|---|---|---|
| `In()` | Every mapped column except `[Id]` | INSERT column/value lists, UPDATE SET |
| `Out()` | `[Id]`, `[Key]` and `[Ref]` columns | OUTPUT / RETURNING lists |
| `Key()` | Primary `[Key]` columns | WHERE clauses for UPDATE/DELETE |
| `Id()` | The single `[Id]` attribute | Identity write-back |

Override per property with `[In]` / `[Out]` from `Jerrycurl.Mvc.Metadata.Annotations`. For example, mark a database-defaulted `CreatedOn` with `[Out]` so it's excluded from INSERTs but can be read back. A `[Key]` that isn't `[Id]` (a natural key) is part of `In()`, so it's inserted, and an UPDATE's SET list includes it (setting it to its current value).

## Writing generated values back

The model is updated from any result column whose alias is a value's `Prop()`, and from output parameters on dialects that support them. Pick the form your database supports:

```sql
-- SQLite / MySQL: separate SELECT
SELECT last_insert_rowid() AS @v.Id().Prop();          -- MySQL: LAST_INSERT_ID()

-- SQL Server: OUTPUT clause
INSERT INTO @v.TblName() ( @v.In().ColNames() )
OUTPUT      @v.Out().Cols("INSERTED").As().Props()
VALUES      ( @v.In().Pars() );

-- PostgreSQL / SQLite 3.35+: RETURNING
INSERT INTO @v.TblName() ( @v.In().ColNames() ) VALUES ( @v.In().Pars() )
RETURNING @v.Out().ColNames().As().Props();             -- or: @v.Id().ColName().As().Prop()
```

`Cols("INSERTED")` qualifies the columns with the `INSERTED` pseudo-table instead of a generated alias.

## Parent and child in one command (foreign-key cascade)

When a child's `[Ref]` property points at a parent `[Key]` in the **same model graph**, `Par()` on the child's FK adds a cascade binding: the child's FK parameter takes the parent's value, including a freshly generated identity. That only works if the parent's INSERT runs in an **earlier batch** than the child's, because a batch's parameters are bound before it executes.

```sql
-- Commands/Blogs/CreateWithPosts.cssql
@model BlogView
@foreach (var v in M.Vals())
{
    INSERT INTO @v.TblName() ( @v.In().ColNames() ) VALUES ( @v.In().Pars() );
    SELECT last_insert_rowid() AS @v.Id().Prop();
}
@foreach (var p in M.Vals(m => m.Posts))
{
    INSERT INTO @p.TblName() ( @p.In().ColNames() ) VALUES ( @p.In().Pars() );
    SELECT last_insert_rowid() AS @p.Id().Prop();
}
```

```csharp
// MaxSql = 1 forces a new batch at every Vals() item, so parents execute before children.
public void CreateWithPosts(BlogView blog) => this.Execute(blog, o => o.MaxSql = 1);
```

Afterwards both `blog.Id` and each `post.BlogId` / `post.Id` are populated. If everything runs in one batch, the children are inserted with the FK's **old** value (e.g. `0`). The alternative is two `Execute` calls, assigning FKs in C# in between.

## Batching

The SQL buffer can be split into several database round-trips:

- **Split points** come after each item of a `Vals()` loop (and manual `Context.Execution.Buffer.Mark()` calls).
- A split happens when a batch would exceed `SqlOptions.MaxParameters` or `MaxSql` (characters). Vendors set sensible defaults (SQL Server: 2098 parameters). Configure globally in the domain (`options.Sql.MaxParameters = ...`) or per call (`Execute(model, o => o.MaxParameters = 500)`).
- Each loop iteration must therefore be a **complete statement**. Do not open a block before the loop and close it after (e.g. `BEGIN ... END;`, an open transaction, or a temp table that later statements rely on) unless you're sure no split can occur, for example by raising the limits.
- Use `o => o.UseTransaction()` to make all batches of one `Execute` atomic.

## Transactions

```csharp
this.Execute(model, o => o.UseTransaction());                         // ADO.NET transaction
this.Execute(model, o => o.UseTransaction(IsolationLevel.Serializable));
this.Execute(model, o => o.UseTransactionScope());                    // System.Transactions
```

To span several accessor calls, wrap them in your own `TransactionScope` and pass `UseTransactionScope()` on each call.

## Other command patterns

- **No model** (DDL, maintenance): `this.Execute();` with `@project` for table names:
  ```sql
  @project Blog b
  @project BlogPost p
  DELETE FROM @p.TblName();
  DELETE FROM @b.TblName();
  ```
- **Conditional SET parts**: `@if (v.HasVal(m => m.Title)) { , @v.ColName(m => m.Title) = @v.Par(m => m.Title) } @else { ... }`.
- **Specific columns**: `@v.ColName(m => m.Rating) = @v.Par(m => m.Rating)`, or `@v.In().Filter(a => ...)`.
- **Reuse**: `@M.Subcommand("Shared/AuditLog")` renders another command page with the current model.
- **Upsert**: write vendor syntax (`ON CONFLICT`, `MERGE`) from the same projection pieces, e.g. `ON CONFLICT (@v.Key().ColNames()) DO UPDATE SET @v.In().ColNames().Eq().Pars()`. (`NonKey()` would also drop `[Ref]` foreign-key columns.)

## Review checklist

- [ ] `@model` is the item type, and the page iterates `M.Vals()` (never `Model` directly) to emit values.
- [ ] INSERTs use `In()` so identity columns are excluded, and generated ids are selected back with `Prop()`.
- [ ] UPDATE/DELETE filter with `Key()`. A model without a `[Key]` produces an empty WHERE, so check the model.
- [ ] Each loop body is a self-contained statement, and multi-statement atomicity uses `UseTransaction()`.
- [ ] Parent/child inserts put parents in an earlier batch (or use separate `Execute` calls).
