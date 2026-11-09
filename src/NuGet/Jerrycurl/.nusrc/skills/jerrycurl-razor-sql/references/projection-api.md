# Projection API reference (`Jerrycurl.Mvc.Sql`)

All methods are extension methods in namespace `Jerrycurl.Mvc.Sql`, which every `.cssql` page imports by default. Projections and attributes are **immutable**. Each call returns a new value whose SQL buffer has text, parameters or bindings appended.

- `IProjection<T>` / `IProjection`: a list of attributes (the *header*), one per mapped property of `T`. Writing it emits each attribute's buffer joined by the separator (default `,` + newline; `IsEq()` switches to `AND`, `Eq()` back to `,`).
- `IProjectionAttribute`: a single property, column or value.
- Most methods have three forms: on an attribute, on a projection (acting on the projection's own attribute), and on `IProjection<TModel>` with a lambda selecting a property (`R.Col(m => m.Id)`).

Outputs below use ISO quoting (`"x"`). Aliases look like `JT0` and parameters like `@JP0`. Both are dialect-dependent.

## Navigation

| Method | Returns | Description |
|---|---|---|
| `For(m => m.Prop)` | `IProjection<TProp>` | Navigate to a property: a nested object, or a list for `Vals`. |
| `For(m => m.List, i => i.Prop)` | `IProjection<TProp>` | Navigate into a list item's property. |
| `Open()` / `Open(m => m.List)` | `IProjection<TItem>` | Navigate to the item of a list (one-to-many). Throws if the target isn't a list. |
| `Attr()` / `Attr(m => m.Prop)` | `IProjectionAttribute` | The attribute for the projection itself or a property. |
| `Attrs()` / `Attrs(m => m.Prop)` | `IEnumerable<IProjectionAttribute>` | The header attributes. |
| `Cast<T>()` | `IProjection<T>` | Re-type an untyped projection. Metadata is unchanged. |

## Filtering the header

| Method | Keeps attributes that are... |
|---|---|
| `In()` / `In(m => m.X)` | Input: all mapped columns **except** `[Id]` (identity) columns, unless overridden with `[In]` / `[Out]`. Use for INSERT columns and UPDATE SET lists. |
| `Out()` / `Out(m => m.X)` | Output: `[Id]` columns, `[Key]`/`[Ref]` columns, and anything marked `[Out]`. Use for OUTPUT/RETURNING lists. |
| `Key()` / `NonKey()` | Primary-key columns (`[Key]`, where `IsPrimary` defaults to true) / columns that are neither keys nor `[Ref]`s. |
| `Id()` | Returns the single `[Id]` **attribute** (not a projection). |
| `Filter(a => bool)` | Custom predicate over `IProjectionAttribute` (e.g. `a.Metadata`, `a.Data`). |

## Tables

| Method | Output |
|---|---|
| `Tbl()` / `Tbl(m => m.Nav)` / `Tbl(tblAlias: "X")` | `"dbo"."Blog" JT0` (correlated table with alias) |
| `TblName()` / `TblName(m => m.Nav)` | `"dbo"."Blog"` (for INSERT/UPDATE/DELETE targets) |
| `Ali()` / `Ali(m => m.Nav)` | `JT0` |

## Columns

| Method | Output |
|---|---|
| `Col(m => m.X)` / `Col()` | `JT0."X"` (qualified) |
| `ColName(m => m.X)` / `ColName()` | `"X"` (unqualified) |
| `Cols()` / `Cols(m => m.Nav)` | `JT0."A", JT0."B", ...` |
| `ColNames()` / `ColNames(m => m.Nav)` | `"A", "B", ...` |

All qualified forms accept `tblAlias` to replace the generated alias. Use the same alias in `Tbl(tblAlias: ...)`.

## Mapping results (queries and write-back)

| Method | Output |
|---|---|
| `Map()` / `Map(m => m.Nav)` (alias `Star`) | `JT0."A" AS "Item.A", JT0."B" AS "Item.B", ...` (shorthand for `Cols().As().Props()`) |
| `Prop()` / `Prop(m => m.X)` | On `R` (no data): `"Item.X"`, the property path the engine maps the column into. On a model **value** (from `Vals()`): a write-back alias like `"Item[0].Id"` that updates that object's property after the command runs. |
| `Props()` / `Props(m => m.Nav)` | Multiple of the above. |
| `As()` | Appends ` AS `. |

Only properties declared on a `[Table]` class (or marked `[Column]`) are columns, so `Map()` on a view type maps the table columns only. Map extra view properties explicitly: `COUNT(*) AS @R.Prop(m => m.PostCount)`.

## Values and parameters

| Method | Output |
|---|---|
| `Par()` / `Par(m => m.X)` | `@JP0`, bound to the model value. Output columns also get write-back and cascade bindings. |
| `Pars()` / `Pars(m => m.Nav)` | `@JP0, @JP1, ...` for each header attribute |
| `ParList(m => m.Ids)` / `ParList()` | `@JP0, @JP1, @JP2`, one parameter per item of a list value (for `IN (...)`) |
| `Lit()` / `Lits()` / `LitList(m => m.Ids)` | Inline literals for numbers and NULL. Everything else falls back to a parameter. |
| `Var()` / `Vars()` | A stable SQL variable name per value (`@V0`), for scripts that declare variables |
| `Vals()` / `Vals(m => m.List)` | `IEnumerable<IProjection<T>>`, one projection per model item (works for a single object or a list). Each iteration is a batch split point. |
| `Val()` / `Val(m => m.X)` | Projection over a single value. Throws if the value is missing. |
| `ValList(a => ...)` | Comma-joined custom rendering of each item of a list value |

Writing any non-projection C# value (`@Model.Name`) also creates a parameter.

## Operators

| Method | Output |
|---|---|
| `Eq()` (attribute) | ` = ` |
| `Eq()` (projection) | Appends ` = ` to each attribute; separator `,` (for `SET a = @p, b = @q`). |
| `IsEq()` (projection) | Appends ` = ` to each attribute; separator `AND` (for `WHERE a = @p AND b = @q`). |
| `IsNull()` / `IsNotNull()` | ` IS NULL` / ` IS NOT NULL` |

Typical chains:

```sql
INSERT INTO @v.TblName() (@v.In().ColNames()) VALUES (@v.In().Pars())
UPDATE @v.TblName() SET @v.In().ColNames().Eq().Pars() WHERE @v.Key().ColNames().IsEq().Pars()
DELETE FROM @v.TblName() WHERE @v.Key().ColNames().IsEq().Pars()
```

## Booleans (for `@if`)

| Method | True when |
|---|---|
| `HasVal()` / `HasVal(m => m.X)` | The value is non-null (data check). |
| `Any()` | The projection has any attributes (metadata check). |
| `HasId()` | The type has an `[Id]` attribute (metadata check). |

## Partials

| Method | Description |
|---|---|
| `R.Subquery("Name", model)` / `R.Subquery(m => m.Nav, "Name", model)` | Render another query page inline, with the current `R` as its result and `model` as its `M`. |
| `M.Subcommand("Name")` / `M.Subcommand(m => m.Nav, "Name")` | Render another command page inline, with the current `M` as its model. |

## JSON

| Method | Output |
|---|---|
| `JsonPath()` / `JsonPath(m => m.X)` | JSON path literal for a property inside a `[Json]` column, e.g. `'$.my.value'`. |
| Vendor `Json(...)` | Vendor-specific JSON extraction. Import `Jerrycurl.Mvc.Sql.SqlServer`, `.Oracle` or `.MySql`. |

## Vendor-specific (import the vendor namespace)

| Namespace | Methods |
|---|---|
| `Jerrycurl.Mvc.Sql.SqlServer` | `Tvp(m => m.List)`, `TvpName(...)` for table-valued parameters; `Json(...)` |
| `Jerrycurl.Mvc.Sql.Oracle` | `Refcursor()` for `OPEN ... FOR` result sets; `Json(...)` |
| `Jerrycurl.Mvc.Sql.MySql` | `Json(...)` |
