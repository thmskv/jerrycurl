# Jerrycurl net11 performance pass: change catalogue

Every proposed change is numbered. Each entry has the current code (**Before**), the proposed code (**After**), a **Diff**, where it runs, and the expected gain. Reply with the numbers you want applied, for example `1.1, 1.2, 2.1, 3.1-3.10, 5.*`.

**Gain scale**
- **High:** removes allocations, boxing, locking or quadratic work on a per-row, per-cell or per-parameter path.
- **Medium:** per query, per result set or per render.
- **Low:** one-time per type or compile, cold code, or micro-savings.

There is no benchmark project, so these ratings come from how the .NET runtime and JIT behave, not from measurements.

**Ground rules (apply to every entry)**
- No public or protected signature changes. Virtual and interface members are still called; some are read once into a local.
- Behavior is identical unless the entry has a **Caveat**. Section 5 (bug fixes) is the exception, and each fix gets its own commit.
- `src/Shared/Jerrycurl.Common` is also compiled into the netstandard2.0 `src/Build` projects, so anything net-only there goes under `#if NET`.
- Work happens on a new branch, `perf/net11-pass`, cut from `feat/relaunch-pages`. There is roughly one commit per entry group.

**Dependencies and overlaps**
- **1.2 includes 5.2**, because the list compare gains a count check, and **makes 5.1 moot**, because `Equality.cs` is deleted.
- **2.1 and 1.2:** if both are applied, `MetadataKey` becomes a struct and its `Equals` drops the null guard.
- **3.1 and 6.1:** 3.1 bypasses the `GetOrAdd` helper inside `CommandBuffer`, and 6.1 fixes the helper itself for its other callers. They are independent.
- **Entries with behavior notes:**
  - 2.1: a recursive *hit* on a cached entry no longer throws.
  - 2.16: Sqlite type names now match under the Turkish culture.
  - 3.11: a null session is still skipped on dispose.
  - 4.16: `Text` is cached and parameters are re-walked on each enumeration.
  - 6.6: `CloseAsync` is used instead of `Close`.
  - 5.x: every entry in section 5.

**Verification after applying**
- `dotnet build jerrycurl.slnx -c Release`, which includes the netstandard2.0 Build projects.
- `dotnet test` for the Relations, Cqs, Mvc and Vendors.Sqlite unit tests. These run against a local SQLite file with no server.
- Each section 5 fix gets a regression test.

## Index

| # | Change | Runs | Gain |
|---|---|---|---|
| 1.1 | Replace `Jerrycurl.Diagnostics.HashCode` with `System.HashCode` | every cache-key and identity hash (per lookup; per row for join keys) | Medium |
| 1.2 | Replace the `Equality` helper with direct comparisons | every cache-key and identity compare; per row for multi-column join keys (`CompositeKey`) | High on join-heavy queries, Medium elsewhere |
| 2.1 | `Schema.Lookup<T>`: `MetadataKey` struct, lock-free fast path and a re-check under the write lock | per lookup | High |
| 2.2 | `SchemaStore.Builders`: cached array, and an `is` loop in `Lookup` | per lookup (miss path only) | Low |
| 2.3 | `RelationCache.GetWriter`: static factory lambda | per query (each `RelationReader.Read` start) | Medium |
| 2.4 | `RelationReader`: cache `Degree` when reading starts, and use the buffer local in `ReadNext` | per cell (indexer) and per row (`ReadNext`) | Medium |
| 2.5 | `RelationDataReader`: read `InnerReader[i]` once per call | per cell | Low |
| 2.6 | `Field.Commit`: reuse the `typedValue` local | per changed field on commit | Low |
| 2.7 | `RelationQueue`: cache the head item in a field | per row (generated code reads `List`/`Index`/`GetFieldName`; `MoveNext` reads `CurrentItem`) | Low |
| 2.8 | Deep tier: `RelationCompiler` compiles straight to `Action<RelationBuffer>` | per row (each queue writer) and per reader (initializer) | Medium |
| 2.9 | `DotNotation`: span and `IndexOf` paths for `Member`, `Depth`, `Path` and `Combine` | mostly per metadata/compile; `Combine(a, b)` runs per row for source-level fields and per `MetadataIdentity.Push` | Low |
| 2.10 | `DotNotation.Parent`: `Substring` fast path, with a fallback for empty segments | per `MetadataIdentity.Pop`/`FieldIdentity.Pop` (metadata building, Cqs `NodeParser` dynamic lookup) | Low |
| 2.11 | `MetadataIdentity.Equals(string)`: compare inline instead of allocating | per call (public API; no hot internal callers today) | Low |
| 2.12 | `DefaultRelationContractResolver`: `static readonly` type arrays and one indexer lookup | one-time (per metadata node) | Low |
| 2.13 | `RelationMetadataBuilder.GetContract`: reverse `for` loop instead of `Concat().Reverse()` | one-time (per metadata node, two or three times per node) | Low |
| 2.14 | `NodeTree.FindNode`: dictionary lookup | one-time (per relation compile; cached by `RelationCache`) | Low |
| 2.15 | `RelationCompiler.IsRunningNetFramework`: a `static readonly bool` with an ordinal comparison | one-time (per empty block during compile) | Low |
| 2.16 | `.ToLower()` → `.ToLowerInvariant()` (`RelationCompiler` variable names, `SqliteContractResolver`) | one-time (per compiled variable) / per query compile (per SQLite column) | Low |
| 3.1 | Single-lookup `GetOrAdd` in `CommandBuffer.Add` | per parameter | High |
| 3.2 | Direct source priority in `FieldBuffer` and `CascadeSource` | per parameter | High |
| 3.3 | Static converter factory in `CommandCache.GetConverter` | per parameter (each bound field on `Commit`) | Medium |
| 3.4 | Non-capturing factories in `QueryCache` | per query (per result set) | Medium |
| 3.5 | Hoist `GetDbType` out of the parameter `Write` contract | per parameter | Low |
| 3.6 | Static type arrays and `MethodInfo`s in `DefaultBindingContractResolver` | one-time (per metadata or compile) | Low |
| 3.7 | `HashSet` dedupe instead of `GroupBy` in `Query.Build` | per parameter | High |
| 3.8 | Tidy the column loop in `CommandBuffer.GetUpdateAction` | per query (per result set) | Low |
| 3.9 | Single-probe dedupe and presizing in `CommandBuffer.Prepare` | per parameter | Low |
| 3.10 | `StringBuilder` in `RelationExtensions.AsCommand(textBuilder)` | per row | High |
| 3.11 | Close `ConfigureAwait(false)` gaps | per row (`ReadAsync`) / per query | Low |
| 3.12 | Skip the async state machine in `AsyncSession.ApplyFilters` when there are no filters | per query (connection open and per command, about 4 calls) | Low |
| 3.13 | `System.Threading.Lock` in `BufferCache` | one-time (per query shape, during parsing) | Low |
| 3.14 | Deep tier: bake `helpers`/`schema` in as constants and compile straight to the public delegates | per row (`EnumerateFactory`, `ListFactory.WriteOne`) / per result set (`WriteAll`, `Initialize`, aggregate) | Medium |
| 3.15 | Deep tier: single-lookup join buffer via `CollectionsMarshal` | per row (per join) | Medium |
| 3.16 | Deep tier: `ElasticArray` backed by a growable array | per cell (every generated list/aggregate/helper/join slot access), plus one allocation per new join key | Medium |
| 3.17 | Deep tier: cache reflection lookups in `QueryCompiler` and `CommandCompiler` | one-time (per query shape compile) | Low |
| 3.18 | Deep tier: bake the helper object into the `CommandCompiler` converter | per parameter (each bound field on `Commit`, via `FieldBuffer.Bind`) | Medium |
| 4.1 | Cache the domain type per origin type in ProcLocator | per query | Medium |
| 4.2 | Static lambdas in FindPage and FindDomain | per query | Low |
| 4.3 | Ordinal suffix and prefix checks in ProcLocator | one-time | Low |
| 4.4 | ProcEngine per-call allocations (GetOrAdd lambdas, options lookup, execution array) | per query | Low |
| 4.5 | Cache compiled page constructors per page type | one-time | Low |
| 4.6 | Single hash lookup per name in ProcLookup.FromKey | per parameter | Medium |
| 4.7 | TryPeek in ProcExecutionStack.Push | per render | Low |
| 4.8 | Read IsoDialect's virtual quote and prefix once | per parameter | Low |
| 4.9 | Static readonly separator strings | per render | Low |
| 4.10 | Loop over table name parts in TblName | per render | Low |
| 4.11 | Copy parameters and bindings once in SqlBuffer.Read and ProcBuffer | per query | Medium |
| 4.12 | SqlBuffer.ReserveBuffer and ReadToEnd sizing | per query | Low |
| 4.13 | Build the attribute directly in ProcPage.Write<T> | per parameter | Low |
| 4.14 | Read the execution buffer once per ProjectionValues enumeration | per item | Low |
| 4.15 | Deep tier: index-filled row data and an array row header in Vals | per item | High |
| 4.16 | Deep tier: lazy append chain in SqlContent (fixes quadratic ParList/LitList) | per item | High |
| 5.1 | `Equality.CombineAll(IEnumerable, IEnumerable)`: inverted equality check | never today (latent) | none (correctness) |
| 5.2 | `Equality.AddRange`: lists of different lengths compare equal | per query-cache and buffer-cache hit | none (correctness) |
| 5.3 | `ProjectionAttribute.With`: inverted metadata condition | per `Append` (per column/parameter per render) | Medium (side effect of the fix) |
| 5.4 | `IsoDialect.String`: `NullReferenceException` on null | per string literal | none (correctness) |
| 5.5 | `MetadataExtensions.HasFlag(ReferenceMetadataFlags)`: wrong null check | per `Key()`/`NonKey()` header filter | none (correctness) |
| 6.1 | Closure-free `DictionaryExtensions.GetOrAdd` (value and `new()` overloads) | per query (CommandBuffer, per parameter); cold for BufferCache (query compile) | Low |
| 6.2 | Retire the shadowed `GetValueOrDefault` / `TryAdd` / `TryRemove` polyfills | cold (metadata lookup) | Low |
| 6.3 | `EnumerableExtensions.IndexOf` via `Enumerable.Index`; delete unused helpers | cold | Low |
| 6.4 | `FieldArray.EnsureIndex` via `CollectionsMarshal.SetCount` | per row (cached relation queues) | Low |
| 6.5 | Guard clauses → `ArgumentNullException.ThrowIfNull` | per row (`Field` ctor), per query (sessions/engines), one-time elsewhere | Low |
| 6.6 | `AsyncSession.DisposeAsync`: `Close()` → `CloseAsync()` | per query (one async session per Execute/Enumerate) | Low |
| 6.7 | `ToolRunner.RunAsync`: `Task.Run(WaitForExit)` + TCS/`Task.Delay` → `Process.WaitForExitAsync` | cold (one Node.js transform per `jerry orm` run) | Low |
| 6.8 | `ToolRunner`: hand-built argument string → `ProcessStartInfo.ArgumentList` | cold | Low |
| 6.9 | `ContainsKey` + `Add`/indexer double lookups → `Dictionary.TryAdd` | one-time per reader; per row in the ORM tool | Low |
| 6.10 | Pre-computed `Lazy<T>` → `Lazy<T>(T value)` constructor | cold (once per projection metadata) | Low |
| 6.11 | `CSharp.Identifier`: LINQ char array → `string.Create` | cold (codegen: Razor/ORM, `Namespace` ctor per part) | Low |
| 6.12 | Static `Regex.*` calls in Tools → `[GeneratedRegex]` | cold (per line of the diffed file in `jerry orm diff`) | Low |

---

## 1. Hashing and equality: switch to the native .NET APIs

`src/Shared/Jerrycurl.Common/Diagnostics` holds two hand-written helpers that every cache key and identity type in Relations, Cqs and Mvc goes through:
- `HashCode`: a 444-line copy of the xxHash32 code behind `System.HashCode`;
- `Equality`: a struct that combines comparisons through delegates and boxes value types.

The netstandard2.0 Build projects compile both files but never use them, so both can be **deleted** and replaced with `System.HashCode` and `EqualityComparer<T>.Default`. No `#if` is needed.

### 1.1 Replace `Jerrycurl.Diagnostics.HashCode` with `System.HashCode`
**File:** [HashCode.cs](src/Shared/Jerrycurl.Common/Diagnostics/HashCode.cs) (deleted), plus the 22 files that alias it · **Native API:** `System.HashCode` (.NET Core 2.1) · **Runs:** every cache-key and identity hash (per lookup; per row for join keys) · **Expected gain:** Medium · **Risk:** low; hash values change, but they were already randomized per process

`System.HashCode` uses the same algorithm. The BCL version is marked `AggressiveInlining`, uses intrinsics, and is maintained with the runtime. Three kinds of change:
- **`HashCode.Combine(...)` and `hashCode.Add(x, comparer)`:** these calls compile unchanged against `System.HashCode`. Only the `using HashCode = Jerrycurl.Diagnostics.HashCode;` alias line is removed.
- **`CombineAll`/`AddRange`:** `System.HashCode` has no equivalent, so the five call sites (`Tuple`, `RelationHeader`, `CommandCacheKey`, `QueryCacheKey`, `BufferCacheKey`) get an indexed loop. This also avoids today's boxed `IEnumerable` enumerator.
- **Alias files:** `MetadataKey`, `RelationCacheKey`, `Field`, `Missing`, `Relation`, `Tuple`, `FieldIdentity`, `MetadataIdentity`, `RelationHeader`, `CompositeKey`, `ColumnAttribute`, `AggregateAttribute`, `BufferCacheKey`, `QueryCacheKey`, `CommandCacheKey`, `ColumnName`, `Reference`, `ReferenceKey`, `PageLocatorKey`, `ProcCacheKey`, `ProcLookupKey` and `Model`.

**Before**
```csharp
// Common/Diagnostics/HashCode.cs: 444 lines of xxHash32
internal struct HashCode
{
    private static readonly uint s_seed = GenerateGlobalSeed();
    // ... Combine<T1..T8>, Add, AddRange, CombineAll, ToHashCode ...
}

// Relations.Abstractions/RelationHeader.cs
using HashCode = Jerrycurl.Diagnostics.HashCode;
// ...
public override int GetHashCode() => HashCode.CombineAll(this.Attributes);

// Cqs/Queries/Internal/Caching/QueryCacheKey.cs
using HashCode = Jerrycurl.Diagnostics.HashCode;
// ...
public override int GetHashCode()
{
    HashCode hashCode = new HashCode();

    hashCode.Add(this.Schema);
    hashCode.AddRange(this.Header);

    return hashCode.ToHashCode();
}

// Cqs/Queries/Internal/CompositeKey.cs (Combine call sites compile unchanged)
using HashCode = Jerrycurl.Diagnostics.HashCode;
// ...
public override int GetHashCode() => HashCode.Combine(this.item1, this.item2);
```

**After**
```csharp
// Common/Diagnostics/HashCode.cs: deleted

// Relations.Abstractions/RelationHeader.cs
public override int GetHashCode()
{
    IReadOnlyList<IRelationAttribute> attributes = this.Attributes;
    HashCode hashCode = new HashCode();

    for (int i = 0; i < attributes.Count; i++)
        hashCode.Add(attributes[i]);

    return hashCode.ToHashCode();
}

// Cqs/Queries/Internal/Caching/QueryCacheKey.cs
public override int GetHashCode()
{
    HashCode hashCode = new HashCode();

    hashCode.Add(this.Schema);

    for (int i = 0; i < this.Header.Count; i++)
        hashCode.Add(this.Header[i]);

    return hashCode.ToHashCode();
}

// Cqs/Queries/Internal/CompositeKey.cs: now resolves to System.HashCode
public override int GetHashCode() => HashCode.Combine(this.item1, this.item2);
```

**Diff**
```diff
-// Common/Diagnostics/HashCode.cs (444 lines deleted)
 
 // RelationHeader.cs
-using HashCode = Jerrycurl.Diagnostics.HashCode;
 // ...
-public override int GetHashCode() => HashCode.CombineAll(this.Attributes);
+public override int GetHashCode()
+{
+    IReadOnlyList<IRelationAttribute> attributes = this.Attributes;
+    HashCode hashCode = new HashCode();
+
+    for (int i = 0; i < attributes.Count; i++)
+        hashCode.Add(attributes[i]);
+
+    return hashCode.ToHashCode();
+}
 
 // QueryCacheKey.cs
-using HashCode = Jerrycurl.Diagnostics.HashCode;
 // ...
     hashCode.Add(this.Schema);
-    hashCode.AddRange(this.Header);
+
+    for (int i = 0; i < this.Header.Count; i++)
+        hashCode.Add(this.Header[i]);
 
 // CompositeKey.cs and the other HashCode.Combine sites: only the alias line goes
-using HashCode = Jerrycurl.Diagnostics.HashCode;
```

### 1.2 Replace the `Equality` helper with direct comparisons
**File:** [Equality.cs](src/Shared/Jerrycurl.Common/Diagnostics/Equality.cs) (deleted), plus 20 `Equals` implementations · **Native API:** `EqualityComparer<T>.Default` (.NET Framework 2.0; devirtualized by the JIT since .NET Core 2.1) · **Runs:** every cache-key and identity compare; per row for multi-column join keys (`CompositeKey`) · **Expected gain:** High on join-heavy queries, Medium elsewhere · **Risk:** low; see the caveats

`Equality` has three costs:
- `Equality.Combine(this, other, m => m.A, m => m.B)` makes two delegate calls per field.
- It never stops after the first mismatch.
- `Equality.Add<T>` uses `left?.Equals(right)`, which binds to `object.Equals(object)` and so **boxes** every value-typed field (`int`, `int?`, `Guid`, enums, and the `CompositeKey<T1..>` items on every join-dictionary probe).

The change writes each `Equals` as a direct, short-circuiting `&&` chain. Reference types use `==`/`Equals`, value types and generics use `EqualityComparer<T>.Default.Equals`, and existing custom comparers are kept.

**Affected types:**
- Cqs: `CompositeKey<…>` (4 arities), `ColumnAttribute`, `AggregateAttribute`, `ColumnName`, `CommandCacheKey`, `QueryCacheKey`, `BufferCacheKey`, `Reference`, `ReferenceKey`
- Relations: `MetadataKey`, `RelationCacheKey`, `Field`, `Missing`, `Model`, `Relation`, `Tuple`
- Relations.Abstractions: `FieldIdentity`, `MetadataIdentity`, `RelationHeader`
- Mvc: `PageLocatorKey`, `ProcCacheKey`, `ProcLookupKey`

**Caveats:**
- **Null argument.** The lambda form evaluates `m.A` on `other` without a null check. On the class types, `x.Equals((T)null)` therefore throws `NullReferenceException` today; it will return `false`. `Equals(object)` already returned `false` for null.
- **List-valued keys.** `QueryCacheKey` and `BufferCacheKey` will compare counts. That is the same change as **5.2**; applying 1.2 includes it, and 5.2 can be committed first on its own.
- **Interaction with 2.1.** 2.1 turns `MetadataKey` into a struct. Its `Equals` then drops the `other is not null` guard.
- **Dead overload.** The buggy `Equality.CombineAll(IEnumerable…)` overload (**5.1**) is deleted along with the file.

**Before**
```csharp
// Shape A, delegate selectors (CompositeKey, ColumnAttribute, AggregateAttribute,
// ColumnName, PageLocatorKey, ProcCacheKey, ProcLookupKey, RelationCacheKey,
// Field, Missing, Model, Relation, FieldIdentity, Reference, ReferenceKey)
public bool Equals(CompositeKey<T1, T2> other) => Equality.Combine(this, other, m => m.item1, m => m.item2);

public bool Equals(ColumnName other) => Equality.Combine(this, other, m => m.Metadata, m => m.Column?.Type, m => m.Column?.TypeName, m => m.Column?.Index);

// Shape B, builder struct (MetadataIdentity, MetadataKey, QueryCacheKey, BufferCacheKey)
public bool Equals(MetadataIdentity other)
{
    if (other == null)
        return false;

    Equality eq = new Equality();

    eq.Add(this.Name, other.Name, this.Notation.Comparer);
    eq.Add(this.Schema, other.Schema);

    return eq.ToEquals();
}

public bool Equals(QueryCacheKey<T> other)
{
    Equality eq = new Equality();

    eq.Add(this.Schema, other?.Schema);
    eq.Add(this.Type, other?.Type);
    eq.AddRange(this.Header, other?.Header);

    return eq.ToEquals();
}

// Shape C, list compare (RelationHeader, CommandCacheKey, Tuple)
public bool Equals(IRelationHeader other) => Equality.CombineAll(this.Attributes, other?.Attributes);
```

**After**
```csharp
// Shape A
public bool Equals(CompositeKey<T1, T2> other)
    => EqualityComparer<T1>.Default.Equals(this.item1, other.item1)
    && EqualityComparer<T2>.Default.Equals(this.item2, other.item2);

public bool Equals(ColumnName other)
    => other is not null
    && Equals(this.Metadata, other.Metadata)
    && this.Column?.Type == other.Column?.Type
    && this.Column?.TypeName == other.Column?.TypeName
    && this.Column?.Index == other.Column?.Index;

// Shape B
public bool Equals(MetadataIdentity other)
    => other is not null
    && this.Notation.Comparer.Equals(this.Name, other.Name)
    && Equals(this.Schema, other.Schema);

public bool Equals(QueryCacheKey<T> other)
{
    if (other is null || !Equals(this.Schema, other.Schema) || this.Type != other.Type)
        return false;

    return ListEquals(this.Header, other.Header);   // count check + indexed compare
}

// Shape C
public bool Equals(IRelationHeader other)
    => other is not null && ListEquals(this.Attributes, other.Attributes);

// One small internal helper replaces Equality.cs (Common/Collections/ListEquality.cs)
internal static bool ListEquals<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
{
    if (ReferenceEquals(left, right))
        return true;
    if (left is null || right is null || left.Count != right.Count)
        return false;

    EqualityComparer<T> comparer = EqualityComparer<T>.Default;

    for (int i = 0; i < left.Count; i++)
    {
        if (!comparer.Equals(left[i], right[i]))
            return false;
    }

    return true;
}
```

**Diff**
```diff
-// Common/Diagnostics/Equality.cs (213 lines deleted)
+// Common/Collections/ListEquality.cs: ListEquals<T>(IReadOnlyList<T>, IReadOnlyList<T>), as above
 
 // CompositeKey.cs
-public bool Equals(CompositeKey<T1, T2> other) => Equality.Combine(this, other, m => m.item1, m => m.item2);
+public bool Equals(CompositeKey<T1, T2> other)
+    => EqualityComparer<T1>.Default.Equals(this.item1, other.item1)
+    && EqualityComparer<T2>.Default.Equals(this.item2, other.item2);
 
 // ColumnName.cs
-public bool Equals(ColumnName other) => Equality.Combine(this, other, m => m.Metadata, m => m.Column?.Type, m => m.Column?.TypeName, m => m.Column?.Index);
+public bool Equals(ColumnName other)
+    => other is not null
+    && Equals(this.Metadata, other.Metadata)
+    && this.Column?.Type == other.Column?.Type
+    && this.Column?.TypeName == other.Column?.TypeName
+    && this.Column?.Index == other.Column?.Index;
 
 // MetadataIdentity.cs
 public bool Equals(MetadataIdentity other)
-{
-    if (other == null)
-        return false;
-
-    Equality eq = new Equality();
-
-    eq.Add(this.Name, other.Name, this.Notation.Comparer);
-    eq.Add(this.Schema, other.Schema);
-
-    return eq.ToEquals();
-}
+    => other is not null
+    && this.Notation.Comparer.Equals(this.Name, other.Name)
+    && Equals(this.Schema, other.Schema);
 
 // QueryCacheKey.cs
 public bool Equals(QueryCacheKey<T> other)
 {
-    Equality eq = new Equality();
-
-    eq.Add(this.Schema, other?.Schema);
-    eq.Add(this.Type, other?.Type);
-    eq.AddRange(this.Header, other?.Header);
-
-    return eq.ToEquals();
+    if (other is null || !Equals(this.Schema, other.Schema) || this.Type != other.Type)
+        return false;
+
+    return ListEquals(this.Header, other.Header);
 }
 
 // RelationHeader.cs
-public bool Equals(IRelationHeader other) => Equality.CombineAll(this.Attributes, other?.Attributes);
+public bool Equals(IRelationHeader other)
+    => other is not null && ListEquals(this.Attributes, other.Attributes);
```

---

## 2. Relations

These changes cover metadata lookup, relation caching, and the per-row reader and generated-code path in `src/Mvc/Jerrycurl.Relations*`. The `Equals`/`GetHashCode` bodies of the key and identity types are handled by the §1 helper replacement; this section only changes their shape where a hot path needs it.

### 2.1 `Schema.Lookup<T>`: `MetadataKey` struct, lock-free fast path and a re-check under the write lock
**File:** [Metadata/Schema.cs:67-120](src/Mvc/Jerrycurl.Relations/Metadata/Schema.cs#L67-L120), [Metadata/MetadataKey.cs:8-31](src/Mvc/Jerrycurl.Relations/Metadata/MetadataKey.cs#L8-L31) · **Runs:** per lookup · **Expected gain:** High · **Risk:** recursive *hit* no longer throws (see caveat)

Today every lookup, including a cache hit, does four things: it allocates a `MetadataKey` object, does a `ConcurrentDictionary.GetOrAdd` for the per-type `ReaderWriterLockSlim`, calls `EnterReadLock`/`ExitReadLock`, and pays for a `try/finally`. Lookup is on the per-parameter path (`Parameter.Build`), the per-projection path (`ProjectionHelper.GetJsonMetadata`), and every `MetadataIdentity.Lookup`. The changes:
- `MetadataKey` becomes an internal `readonly struct`, so it lives on the stack. Because it implements `IEquatable<MetadataKey>`, `ConcurrentDictionary` doesn't box it. Its `Equals`/`GetHashCode` bodies come from the §1 rewrite; the only edit needed here is that `other?.` becomes `other.`, because a struct parameter can't be null.
- A hit becomes one lock-free `entries.TryGetValue`.
- The existing read-lock block stays on the miss path, so a recursive *miss* still raises `MetadataBuilderException.NoRecursion`.
- After `EnterWriteLock`, `entries` is checked again before building. Builders call `AddMetadata`, which throws `"Metadata already added."` on duplicates, so this re-check also closes a race in which two threads build the same entry.

**Caveat (behavior change, mention in commit message):** take a builder for `T` that, while holding `T`'s write lock, calls `Lookup<T>` for a name that is *already cached*. Today that call throws `NoRecursion`, because `EnterReadLock` hits `LockRecursionException`. After the change it returns the cached value. A recursive *miss* still throws as before.

**Before**
```csharp
// MetadataKey.cs
internal class MetadataKey : IEquatable<MetadataKey>
{
    // ... properties, ctor, FromIdentity unchanged ...
    public bool Equals(MetadataKey other)
    {
        Equality eq = new Equality();

        eq.Add(this.MetadataType, other?.MetadataType);
        eq.Add(this.Name, other?.Name, this.Comparer);
        // ... (body replaced by §1)

// Schema.cs
public TMetadata Lookup<TMetadata>(string name)
    where TMetadata : IMetadata
{
    MetadataKey key = this.CreateKey<TMetadata>(name);
    ReaderWriterLockSlim slim = this.GetLock<TMetadata>();

    try
    {
        slim.EnterReadLock();

        if (this.entries.TryGetValue(key, out object value))
            return (TMetadata)value;
    }
    // ... catch LockRecursionException → NoRecursion; finally ExitReadLock ...

    slim.EnterWriteLock();

    try
    {
        MetadataIdentity identity = new MetadataIdentity(this, name);
        // ...
```

**After**
```csharp
// MetadataKey.cs
internal readonly struct MetadataKey : IEquatable<MetadataKey>
{
    // ... properties, ctor (still null-checks all three), FromIdentity unchanged ...
    // Equals/GetHashCode: §1 version, reading other.X instead of other?.X

// Schema.cs
public TMetadata Lookup<TMetadata>(string name)
    where TMetadata : IMetadata
{
    MetadataKey key = this.CreateKey<TMetadata>(name);

    if (this.entries.TryGetValue(key, out object cached))
        return (TMetadata)cached;

    ReaderWriterLockSlim slim = this.GetLock<TMetadata>();

    try
    {
        slim.EnterReadLock();

        if (this.entries.TryGetValue(key, out object value))
            return (TMetadata)value;
    }
    // ... catch LockRecursionException → NoRecursion; finally ExitReadLock (unchanged) ...

    slim.EnterWriteLock();

    try
    {
        if (this.entries.TryGetValue(key, out object built))
            return (TMetadata)built;

        MetadataIdentity identity = new MetadataIdentity(this, name);
        // ...
```

**Diff**
```diff
 // MetadataKey.cs
-internal class MetadataKey : IEquatable<MetadataKey>
+internal readonly struct MetadataKey : IEquatable<MetadataKey>
 
 // Schema.cs
     MetadataKey key = this.CreateKey<TMetadata>(name);
+
+    if (this.entries.TryGetValue(key, out object cached))
+        return (TMetadata)cached;
+
     ReaderWriterLockSlim slim = this.GetLock<TMetadata>();
 
     try
     {
         slim.EnterReadLock();
 // ...
     slim.EnterWriteLock();
 
     try
     {
+        if (this.entries.TryGetValue(key, out object built))
+            return (TMetadata)built;
+
         MetadataIdentity identity = new MetadataIdentity(this, name);
```

### 2.2 `SchemaStore.Builders`: cached array, and an `is` loop in `Lookup`
**File:** [Metadata/SchemaStore.cs:11-16](src/Mvc/Jerrycurl.Relations/Metadata/SchemaStore.cs#L11-L16), [Metadata/Schema.cs:101-109](src/Mvc/Jerrycurl.Relations/Metadata/Schema.cs#L101-L109) · **Runs:** per lookup (miss path only) · **Expected gain:** Low · **Risk:** none; the builder list is fixed after construction

On every call, `Builders` allocates a one-element array, a `Concat` iterator and its enumerator. On every miss, `Lookup` adds an `OfType` iterator on top. The private `builders` list is only filled in the constructor and `RelationBuilder` is get-only, so the combined array can be built lazily once. A benign race only builds an identical array. `Lookup` loops over the internal array with an `is` check, which matches `OfType` exactly, nulls included. The public `Builders` keeps its `IEnumerable<IMetadataBuilder>` type. It returns a cached read-only wrapper so callers can't cast it back to the array and mutate it. With 2.1 in place, misses are rare, so the gain is small.

**Before**
```csharp
private readonly List<IMetadataBuilder> builders = [];
// ...
public IEnumerable<IMetadataBuilder> Builders => new IMetadataBuilder[] { this.RelationBuilder }.Concat(this.builders);

// Schema.Lookup
foreach (IMetadataBuilder<TMetadata> metadataBuilder in this.Store.Builders.OfType<IMetadataBuilder<TMetadata>>())
{
    MetadataBuilderContext context = new MetadataBuilderContext(this, relation);
    // ...
}
```

**After**
```csharp
private readonly List<IMetadataBuilder> builders = [];
private IMetadataBuilder[] allBuilders;
private ReadOnlyCollection<IMetadataBuilder> publicBuilders;
// ...
internal IMetadataBuilder[] AllBuilders => this.allBuilders ??= [this.RelationBuilder, .. this.builders];
public IEnumerable<IMetadataBuilder> Builders => this.publicBuilders ??= Array.AsReadOnly(this.AllBuilders);

// Schema.Lookup
IMetadataBuilder[] builders = this.Store.AllBuilders;

for (int i = 0; i < builders.Length; i++)
{
    if (builders[i] is not IMetadataBuilder<TMetadata> metadataBuilder)
        continue;

    MetadataBuilderContext context = new MetadataBuilderContext(this, relation);
    // ...
}
```

**Diff**
```diff
 private readonly List<IMetadataBuilder> builders = [];
+private IMetadataBuilder[] allBuilders;
+private ReadOnlyCollection<IMetadataBuilder> publicBuilders;
 // ...
-public IEnumerable<IMetadataBuilder> Builders => new IMetadataBuilder[] { this.RelationBuilder }.Concat(this.builders);
+internal IMetadataBuilder[] AllBuilders => this.allBuilders ??= [this.RelationBuilder, .. this.builders];
+public IEnumerable<IMetadataBuilder> Builders => this.publicBuilders ??= Array.AsReadOnly(this.AllBuilders);
 
 // Schema.Lookup
-foreach (IMetadataBuilder<TMetadata> metadataBuilder in this.Store.Builders.OfType<IMetadataBuilder<TMetadata>>())
+IMetadataBuilder[] builders = this.Store.AllBuilders;
+
+for (int i = 0; i < builders.Length; i++)
 {
+    if (builders[i] is not IMetadataBuilder<TMetadata> metadataBuilder)
+        continue;
+
     MetadataBuilderContext context = new MetadataBuilderContext(this, relation);
```

### 2.3 `RelationCache.GetWriter`: static factory lambda
**File:** [Internal/Caching/RelationCache.cs:27-39](src/Mvc/Jerrycurl.Relations/Internal/Caching/RelationCache.cs#L27-L39) · **Runs:** per query (each `RelationReader.Read` start) · **Expected gain:** Medium · **Risk:** none; `k.Source`/`k.Header` are the same references the constructor stored

The factory lambda captures `source` and `header`. That means a closure display class is allocated at method entry and a new delegate on every call, even on a cache hit. With the change, the lambda is `static` and reads `k.Source`/`k.Header` from the key, which removes both allocations on a hit. The key's own allocation and equality are covered by §1.

**Before**
```csharp
private static BufferWriter GetWriter(MetadataIdentity source, IRelationHeader header)
{
    RelationCacheKey key = new RelationCacheKey(source, header);

    return cache.GetOrAdd(key, _ =>
    {
        BufferParser parser = new BufferParser();
        BufferTree tree = parser.Parse(source, header);
        RelationCompiler compiler = new RelationCompiler();

        return compiler.Compile(tree);
    });
}
```

**After**
```csharp
private static BufferWriter GetWriter(MetadataIdentity source, IRelationHeader header)
{
    RelationCacheKey key = new RelationCacheKey(source, header);

    return cache.GetOrAdd(key, static k =>
    {
        BufferParser parser = new BufferParser();
        BufferTree tree = parser.Parse(k.Source, k.Header);
        RelationCompiler compiler = new RelationCompiler();

        return compiler.Compile(tree);
    });
}
```

**Diff**
```diff
-    return cache.GetOrAdd(key, _ =>
+    return cache.GetOrAdd(key, static k =>
     {
         BufferParser parser = new BufferParser();
-        BufferTree tree = parser.Parse(source, header);
+        BufferTree tree = parser.Parse(k.Source, k.Header);
```

### 2.4 `RelationReader`: cache `Degree` when reading starts, and use the buffer local in `ReadNext`
**File:** [RelationReader.cs:13-21](src/Mvc/Jerrycurl.Relations/RelationReader.cs#L13-L21), [RelationReader.cs:75-111](src/Mvc/Jerrycurl.Relations/RelationReader.cs#L75-L111) · **Runs:** per cell (indexer) and per row (`ReadNext`) · **Expected gain:** Medium · **Risk:** assumes the header degree doesn't change mid-read (`CreateBuffer` already sizes `Fields` from it once)

Every `reader[i]`, and so every `RelationDataReader` cell access, evaluates `Degree`. That is four indirect calls: `IRelation.Header`, `IRelationHeader.Degree`, the virtual `Attributes`, and `IReadOnlyList.Count`. `ReadNext` also re-runs the `Buffer` property's two guard checks on every queue step. With the change, `Degree` is read once when reading starts. Before the first `Read`, `Degree` still evaluates live, so pre-read behavior (including which exception the indexer throws) is unchanged. `ReadNext` validates once through `this.Buffer`, which still throws `NoDataAvailable` after completion, and then uses a local inside the loop. Neither `hasCompleted` nor `buffer` can change inside that loop.

**Before**
```csharp
public int Degree => this.Relation.Header.Degree;
// ...
private bool ReadFirst()
{
    this.buffer = RelationCache.CreateBuffer(this.Relation);
    this.buffer.Writer.Initializer(this.buffer);
    // ...
}

private bool ReadNext()
{
    Action<RelationBuffer>[] writers = this.Buffer.Writer.Queues;
    IRelationQueue[] queues = this.Buffer.Queues;
    // ...
        else if (this.ReadOrThrow(queues[this.currentIndex]))
        {
            writers[this.currentIndex](this.Buffer);
```

**After**
```csharp
private int degree = -1;
public int Degree => this.degree >= 0 ? this.degree : this.Relation.Header.Degree;
// ...
private bool ReadFirst()
{
    this.buffer = RelationCache.CreateBuffer(this.Relation);
    this.degree = this.Relation.Header.Degree;
    this.buffer.Writer.Initializer(this.buffer);
    // ...
}

private bool ReadNext()
{
    RelationBuffer buffer = this.Buffer;
    Action<RelationBuffer>[] writers = buffer.Writer.Queues;
    IRelationQueue[] queues = buffer.Queues;
    // ...
        else if (this.ReadOrThrow(queues[this.currentIndex]))
        {
            writers[this.currentIndex](buffer);
```

**Diff**
```diff
-public int Degree => this.Relation.Header.Degree;
+private int degree = -1;
+public int Degree => this.degree >= 0 ? this.degree : this.Relation.Header.Degree;
 // ...
     this.buffer = RelationCache.CreateBuffer(this.Relation);
+    this.degree = this.Relation.Header.Degree;
     this.buffer.Writer.Initializer(this.buffer);
 // ...
 private bool ReadNext()
 {
-    Action<RelationBuffer>[] writers = this.Buffer.Writer.Queues;
-    IRelationQueue[] queues = this.Buffer.Queues;
+    RelationBuffer buffer = this.Buffer;
+    Action<RelationBuffer>[] writers = buffer.Writer.Queues;
+    IRelationQueue[] queues = buffer.Queues;
 // ...
-            writers[this.currentIndex](this.Buffer);
+            writers[this.currentIndex](buffer);
```

### 2.5 `RelationDataReader`: read `InnerReader[i]` once per call
**File:** [RelationDataReader.cs:50-59](src/Mvc/Jerrycurl.Relations/RelationDataReader.cs#L50-L59), [RelationDataReader.cs:78-86](src/Mvc/Jerrycurl.Relations/RelationDataReader.cs#L78-L86) · **Runs:** per cell · **Expected gain:** Low · **Risk:** none; the inner indexer is a pure read

The indexer and `IsDBNull` each fetch `InnerReader[i]` twice. Each fetch is a range check, the `Degree` chain and the `Buffer` guard. `GetFieldValue<T>` calls both members, so a typed read costs four fetches. With the change, each member reads the field into a local. `GetFieldValue<T>` keeps calling the virtual `IsDBNull` and `this[i]`, which drops it from four fetches to two.

**Before**
```csharp
public override object this[int i]
{
    get
    {
        if (this.InnerReader[i].Type == FieldType.Missing)
            return DBNull.Value;

        return this.InnerReader[i].Snapshot ?? DBNull.Value;
    }
}
// ...
public override bool IsDBNull(int i)
{
    if (this.InnerReader[i].Type == FieldType.Missing)
        return true;
    else if (this.InnerReader[i].Snapshot == null)
        return true;

    return false;
}
```

**After**
```csharp
public override object this[int i]
{
    get
    {
        IField field = this.InnerReader[i];

        if (field.Type == FieldType.Missing)
            return DBNull.Value;

        return field.Snapshot ?? DBNull.Value;
    }
}
// ...
public override bool IsDBNull(int i)
{
    IField field = this.InnerReader[i];

    return field.Type == FieldType.Missing || field.Snapshot == null;
}
```

**Diff**
```diff
     get
     {
-        if (this.InnerReader[i].Type == FieldType.Missing)
+        IField field = this.InnerReader[i];
+
+        if (field.Type == FieldType.Missing)
             return DBNull.Value;
 
-        return this.InnerReader[i].Snapshot ?? DBNull.Value;
+        return field.Snapshot ?? DBNull.Value;
     }
 // ...
 public override bool IsDBNull(int i)
 {
-    if (this.InnerReader[i].Type == FieldType.Missing)
-        return true;
-    else if (this.InnerReader[i].Snapshot == null)
-        return true;
-
-    return false;
+    IField field = this.InnerReader[i];
+
+    return field.Type == FieldType.Missing || field.Snapshot == null;
 }
```

### 2.6 `Field.Commit`: reuse the `typedValue` local
**File:** [Field.cs:53-55](src/Mvc/Jerrycurl.Relations/Field.cs#L53-L55) · **Runs:** per changed field on commit · **Expected gain:** Low · **Risk:** none; the first cast already throws the same exception

`typedValue` is computed and then ignored, so `Snapshot` is unboxed (or cast) twice. With the change, `Bind` receives the local, which removes one type check or unbox.

**Before**
```csharp
TValue typedValue = (TValue)this.Snapshot;

this.Data.Bind((TValue)this.Snapshot);
```

**After**
```csharp
TValue typedValue = (TValue)this.Snapshot;

this.Data.Bind(typedValue);
```

**Diff**
```diff
 TValue typedValue = (TValue)this.Snapshot;
 
-this.Data.Bind((TValue)this.Snapshot);
+this.Data.Bind(typedValue);
```

### 2.7 `RelationQueue`: cache the head item in a field
**File:** [Internal/Queues/RelationQueue.cs:13-16](src/Mvc/Jerrycurl.Relations/Internal/Queues/RelationQueue.cs#L13-L16), [RelationQueue.cs:30-40](src/Mvc/Jerrycurl.Relations/Internal/Queues/RelationQueue.cs#L30-L40), [RelationQueue.cs:112-124](src/Mvc/Jerrycurl.Relations/Internal/Queues/RelationQueue.cs#L112-L124) · **Runs:** per row (generated code reads `List`/`Index`/`GetFieldName`; `MoveNext` reads `CurrentItem`) · **Expected gain:** Low · **Risk:** the field must track the head; a fallback to `Peek()` keeps the empty-queue exception

`CurrentItem` calls `Queue<T>.Peek()` several times per row. That is cheap but not free: a size check, an array read and a throw-helper path. With the change, the head is kept in a field. It is set on `Enqueue` when the queue goes from empty to one item, cleared on `Clear`, and refreshed with `TryPeek` after `Dequeue`. `CurrentItem` falls back to `Peek()` when the field is null. On an empty queue it therefore still throws `InvalidOperationException`, and a legitimately null head still returns null.

**Before**
```csharp
public RelationQueueItem<TList> CurrentItem => this.innerQueue.Peek();
// ...
public void Enqueue(RelationQueueItem<TList> item)
{
    if (this.IsCached)
    {
        this.IsCached = false;
        this.innerQueue.Clear();
        this.Reset();
    }

    this.innerQueue.Enqueue(item);
}
// ...
    else if (this.HasItems)
    {
        this.innerQueue.Dequeue();
        this.Reset();
    }
```

**After**
```csharp
private RelationQueueItem<TList> currentItem;
public RelationQueueItem<TList> CurrentItem => this.currentItem ?? this.innerQueue.Peek();
// ...
public void Enqueue(RelationQueueItem<TList> item)
{
    if (this.IsCached)
    {
        this.IsCached = false;
        this.innerQueue.Clear();
        this.currentItem = null;
        this.Reset();
    }

    this.innerQueue.Enqueue(item);

    if (this.innerQueue.Count == 1)
        this.currentItem = item;
}
// ...
    else if (this.HasItems)
    {
        this.innerQueue.Dequeue();
        this.innerQueue.TryPeek(out this.currentItem);
        this.Reset();
    }
```

**Diff**
```diff
-public RelationQueueItem<TList> CurrentItem => this.innerQueue.Peek();
+private RelationQueueItem<TList> currentItem;
+public RelationQueueItem<TList> CurrentItem => this.currentItem ?? this.innerQueue.Peek();
 // ...
         this.innerQueue.Clear();
+        this.currentItem = null;
         this.Reset();
     }
 
     this.innerQueue.Enqueue(item);
+
+    if (this.innerQueue.Count == 1)
+        this.currentItem = item;
 }
 // ...
         this.innerQueue.Dequeue();
+        this.innerQueue.TryPeek(out this.currentItem);
         this.Reset();
```

### 2.8 Deep tier: `RelationCompiler` compiles straight to `Action<RelationBuffer>`
**File:** [Internal/Compilation/RelationCompiler.cs:18-58](src/Mvc/Jerrycurl.Relations/Internal/Compilation/RelationCompiler.cs#L18-L58) · **Runs:** per row (each queue writer) and per reader (initializer) · **Expected gain:** Medium · **Risk:** generated-code change; covered by the Relations test suite

Each queue writer is a compiled 7-argument `BufferInternalWriter` wrapped in a C# closure, `buf => writer(buf.Fields, …, notation, binders, metadata)`. Every row step therefore pays two delegate invocations and a 7-argument shuffle. With the change, the lambda takes the `RelationBuffer` directly. The existing static `Arguments.*` parameter expressions become block variables, so none of the expression-building code changes. Four are assigned from the buffer's properties at entry, and `notation`, `binders` and `metadata` are assigned as `Expression.Constant`s. Each row step is then one delegate call and there is no wrapper closure. Compile order (the initializer first, then the queues in order) is unchanged.

**Before**
```csharp
private delegate void BufferInternalWriter(IField[] fields, IRelationQueue[] queues, IField source, IField model, DotNotation notation, Delegate[] binders, IRelationMetadata[] metadata);

public BufferWriter Compile(BufferTree tree)
{
    // ... notation, binders, metadata ...
    BufferInternalWriter initializer = this.Compile(tree.Source, tree.Queues, tree.Header.Degree);
    List<BufferInternalWriter> writers = [.. tree.Queues.Select(this.Compile)];

    return new BufferWriter()
    {
        Initializer = this.Recompile(initializer, notation, binders, metadata),
        Queues = [.. writers.Select(w => this.Recompile(w, notation, binders, metadata))],
    };
}

private Action<RelationBuffer> Recompile(BufferInternalWriter writer, DotNotation notation, Delegate[] binders, IRelationMetadata[] metadata)
    => buf => writer(buf.Fields, buf.Queues, buf.Source, buf.Model, notation, binders, metadata);
// ... Compile(SourceReader, ...) / Compile(QueueReader) wrap Compile(Expression) ...
private BufferInternalWriter Compile(Expression body)
{
    ParameterExpression[] innerArgs = [Arguments.Fields, Arguments.Queues, Arguments.Source, Arguments.Model, Arguments.Notation, Arguments.Binders, Arguments.Metadata];

    return Expression.Lambda<BufferInternalWriter>(body, innerArgs).Compile();
}
```

**After**
```csharp
private static readonly ParameterExpression BufferArgument = Expression.Parameter(typeof(RelationBuffer), "buffer");

public BufferWriter Compile(BufferTree tree)
{
    // ... notation, binders, metadata ...
    Expression initializer = this.GetInitializerExpression(tree.Source, tree.Queues, tree.Header.Degree);

    return new BufferWriter()
    {
        Initializer = this.Compile(initializer, notation, binders, metadata),
        Queues = [.. tree.Queues.Select(q => this.Compile(this.GetReadWriteExpression(q), notation, binders, metadata))],
    };
}

private Action<RelationBuffer> Compile(Expression body, DotNotation notation, Delegate[] binders, IRelationMetadata[] metadata)
{
    ParameterExpression buffer = BufferArgument;

    Expression block = Expression.Block(typeof(void),
        [Arguments.Fields, Arguments.Queues, Arguments.Source, Arguments.Model, Arguments.Notation, Arguments.Binders, Arguments.Metadata],
        Expression.Assign(Arguments.Fields, Expression.Property(buffer, nameof(RelationBuffer.Fields))),
        Expression.Assign(Arguments.Queues, Expression.Property(buffer, nameof(RelationBuffer.Queues))),
        Expression.Assign(Arguments.Source, Expression.Property(buffer, nameof(RelationBuffer.Source))),
        Expression.Assign(Arguments.Model, Expression.Property(buffer, nameof(RelationBuffer.Model))),
        Expression.Assign(Arguments.Notation, Expression.Constant(notation, typeof(DotNotation))),
        Expression.Assign(Arguments.Binders, Expression.Constant(binders, typeof(Delegate[]))),
        Expression.Assign(Arguments.Metadata, Expression.Constant(metadata, typeof(IRelationMetadata[]))),
        body);

    return Expression.Lambda<Action<RelationBuffer>>(block, buffer).Compile();
}
```

**Diff**
```diff
-private delegate void BufferInternalWriter(IField[] fields, IRelationQueue[] queues, IField source, IField model, DotNotation notation, Delegate[] binders, IRelationMetadata[] metadata);
+private static readonly ParameterExpression BufferArgument = Expression.Parameter(typeof(RelationBuffer), "buffer");
 
 public BufferWriter Compile(BufferTree tree)
 {
     // ...
-    BufferInternalWriter initializer = this.Compile(tree.Source, tree.Queues, tree.Header.Degree);
-    List<BufferInternalWriter> writers = [.. tree.Queues.Select(this.Compile)];
+    Expression initializer = this.GetInitializerExpression(tree.Source, tree.Queues, tree.Header.Degree);
 
     return new BufferWriter()
     {
-        Initializer = this.Recompile(initializer, notation, binders, metadata),
-        Queues = [.. writers.Select(w => this.Recompile(w, notation, binders, metadata))],
+        Initializer = this.Compile(initializer, notation, binders, metadata),
+        Queues = [.. tree.Queues.Select(q => this.Compile(this.GetReadWriteExpression(q), notation, binders, metadata))],
     };
 }
 
-private Action<RelationBuffer> Recompile(BufferInternalWriter writer, DotNotation notation, Delegate[] binders, IRelationMetadata[] metadata)
-    => buf => writer(buf.Fields, buf.Queues, buf.Source, buf.Model, notation, binders, metadata);
-// ... Compile(SourceReader, ...) / Compile(QueueReader) removed ...
-private BufferInternalWriter Compile(Expression body)
+private Action<RelationBuffer> Compile(Expression body, DotNotation notation, Delegate[] binders, IRelationMetadata[] metadata)
 {
-    ParameterExpression[] innerArgs = [Arguments.Fields, Arguments.Queues, Arguments.Source, Arguments.Model, Arguments.Notation, Arguments.Binders, Arguments.Metadata];
-
-    return Expression.Lambda<BufferInternalWriter>(body, innerArgs).Compile();
+    ParameterExpression buffer = BufferArgument;
+
+    Expression block = Expression.Block(typeof(void),
+        [Arguments.Fields, Arguments.Queues, Arguments.Source, Arguments.Model, Arguments.Notation, Arguments.Binders, Arguments.Metadata],
+        Expression.Assign(Arguments.Fields, Expression.Property(buffer, nameof(RelationBuffer.Fields))),
+        Expression.Assign(Arguments.Queues, Expression.Property(buffer, nameof(RelationBuffer.Queues))),
+        Expression.Assign(Arguments.Source, Expression.Property(buffer, nameof(RelationBuffer.Source))),
+        Expression.Assign(Arguments.Model, Expression.Property(buffer, nameof(RelationBuffer.Model))),
+        Expression.Assign(Arguments.Notation, Expression.Constant(notation, typeof(DotNotation))),
+        Expression.Assign(Arguments.Binders, Expression.Constant(binders, typeof(Delegate[]))),
+        Expression.Assign(Arguments.Metadata, Expression.Constant(metadata, typeof(IRelationMetadata[]))),
+        body);
+
+    return Expression.Lambda<Action<RelationBuffer>>(block, buffer).Compile();
 }
```

### 2.9 `DotNotation`: span and `IndexOf` paths for `Member`, `Depth`, `Path` and `Combine`
**File:** [Metadata/DotNotation.cs:26-39](src/Mvc/Jerrycurl.Relations.Abstractions/Metadata/DotNotation.cs#L26-L39), [DotNotation.cs:69-110](src/Mvc/Jerrycurl.Relations.Abstractions/Metadata/DotNotation.cs#L69-L110) · **Runs:** mostly per metadata/compile; `Combine(a, b)` runs per row for source-level fields and per `MetadataIdentity.Push` · **Expected gain:** Low · **Risk:** public class, but the methods are non-virtual and keep identical results; the virtual `Dot` is read once into a local

- `Member` and `Depth` split the whole name into a `string[]` just to take the last part or count the parts. They become `LastIndexOf`/`Substring` and `AsSpan().Count(dot) + 1`.
- `Combine(params)` runs `Where` plus `string.Join(string, IEnumerable)`. It becomes a counting loop plus `string.Join(char, string[])`. A null element still throws `NullReferenceException`, and a null array still throws `ArgumentNullException("source")`.
- `Combine(a, b)` drops the interpolation handler, which rents and copies a buffer, for a single exact-size `string.Concat` over spans.
- `Path` uses `Substring` instead of `Remove`, which produces the same string.

**Before**
```csharp
public string Combine(params string[] parts)
    => string.Join(this.Dot.ToString(), parts.Where(p => p.Length > 0));

public string Combine(string part1, string part2)
{
    // ... null / empty checks ...
    return $"{part1}{this.Dot}{part2}";
}
// ...
    return to.Remove(0, from.Length + 1);          // Path
// ...
public string Member(string name)
{
    string[] parts = name?.Split([this.Dot]);

    return parts?.Last();
}

public int Depth(string name)
{
    // ... null → -1, model → 0 ...
    string[] parts = name.Split([this.Dot]);

    return parts.Length;
}
```

**After**
```csharp
public string Combine(params string[] parts)
{
    ArgumentNullException.ThrowIfNull(parts, "source"); // same exception as Enumerable.Where

    char dot = this.Dot;
    int count = 0;

    foreach (string part in parts)
    {
        if (part.Length > 0) // NRE on a null part, as before
            count++;
    }

    if (count == parts.Length)
        return string.Join(dot, parts);

    string[] nonEmpty = new string[count];

    for (int i = 0, j = 0; i < parts.Length; i++)
    {
        if (parts[i].Length > 0)
            nonEmpty[j++] = parts[i];
    }

    return string.Join(dot, nonEmpty);
}

public string Combine(string part1, string part2)
{
    // ... null / empty checks unchanged ...
    char dot = this.Dot;

    return string.Concat(part1.AsSpan(), new ReadOnlySpan<char>(in dot), part2.AsSpan());
}
// ...
    return to.Substring(from.Length + 1);          // Path
// ...
public string Member(string name)
{
    if (name == null)
        return null;

    int index = name.LastIndexOf(this.Dot);

    return index < 0 ? name : name.Substring(index + 1);
}

public int Depth(string name)
{
    // ... null → -1, model → 0 unchanged ...
    return name.AsSpan().Count(this.Dot) + 1;
}
```

**Diff**
```diff
-public string Combine(params string[] parts)
-    => string.Join(this.Dot.ToString(), parts.Where(p => p.Length > 0));
+public string Combine(params string[] parts)
+{
+    ArgumentNullException.ThrowIfNull(parts, "source");
+
+    char dot = this.Dot;
+    int count = 0;
+
+    foreach (string part in parts)
+    {
+        if (part.Length > 0)
+            count++;
+    }
+
+    if (count == parts.Length)
+        return string.Join(dot, parts);
+
+    string[] nonEmpty = new string[count];
+
+    for (int i = 0, j = 0; i < parts.Length; i++)
+    {
+        if (parts[i].Length > 0)
+            nonEmpty[j++] = parts[i];
+    }
+
+    return string.Join(dot, nonEmpty);
+}
 
 public string Combine(string part1, string part2)
 {
     // ...
-    return $"{part1}{this.Dot}{part2}";
+    char dot = this.Dot;
+
+    return string.Concat(part1.AsSpan(), new ReadOnlySpan<char>(in dot), part2.AsSpan());
 }
 // ...
-    return to.Remove(0, from.Length + 1);
+    return to.Substring(from.Length + 1);
 // ...
 public string Member(string name)
 {
-    string[] parts = name?.Split([this.Dot]);
-
-    return parts?.Last();
+    if (name == null)
+        return null;
+
+    int index = name.LastIndexOf(this.Dot);
+
+    return index < 0 ? name : name.Substring(index + 1);
 }
 // ...
-    string[] parts = name.Split([this.Dot]);
-
-    return parts.Length;
+    return name.AsSpan().Count(this.Dot) + 1;
```

### 2.10 `DotNotation.Parent`: `Substring` fast path, with a fallback for empty segments
**File:** [Metadata/DotNotation.cs:83-91](src/Mvc/Jerrycurl.Relations.Abstractions/Metadata/DotNotation.cs#L83-L91) · **Runs:** per `MetadataIdentity.Pop`/`FieldIdentity.Pop` (metadata building, Cqs `NodeParser` dynamic lookup) · **Expected gain:** Low · **Risk:** the fast path only applies when the name has no empty segments

Today `Parent` builds the split array and all its segment strings, then a `Take` array, then `Where` and `Join`. When the name has no empty segment (no leading or trailing dot and no doubled dot), the result is exactly `name.Substring(0, lastDot)`, or `""` when there is no dot. A name with empty segments, such as `"a..b"` → `"a"` or `".a"` → `""`, goes through the existing split, filter and join code unchanged, because the empty-part filtering changes the result there.

**Caveat:** this isn't a behavior change, but the fast path is only valid for names without empty segments. The fallback exists for that reason and must be kept.

**Before**
```csharp
public string Parent(string name)
{
    if (this.Comparer.Equals(name, this.Model()) || name == null)
        return null;

    string[] parts = name.Split([this.Dot]);

    return this.Combine([.. parts.Take(parts.Length - 1)]);
}
```

**After**
```csharp
public string Parent(string name)
{
    if (this.Comparer.Equals(name, this.Model()) || name == null)
        return null;

    char dot = this.Dot;
    int last = name.LastIndexOf(dot);

    if (last < 0)
        return "";

    ReadOnlySpan<char> span = name.AsSpan();
    ReadOnlySpan<char> doubleDot = [dot, dot];

    if (span[0] != dot && span[^1] != dot && span.IndexOf(doubleDot) < 0)
        return name.Substring(0, last);

    string[] parts = name.Split([dot]);

    return this.Combine([.. parts.Take(parts.Length - 1)]);
}
```

**Diff**
```diff
     if (this.Comparer.Equals(name, this.Model()) || name == null)
         return null;
 
-    string[] parts = name.Split([this.Dot]);
+    char dot = this.Dot;
+    int last = name.LastIndexOf(dot);
+
+    if (last < 0)
+        return "";
+
+    ReadOnlySpan<char> span = name.AsSpan();
+    ReadOnlySpan<char> doubleDot = [dot, dot];
+
+    if (span[0] != dot && span[^1] != dot && span.IndexOf(doubleDot) < 0)
+        return name.Substring(0, last);
+
+    string[] parts = name.Split([dot]);
 
     return this.Combine([.. parts.Take(parts.Length - 1)]);
```

### 2.11 `MetadataIdentity.Equals(string)`: compare inline instead of allocating
**File:** [Metadata/MetadataIdentity.cs:46-47](src/Mvc/Jerrycurl.Relations.Abstractions/Metadata/MetadataIdentity.cs#L46-L47) · **Runs:** per call (public API; no hot internal callers today) · **Expected gain:** Low · **Risk:** none; the exceptions and virtual calls are the same

`Equals(string)` allocates a temporary `MetadataIdentity` only to compare it with `this`. The inline version does the same checks in the same order, without the allocation:
- the `ArgumentNullException(nameof(name))` the constructor threw;
- the name comparison through the virtual `Notation.Comparer`, where a null comparer still throws `ArgumentNullException`, as `Equality.Add(…, comparer)` did;
- the `Schema.Equals` call the old path made, which here compares the schema with itself.

**Before**
```csharp
public bool Equals(string name)
    => this.Equals(new MetadataIdentity(this.Schema, name));
```

**After**
```csharp
public bool Equals(string name)
{
    if (name == null)
        throw new ArgumentNullException(nameof(name));

    IEqualityComparer<string> comparer = this.Notation.Comparer ?? throw new ArgumentNullException("comparer");

    return comparer.Equals(this.Name, name) && this.Schema.Equals(this.Schema); // same calls as the old path
}
```

**Diff**
```diff
 public bool Equals(string name)
-    => this.Equals(new MetadataIdentity(this.Schema, name));
+{
+    if (name == null)
+        throw new ArgumentNullException(nameof(name));
+
+    IEqualityComparer<string> comparer = this.Notation.Comparer ?? throw new ArgumentNullException("comparer");
+
+    return comparer.Equals(this.Name, name) && this.Schema.Equals(this.Schema);
+}
```

### 2.12 `DefaultRelationContractResolver`: `static readonly` type arrays and one indexer lookup
**File:** [Metadata/DefaultRelationContractResolver.cs:13-20](src/Mvc/Jerrycurl.Relations/Metadata/DefaultRelationContractResolver.cs#L13-L20), [DefaultRelationContractResolver.cs:47-68](src/Mvc/Jerrycurl.Relations/Metadata/DefaultRelationContractResolver.cs#L47-L68), [DefaultRelationContractResolver.cs:87-126](src/Mvc/Jerrycurl.Relations/Metadata/DefaultRelationContractResolver.cs#L87-L126) · **Runs:** one-time (per metadata node) · **Expected gain:** Low · **Risk:** none; only private members change

For an enumerable, each `GetContract` builds the `allowedTypes` array three times and `convertTypes` twice: in `IsEnumerable`, then in `GetListIndexer` (called once for `ReadIndex` and again for `WriteIndex`). With the change, the arrays are `static readonly` and `GetListIndexer` is called once, with both accessors taken from its result. `GetListIndexer` is pure reflection, so calling it once returns the same `PropertyInfo`.

**Before**
```csharp
return new RelationContract()
{
    ItemType = this.GetGenericItemType(metadata),
    ReadIndex = this.GetListIndexReader(metadata),
    WriteIndex = this.GetListIndexWriter(metadata),
};
// ...
private bool IsEnumerable(IRelationMetadata metadata)
{
    // ...
    Type[] allowedTypes =
    [
        typeof(IList<>), typeof(List<>), typeof(IEnumerable<>),
        typeof(ICollection<>), typeof(IReadOnlyList<>), typeof(IReadOnlyCollection<>),
    ];
    // ...
private PropertyInfo GetListIndexer(IRelationMetadata metadata)
{
    Type[] allowedTypes = [ /* same 6 */ ];
    Type[] convertTypes = [typeof(IEnumerable<>), typeof(ICollection<>), typeof(IReadOnlyCollection<>)];
```

**After**
```csharp
private static readonly Type[] AllowedListTypes =
[
    typeof(IList<>), typeof(List<>), typeof(IEnumerable<>),
    typeof(ICollection<>), typeof(IReadOnlyList<>), typeof(IReadOnlyCollection<>),
];
private static readonly Type[] ConvertListTypes = [typeof(IEnumerable<>), typeof(ICollection<>), typeof(IReadOnlyCollection<>)];

// GetContract
PropertyInfo indexer = this.GetListIndexer(metadata);

return new RelationContract()
{
    ItemType = this.GetGenericItemType(metadata),
    ReadIndex = indexer?.GetMethod,
    WriteIndex = indexer?.SetMethod,
};
// IsEnumerable / GetListIndexer use AllowedListTypes / ConvertListTypes instead of locals
```

**Diff**
```diff
+private static readonly Type[] AllowedListTypes = [ /* same 6 */ ];
+private static readonly Type[] ConvertListTypes = [typeof(IEnumerable<>), typeof(ICollection<>), typeof(IReadOnlyCollection<>)];
 // GetContract
+    PropertyInfo indexer = this.GetListIndexer(metadata);
+
     return new RelationContract()
     {
         ItemType = this.GetGenericItemType(metadata),
-        ReadIndex = this.GetListIndexReader(metadata),
-        WriteIndex = this.GetListIndexWriter(metadata),
+        ReadIndex = indexer?.GetMethod,
+        WriteIndex = indexer?.SetMethod,
     };
 // IsEnumerable
-    Type[] allowedTypes = [ /* 6 */ ];
-    // ...
-    if (!allowedTypes.Contains(openType))
+    if (!AllowedListTypes.Contains(openType))
 // GetListIndexer
-    Type[] allowedTypes = [ /* 6 */ ];
-    Type[] convertTypes = [ /* 3 */ ];
-    // ...
-    if (!allowedTypes.Contains(openType))
+    if (!AllowedListTypes.Contains(openType))
         return null;
-    if (convertTypes.Contains(openType))
+    if (ConvertListTypes.Contains(openType))
```

### 2.13 `RelationMetadataBuilder.GetContract`: reverse `for` loop instead of `Concat().Reverse()`
**File:** [Metadata/RelationMetadataBuilder.cs:58-68](src/Mvc/Jerrycurl.Relations/Metadata/RelationMetadataBuilder.cs#L58-L68) · **Runs:** one-time (per metadata node, two or three times per node) · **Expected gain:** Low · **Risk:** none; same order and same short-circuit

`new[] { DefaultResolver }.Concat(this).Reverse().NotNull(selector).FirstOrDefault()` allocates an array, a `Concat` iterator, a `Reverse` buffer (a full copy) and `Select`/`Where` iterators. It asks the resolvers last-added first, with `DefaultResolver` last, and stops at the first non-null contract. The loop keeps that order and short-circuit, and still calls `GetContract` without a null check, so a null resolver still throws `NullReferenceException` as the selector did. `Count` and `this[i]` on `Collection<T>` are non-virtual.

**Before**
```csharp
IEnumerable<IRelationContractResolver> allResolvers = new[] { this.DefaultResolver }.Concat(this);

IRelationContract contract = allResolvers.Reverse().NotNull(cr => cr.GetContract(metadata)).FirstOrDefault();
```

**After**
```csharp
IRelationContract contract = null;

for (int i = this.Count - 1; i >= 0 && contract == null; i--)
    contract = this[i].GetContract(metadata);

contract ??= this.DefaultResolver.GetContract(metadata);
```

**Diff**
```diff
-IEnumerable<IRelationContractResolver> allResolvers = new[] { this.DefaultResolver }.Concat(this);
-
-IRelationContract contract = allResolvers.Reverse().NotNull(cr => cr.GetContract(metadata)).FirstOrDefault();
+IRelationContract contract = null;
+
+for (int i = this.Count - 1; i >= 0 && contract == null; i--)
+    contract = this[i].GetContract(metadata);
+
+contract ??= this.DefaultResolver.GetContract(metadata);
```

### 2.14 `NodeTree.FindNode`: dictionary lookup
**File:** [Internal/Parsing/NodeTree.cs:10-14](src/Mvc/Jerrycurl.Relations/Internal/Parsing/NodeTree.cs#L10-L14), [Internal/Parsing/NodeParser.cs:40](src/Mvc/Jerrycurl.Relations/Internal/Parsing/NodeParser.cs#L40), [NodeParser.cs:63](src/Mvc/Jerrycurl.Relations/Internal/Parsing/NodeParser.cs#L63) · **Runs:** one-time (per relation compile; cached by `RelationCache`) · **Expected gain:** Low · **Risk:** the map must stay in sync with `Nodes`; there are only two add sites

`FindNode` runs a linear `FirstOrDefault` with a closure for each header attribute and each parent it walks, so parsing is O(n²) in header width. A `Dictionary<MetadataIdentity, Node>` is filled with `TryAdd`, so the first node added wins, as `FirstOrDefault` did. The dictionary uses `MetadataIdentity.Equals`/`GetHashCode` with the stored key as the receiver, the same direction as `n.Identity.Equals(identity)`. A null identity returns null, as before.

**Before**
```csharp
public IList<Node> Nodes { get; } = [];
// ...
public Node FindNode(MetadataIdentity identity) => this.Nodes.FirstOrDefault(n => n.Identity.Equals(identity));

// NodeParser (2 sites)
tree.Nodes.Add(sourceNode);
tree.Nodes.Add(thisNode);
```

**After**
```csharp
private readonly Dictionary<MetadataIdentity, Node> nodeMap = [];

public IList<Node> Nodes { get; } = [];
// ...
public Node FindNode(MetadataIdentity identity)
    => identity != null && this.nodeMap.TryGetValue(identity, out Node node) ? node : null;

public void AddNode(Node node)
{
    this.Nodes.Add(node);
    this.nodeMap.TryAdd(node.Identity, node);
}

// NodeParser (2 sites)
tree.AddNode(sourceNode);
tree.AddNode(thisNode);
```

**Diff**
```diff
+private readonly Dictionary<MetadataIdentity, Node> nodeMap = [];
+
 public IList<Node> Nodes { get; } = [];
 // ...
-public Node FindNode(MetadataIdentity identity) => this.Nodes.FirstOrDefault(n => n.Identity.Equals(identity));
+public Node FindNode(MetadataIdentity identity)
+    => identity != null && this.nodeMap.TryGetValue(identity, out Node node) ? node : null;
+
+public void AddNode(Node node)
+{
+    this.Nodes.Add(node);
+    this.nodeMap.TryAdd(node.Identity, node);
+}
 
 // NodeParser
-tree.Nodes.Add(sourceNode);
+tree.AddNode(sourceNode);
-tree.Nodes.Add(thisNode);
+tree.AddNode(thisNode);
```

### 2.15 `RelationCompiler.IsRunningNetFramework`: a `static readonly bool` with an ordinal comparison
**File:** [Internal/Compilation/RelationCompiler.cs:447](src/Mvc/Jerrycurl.Relations/Internal/Compilation/RelationCompiler.cs#L447), [RelationCompiler.cs:454](src/Mvc/Jerrycurl.Relations/Internal/Compilation/RelationCompiler.cs#L454) · **Runs:** one-time (per empty block during compile) · **Expected gain:** Low · **Risk:** none; the prefix is ASCII

The method re-reads `RuntimeInformation.FrameworkDescription` and runs a culture-sensitive `StartsWith` on every call. The value can't change while the process runs, so it's computed once with an ordinal comparison.

**Before**
```csharp
else if (expressions.Count == 0 && this.IsRunningNetFramework())
// ...
private bool IsRunningNetFramework() => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");
```

**After**
```csharp
else if (expressions.Count == 0 && IsRunningNetFramework)
// ...
private static readonly bool IsRunningNetFramework = RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.Ordinal);
```

**Diff**
```diff
-else if (expressions.Count == 0 && this.IsRunningNetFramework())
+else if (expressions.Count == 0 && IsRunningNetFramework)
 // ...
-private bool IsRunningNetFramework() => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");
+private static readonly bool IsRunningNetFramework = RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.Ordinal);
```

### 2.16 `.ToLower()` → `.ToLowerInvariant()` (`RelationCompiler` variable names, `SqliteContractResolver`)
**File:** [Internal/Compilation/RelationCompiler.cs:439](src/Mvc/Jerrycurl.Relations/Internal/Compilation/RelationCompiler.cs#L439), [Vendors/Jerrycurl.Vendors.Sqlite/Metadata/SqliteContractResolver.cs:30](src/Vendors/Jerrycurl.Vendors.Sqlite/Metadata/SqliteContractResolver.cs#L30) · **Runs:** one-time (per compiled variable) / per query compile (per SQLite column) · **Expected gain:** Low · **Risk:** the SQLite one changes behavior under the Turkish/Azeri cultures (see caveat)

`ToLower()` goes through the current culture's `TextInfo`. `ToLowerInvariant()` skips the culture lookup and is deterministic. In `RelationCompiler` the result is only an expression variable's debug name, so nothing observable changes.

**Caveat:** in `SqliteContractResolver`, under `tr-TR`/`az` cultures, `"INTEGER".ToLower()` gives `"ınteger"` (dotless ı). That matches none of the `case` labels, so today the code silently falls back to the default reader method. With `ToLowerInvariant()` the type is matched correctly. Call this out in the commit message.

**Before**
```csharp
string varName = "_" + metadata.Identity.Name.ToLower().Replace('.', '_');
// SqliteContractResolver
switch (columnInfo.Column.TypeName?.ToLower())
```

**After**
```csharp
string varName = "_" + metadata.Identity.Name.ToLowerInvariant().Replace('.', '_');
// SqliteContractResolver
switch (columnInfo.Column.TypeName?.ToLowerInvariant())
```

**Diff**
```diff
-string varName = "_" + metadata.Identity.Name.ToLower().Replace('.', '_');
+string varName = "_" + metadata.Identity.Name.ToLowerInvariant().Replace('.', '_');
 // SqliteContractResolver
-switch (columnInfo.Column.TypeName?.ToLower())
+switch (columnInfo.Column.TypeName?.ToLowerInvariant())
```

---

## 3. Cqs

Command buffering, parameter binding, query caching and the generated per-row reader/writer code in `src/Mvc/Jerrycurl.Cqs` (key `Equals`/`GetHashCode` rewrites are covered by the consolidated section-1 entry).

### 3.1 Single-lookup `GetOrAdd` in `CommandBuffer.Add`
**File:** [Commands/CommandBuffer.cs:175-248](src/Mvc/Jerrycurl.Cqs/Commands/CommandBuffer.cs#L175-L248) · **Runs:** per parameter · **Expected gain:** High · **Risk:** none; values are never null

Every `Add` overload calls the `IDictionary` extension `GetOrAdd(key)`. That extension evaluates `new TValue()` eagerly, wraps it in a `() => value` closure and calls through the interface, so each call allocates a `FieldBuffer`, a closure and a delegate even when the key already exists. `Add(IParameter)` then calls `TryAdd` on a key that is always present by then. A private helper built on `CollectionsMarshal.GetValueRefOrAddDefault` with `??=` does one hash lookup and allocates only on a miss. Null keys still throw `ArgumentNullException`.

**Before**
```csharp
public void Add(IParameter parameter)
{
    // ...
    FieldBuffer buffer = this.paramHeader.GetOrAdd(parameter.Name);

    buffer.Parameter ??= new ParameterSource();
    buffer.Parameter.Parameter = parameter;
    buffer.Parameter.Name = parameter.Name;

    this.paramHeader.TryAdd(parameter.Name, buffer);
}
// ...
    FieldBuffer buffer = this.fieldBuffers.GetOrAdd(binding.Target);          // Add(CascadeBinding), Add(ColumnBinding)
// ...
    FieldBuffer buffer = this.fieldBuffers.GetOrAdd(binding.Target);          // Add(ParameterBinding)
    FieldBuffer paramBuffer = this.paramHeader.GetOrAdd(binding.ParameterName);
```

**After**
```csharp
using System.Runtime.InteropServices;
// ...
private static FieldBuffer GetOrAddBuffer<TKey>(Dictionary<TKey, FieldBuffer> map, TKey key)
{
    ref FieldBuffer slot = ref CollectionsMarshal.GetValueRefOrAddDefault(map, key, out _);

    return slot ??= new FieldBuffer();
}

public void Add(IParameter parameter)
{
    // ...
    FieldBuffer buffer = GetOrAddBuffer(this.paramHeader, parameter.Name);

    buffer.Parameter ??= new ParameterSource();
    buffer.Parameter.Parameter = parameter;
    buffer.Parameter.Name = parameter.Name;
}
// ...
    FieldBuffer buffer = GetOrAddBuffer(this.fieldBuffers, binding.Target);   // Add(CascadeBinding), Add(ColumnBinding)
// ...
    FieldBuffer buffer = GetOrAddBuffer(this.fieldBuffers, binding.Target);   // Add(ParameterBinding)
    FieldBuffer paramBuffer = GetOrAddBuffer(this.paramHeader, binding.ParameterName);
```

**Diff**
```diff
+using System.Runtime.InteropServices;
 // ...
+    private static FieldBuffer GetOrAddBuffer<TKey>(Dictionary<TKey, FieldBuffer> map, TKey key)
+    {
+        ref FieldBuffer slot = ref CollectionsMarshal.GetValueRefOrAddDefault(map, key, out _);
+
+        return slot ??= new FieldBuffer();
+    }
+
     public void Add(IParameter parameter)
     {
         // ...
-        FieldBuffer buffer = this.paramHeader.GetOrAdd(parameter.Name);
+        FieldBuffer buffer = GetOrAddBuffer(this.paramHeader, parameter.Name);
 
         buffer.Parameter ??= new ParameterSource();
         buffer.Parameter.Parameter = parameter;
         buffer.Parameter.Name = parameter.Name;
-
-        this.paramHeader.TryAdd(parameter.Name, buffer);
     }
 // ...
-        FieldBuffer buffer = this.fieldBuffers.GetOrAdd(binding.Target);
+        FieldBuffer buffer = GetOrAddBuffer(this.fieldBuffers, binding.Target);
 // ...
-        FieldBuffer buffer = this.fieldBuffers.GetOrAdd(binding.Target);
-        FieldBuffer paramBuffer = this.paramHeader.GetOrAdd(binding.ParameterName);
+        FieldBuffer buffer = GetOrAddBuffer(this.fieldBuffers, binding.Target);
+        FieldBuffer paramBuffer = GetOrAddBuffer(this.paramHeader, binding.ParameterName);
```
`columnHeader.TryAdd` in `Add(ColumnBinding)` targets a different dictionary and stays.

### 3.2 Direct source priority in `FieldBuffer` and `CascadeSource`
**File:** [Commands/Internal/FieldBuffer.cs:19-37](src/Mvc/Jerrycurl.Cqs/Commands/Internal/FieldBuffer.cs#L19-L37), [Commands/Internal/CascadeSource.cs:23-37](src/Mvc/Jerrycurl.Cqs/Commands/Internal/CascadeSource.cs#L23-L37), [Commands/CommandBuffer.cs:149](src/Mvc/Jerrycurl.Cqs/Commands/CommandBuffer.cs#L149) · **Runs:** per parameter · **Expected gain:** High · **Risk:** none; same order and same short-circuiting

`Read` runs once per parameter in `Prepare` (`TryReadValue`) and once per bound field in `Commit` (`Bind`). Each call builds a 3-element `IFieldSource[]`, wraps it in a `Where(it => it != null)` iterator and runs `FirstOrDefault(predicate)` over it. `CascadeSource` does the same through `CommandBuffer.GetSources(...).FirstOrDefault()` at every hop of the cascade chain. Checking `Column`, then `Parameter`, then `Cascade` directly keeps the same priority and the same order of `HasChanged` calls, with no allocations. `GetSources`/`GetChanges` stay for any other callers.

**Before**
```csharp
public bool HasChanges() => this.GetChanges().Any();
public IEnumerable<IFieldSource> GetSources() => new IFieldSource[] { this.Column, this.Parameter, this.Cascade }.NotNull();
public IEnumerable<IFieldSource> GetChanges() => this.GetSources().Where(s => s.HasChanged);

public bool Read(out object value)
{
    IFieldSource changedSource = this.GetSources().FirstOrDefault(f => f.HasChanged);
    // ...
}

// CascadeSource.GetNonCascadeSource
IFieldSource source = this.Buffer.GetSources(this.Binding.Source).FirstOrDefault();
// ...
    nextSource = this.Buffer.GetSources(cascade.Binding.Source).FirstOrDefault();
```

**After**
```csharp
public bool HasChanges() => IsChanged(this.Column) || IsChanged(this.Parameter) || IsChanged(this.Cascade);
public IEnumerable<IFieldSource> GetSources() => new IFieldSource[] { this.Column, this.Parameter, this.Cascade }.NotNull();
public IEnumerable<IFieldSource> GetChanges() => this.GetSources().Where(s => s.HasChanged);
public IFieldSource GetFirstSource() => (IFieldSource)this.Column ?? (IFieldSource)this.Parameter ?? this.Cascade;

public bool Read(out object value)
{
    IFieldSource changedSource = IsChanged(this.Column) ? this.Column
                               : IsChanged(this.Parameter) ? this.Parameter
                               : IsChanged(this.Cascade) ? this.Cascade
                               : null;
    // ...
}

private static bool IsChanged(IFieldSource source) => source != null && source.HasChanged;

// CommandBuffer
internal IFieldSource GetFirstSource(IField target) => this.GetBuffer(target)?.GetFirstSource();

// CascadeSource.GetNonCascadeSource
IFieldSource source = this.Buffer.GetFirstSource(this.Binding.Source);
// ...
    nextSource = this.Buffer.GetFirstSource(cascade.Binding.Source);
```

**Diff**
```diff
-    public bool HasChanges() => this.GetChanges().Any();
+    public bool HasChanges() => IsChanged(this.Column) || IsChanged(this.Parameter) || IsChanged(this.Cascade);
     public IEnumerable<IFieldSource> GetSources() => new IFieldSource[] { this.Column, this.Parameter, this.Cascade }.NotNull();
     public IEnumerable<IFieldSource> GetChanges() => this.GetSources().Where(s => s.HasChanged);
+    public IFieldSource GetFirstSource() => (IFieldSource)this.Column ?? (IFieldSource)this.Parameter ?? this.Cascade;
 
     public bool Read(out object value)
     {
-        IFieldSource changedSource = this.GetSources().FirstOrDefault(f => f.HasChanged);
+        IFieldSource changedSource = IsChanged(this.Column) ? this.Column
+                                   : IsChanged(this.Parameter) ? this.Parameter
+                                   : IsChanged(this.Cascade) ? this.Cascade
+                                   : null;
         // ...
     }
+
+    private static bool IsChanged(IFieldSource source) => source != null && source.HasChanged;
 
 // CommandBuffer
+    internal IFieldSource GetFirstSource(IField target) => this.GetBuffer(target)?.GetFirstSource();
 
 // CascadeSource.GetNonCascadeSource
-        IFieldSource source = this.Buffer.GetSources(this.Binding.Source).FirstOrDefault();
+        IFieldSource source = this.Buffer.GetFirstSource(this.Binding.Source);
 // ...
-            nextSource = this.Buffer.GetSources(cascade.Binding.Source).FirstOrDefault();
+            nextSource = this.Buffer.GetFirstSource(cascade.Binding.Source);
```

### 3.3 Static converter factory in `CommandCache.GetConverter`
**File:** [Commands/Internal/Caching/CommandCache.cs:26-36](src/Mvc/Jerrycurl.Cqs/Commands/Internal/Caching/CommandCache.cs#L26-L36) · **Runs:** per parameter (each bound field on `Commit`) · **Expected gain:** Medium · **Risk:** none

`FieldBuffer.Bind` calls `GetConverter` for every committed field. Its factory lambda captures `metadata` and `columnMetadata`, so every call allocates a closure and a delegate even on a cache hit. The `GetOrAdd<TArg>` overload with a `static` lambda and a value-tuple `factoryArgument` removes both, and the compiler receives the same objects as before. The key and list allocations remain, so the gain is Medium. `GetWriter` already captures nothing (it reads only `k.Columns`) and needs no change.

**Before**
```csharp
return converterMap.GetOrAdd(cacheKey, k =>
{
    CommandCompiler compiler = new CommandCompiler();

    return compiler.Compile(metadata, columnMetadata);
});
```

**After**
```csharp
return converterMap.GetOrAdd(cacheKey, static (_, args) =>
{
    CommandCompiler compiler = new CommandCompiler();

    return compiler.Compile(args.metadata, args.columnMetadata);
}, (metadata, columnMetadata));
```

**Diff**
```diff
-        return converterMap.GetOrAdd(cacheKey, k =>
+        return converterMap.GetOrAdd(cacheKey, static (_, args) =>
         {
             CommandCompiler compiler = new CommandCompiler();
 
-            return compiler.Compile(metadata, columnMetadata);
-        });
+            return compiler.Compile(args.metadata, args.columnMetadata);
+        }, (metadata, columnMetadata));
```

### 3.4 Non-capturing factories in `QueryCache`
**File:** [Queries/Internal/Caching/QueryCache.cs:38-67](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Caching/QueryCache.cs#L38-L67) · **Runs:** per query (per result set) · **Expected gain:** Medium · **Risk:** none; `k` is the key instance just passed in

The `GetListFactory` factory captures `queryType`, and the `GetEnumerateFactory<T>` factory captures `schema`. A closure is therefore allocated on every call, hit or miss. Both values are already on the key (`k.Type`, `k.Schema`), and `ConcurrentDictionary` hands the factory the very key passed in, so reading them from `k` gives identical inputs and lets the lambdas be `static`. `GetAggregateFactory` and `GetBuffer` already capture nothing. `GetCacheKey` already presizes its list with `fieldCount`, so that part of the plan is already done.

**Before**
```csharp
return listMap.GetOrAdd(cacheKey, k =>
{
    BufferCache buffer = GetBuffer(k.Schema);
    ListParser parser = new ListParser(buffer, queryType);
    // ...
});
// ...
return (EnumerateFactory<T>)enumerateMap.GetOrAdd(cacheKey, k =>
{
    EnumerateParser parser = new EnumerateParser(schema);
    // ...
});
```

**After**
```csharp
return listMap.GetOrAdd(cacheKey, static k =>
{
    BufferCache buffer = GetBuffer(k.Schema);
    ListParser parser = new ListParser(buffer, k.Type);
    // ...
});
// ...
return (EnumerateFactory<T>)enumerateMap.GetOrAdd(cacheKey, static k =>
{
    EnumerateParser parser = new EnumerateParser(k.Schema);
    // ...
});
```

**Diff**
```diff
-        return listMap.GetOrAdd(cacheKey, k =>
+        return listMap.GetOrAdd(cacheKey, static k =>
         {
             BufferCache buffer = GetBuffer(k.Schema);
-            ListParser parser = new ListParser(buffer, queryType);
+            ListParser parser = new ListParser(buffer, k.Type);
 // ...
-        return (EnumerateFactory<T>)enumerateMap.GetOrAdd(cacheKey, k =>
+        return (EnumerateFactory<T>)enumerateMap.GetOrAdd(cacheKey, static k =>
         {
-            EnumerateParser parser = new EnumerateParser(schema);
+            EnumerateParser parser = new EnumerateParser(k.Schema);
```

### 3.5 Hoist `GetDbType` out of the parameter `Write` contract
**File:** [Metadata/DefaultBindingContractResolver.cs:23-41](src/Mvc/Jerrycurl.Cqs/Metadata/DefaultBindingContractResolver.cs#L23-L41) · **Runs:** per parameter · **Expected gain:** Low · **Risk:** `metadata.Type` is read once, when the contract is built

`Parameter.Build` calls `contract.Write` for every ADO parameter. The default `Write` re-runs `GetDbType(metadata)` each time: a `Nullable.GetUnderlyingType` call and a chain of up to 21 `Type` comparisons. `GetDbType` is private and depends only on `metadata.Type`, which on `BindingMetadata` is `Relation.Type` and immutable. Computing it once when the contract is built is equivalent, and the closure now captures a `DbType?` instead of `this` and `metadata`. **Caveat:** a custom `IBindingMetadata` passed to this public resolver whose `Type` changes after `GetParameterContract` would now see the old value. That cannot happen with the built-in metadata.

**Before**
```csharp
return new BindingParameterContract()
{
    Convert = o => o ?? DBNull.Value,
    Write = info =>
    {
        DbType? dbType = this.GetDbType(metadata);

        if (dbType != null)
            info.Parameter.DbType = dbType.Value;
    }
};
```

**After**
```csharp
DbType? dbType = this.GetDbType(metadata);

return new BindingParameterContract()
{
    Convert = o => o ?? DBNull.Value,
    Write = info =>
    {
        if (dbType != null)
            info.Parameter.DbType = dbType.Value;
    }
};
```

**Diff**
```diff
+        DbType? dbType = this.GetDbType(metadata);
+
         return new BindingParameterContract()
         {
             Convert = o => o ?? DBNull.Value,
             Write = info =>
             {
-                DbType? dbType = this.GetDbType(metadata);
-
                 if (dbType != null)
                     info.Parameter.DbType = dbType.Value;
             }
         };
```

### 3.6 Static type arrays and `MethodInfo`s in `DefaultBindingContractResolver`
**File:** [Metadata/DefaultBindingContractResolver.cs:16-18](src/Mvc/Jerrycurl.Cqs/Metadata/DefaultBindingContractResolver.cs#L16-L18), [:106-124](src/Mvc/Jerrycurl.Cqs/Metadata/DefaultBindingContractResolver.cs#L106-L124), [:240](src/Mvc/Jerrycurl.Cqs/Metadata/DefaultBindingContractResolver.cs#L240), [:284](src/Mvc/Jerrycurl.Cqs/Metadata/DefaultBindingContractResolver.cs#L284), [:382-394](src/Mvc/Jerrycurl.Cqs/Metadata/DefaultBindingContractResolver.cs#L382-L394) · **Runs:** one-time (per metadata or compile) · **Expected gain:** Low · **Risk:** none; private members only

Each resolver instance allocates three `Type[]` fields, and `GetListElementType` allocates a fresh six-element `openLists` array on every call (twice per `GetCompositionContract`). The XDocument, JsonNode and DateOnly/TimeOnly reader proxies call `GetMethod` each time they build an expression. These become `private static readonly` fields. No public or protected member changes, and nothing virtual is involved. `GetRecordMethod(name)` stays as it is, since it serves 12 different names.

**Before**
```csharp
private readonly Type[] intTypes = [typeof(int), /* ... */ typeof(ulong)];
private readonly Type[] decTypes = [typeof(float), typeof(double), typeof(decimal)];
private readonly Type[] dateTimeTypes = [typeof(DateTime), typeof(DateOnly), typeof(TimeSpan), typeof(TimeOnly)];
// ...
private Type GetListElementType(IBindingMetadata metadata)
{
    Type[] openLists =
    [
        typeof(List<>), typeof(IList<>), typeof(IReadOnlyList<>),
        typeof(IReadOnlyCollection<>), typeof(ICollection<>), typeof(IEnumerable<>),
    ];
    // ...
    if (openLists.Contains(openType))
// ...
MethodInfo parseInfo = typeof(XDocument).GetMethod("Parse", [typeof(string)]);
// ...
MethodInfo parseInfo = typeof(DefaultBindingContractResolver).GetMethod(nameof(ParseJsonNode), BindingFlags.NonPublic | BindingFlags.Static);
// ...
return Expression.Call(typeof(DateOnly).GetMethod(nameof(DateOnly.FromDateTime), [typeof(DateTime)]), value);
// ... (likewise ToDateTime, FromTimeSpan, ToTimeSpan)
private bool IsIntegerType(Type type) => this.intTypes.Contains(type);
```

**After**
```csharp
private static readonly Type[] intTypes = [typeof(int), /* ... */ typeof(ulong)];
private static readonly Type[] decTypes = [typeof(float), typeof(double), typeof(decimal)];
private static readonly Type[] dateTimeTypes = [typeof(DateTime), typeof(DateOnly), typeof(TimeSpan), typeof(TimeOnly)];
private static readonly Type[] openListTypes =
[
    typeof(List<>), typeof(IList<>), typeof(IReadOnlyList<>),
    typeof(IReadOnlyCollection<>), typeof(ICollection<>), typeof(IEnumerable<>),
];
private static readonly MethodInfo xDocumentParseMethod = typeof(XDocument).GetMethod("Parse", [typeof(string)]);
private static readonly MethodInfo parseJsonNodeMethod = typeof(DefaultBindingContractResolver).GetMethod(nameof(ParseJsonNode), BindingFlags.NonPublic | BindingFlags.Static);
private static readonly MethodInfo dateOnlyFromDateTimeMethod = typeof(DateOnly).GetMethod(nameof(DateOnly.FromDateTime), [typeof(DateTime)]);
private static readonly MethodInfo dateOnlyToDateTimeMethod = typeof(DateOnly).GetMethod(nameof(DateOnly.ToDateTime), [typeof(TimeOnly)]);
private static readonly MethodInfo timeOnlyFromTimeSpanMethod = typeof(TimeOnly).GetMethod(nameof(TimeOnly.FromTimeSpan), [typeof(TimeSpan)]);
private static readonly MethodInfo timeOnlyToTimeSpanMethod = typeof(TimeOnly).GetMethod(nameof(TimeOnly.ToTimeSpan), Type.EmptyTypes);
// ...
    if (openListTypes.Contains(openType))
// ...
MethodInfo parseInfo = xDocumentParseMethod;
// ...
MethodInfo parseInfo = parseJsonNodeMethod;
// ...
return Expression.Call(dateOnlyFromDateTimeMethod, value);
// ...
private bool IsIntegerType(Type type) => intTypes.Contains(type);
```

**Diff**
```diff
-    private readonly Type[] intTypes = [typeof(int), /* ... */ typeof(ulong)];
-    private readonly Type[] decTypes = [typeof(float), typeof(double), typeof(decimal)];
-    private readonly Type[] dateTimeTypes = [typeof(DateTime), typeof(DateOnly), typeof(TimeSpan), typeof(TimeOnly)];
+    private static readonly Type[] intTypes = [typeof(int), /* ... */ typeof(ulong)];
+    private static readonly Type[] decTypes = [typeof(float), typeof(double), typeof(decimal)];
+    private static readonly Type[] dateTimeTypes = [typeof(DateTime), typeof(DateOnly), typeof(TimeSpan), typeof(TimeOnly)];
+    private static readonly Type[] openListTypes = [typeof(List<>), typeof(IList<>), typeof(IReadOnlyList<>), typeof(IReadOnlyCollection<>), typeof(ICollection<>), typeof(IEnumerable<>)];
+    private static readonly MethodInfo xDocumentParseMethod = typeof(XDocument).GetMethod("Parse", [typeof(string)]);
+    private static readonly MethodInfo parseJsonNodeMethod = typeof(DefaultBindingContractResolver).GetMethod(nameof(ParseJsonNode), BindingFlags.NonPublic | BindingFlags.Static);
+    private static readonly MethodInfo dateOnlyFromDateTimeMethod = typeof(DateOnly).GetMethod(nameof(DateOnly.FromDateTime), [typeof(DateTime)]);
+    private static readonly MethodInfo dateOnlyToDateTimeMethod = typeof(DateOnly).GetMethod(nameof(DateOnly.ToDateTime), [typeof(TimeOnly)]);
+    private static readonly MethodInfo timeOnlyFromTimeSpanMethod = typeof(TimeOnly).GetMethod(nameof(TimeOnly.FromTimeSpan), [typeof(TimeSpan)]);
+    private static readonly MethodInfo timeOnlyToTimeSpanMethod = typeof(TimeOnly).GetMethod(nameof(TimeOnly.ToTimeSpan), Type.EmptyTypes);
 // ...
-        Type[] openLists = [ /* six open types */ ];
-
         Type openType = this.GetOpenType(metadata);
 
-        if (openLists.Contains(openType))
+        if (openListTypes.Contains(openType))
 // ...
-        MethodInfo parseInfo = typeof(XDocument).GetMethod("Parse", [typeof(string)]);
+        MethodInfo parseInfo = xDocumentParseMethod;
 // ...
-        MethodInfo parseInfo = typeof(DefaultBindingContractResolver).GetMethod(nameof(ParseJsonNode), BindingFlags.NonPublic | BindingFlags.Static);
+        MethodInfo parseInfo = parseJsonNodeMethod;
 // ...
-            return Expression.Call(typeof(DateOnly).GetMethod(nameof(DateOnly.FromDateTime), [typeof(DateTime)]), value);
+            return Expression.Call(dateOnlyFromDateTimeMethod, value);
 // ... (same for the other three conversions)
-    private bool IsIntegerType(Type type) => this.intTypes.Contains(type);
-    private bool IsFloatType(Type type) => this.decTypes.Contains(type);
+    private bool IsIntegerType(Type type) => intTypes.Contains(type);
+    private bool IsFloatType(Type type) => decTypes.Contains(type);
 // ... (this.intTypes/decTypes/dateTimeTypes -> static in GetNumberTypes, GetObjectReaderProxy, IsDateTimeType)
```

### 3.7 `HashSet` dedupe instead of `GroupBy` in `Query.Build`
**File:** [Queries/Query.cs:13-28](src/Mvc/Jerrycurl.Cqs/Queries/Query.cs#L13-L28) · **Runs:** per parameter · **Expected gain:** High · **Risk:** enumeration is now interleaved with `Build` (see caveat)

`GroupBy(p => p.Name).Select(g => g.First())` builds a `Lookup`, one `Grouping` object plus an element array per distinct name, and two iterators, only to keep the first parameter per name. `GroupBy` without a comparer uses `EqualityComparer<string>.Default` (ordinal, null-safe), and groups come back in first-seen order. A `HashSet<string>` built with that same default comparer gives the same set and order. **Caveat:** `GroupBy` reads every `Name` before the first `IParameter.Build` runs. The loop interleaves the two, so an `IParameter.Build` that mutates `Query.Parameters` would now hit an enumeration error. Built-in parameters never do that.

**Before**
```csharp
if (this.Parameters != null)
{
    foreach (IParameter parameter in this.Parameters.GroupBy(p => p.Name).Select(g => g.First()))
    {
        IDbDataParameter adoParameter = adoCommand.CreateParameter();
        // ...
    }
}
```

**After**
```csharp
if (this.Parameters != null)
{
    HashSet<string> names = new HashSet<string>(); // EqualityComparer<string>.Default, same as GroupBy

    foreach (IParameter parameter in this.Parameters)
    {
        if (!names.Add(parameter.Name))
            continue;

        IDbDataParameter adoParameter = adoCommand.CreateParameter();
        // ...
    }
}
```

**Diff**
```diff
         if (this.Parameters != null)
         {
-            foreach (IParameter parameter in this.Parameters.GroupBy(p => p.Name).Select(g => g.First()))
+            HashSet<string> names = new HashSet<string>();
+
+            foreach (IParameter parameter in this.Parameters)
             {
+                if (!names.Add(parameter.Name))
+                    continue;
+
                 IDbDataParameter adoParameter = adoCommand.CreateParameter();
```

### 3.8 Tidy the column loop in `CommandBuffer.GetUpdateAction`
**File:** [Commands/CommandBuffer.cs:52-79](src/Mvc/Jerrycurl.Cqs/Commands/CommandBuffer.cs#L52-L79) · **Runs:** per query (per result set) · **Expected gain:** Low · **Risk:** none

The loop condition calls `GetFieldCount` (a `try`/`catch` around `FieldCount`) on every iteration. Each matched column calls `GetName(i)` twice, once directly and once inside `GetColumnInfo`. Both lists also start empty and grow. The fix reads `FieldCount` once, reuses the name it already has, and presizes the lists.

**Before**
```csharp
List<ColumnName> names = [];
List<FieldBuffer> bufferList = [];

for (int i = 0; i < this.GetFieldCount(dataReader); i++)
{
    string columnName = dataReader.GetName(i);

    if (this.columnHeader.TryGetValue(columnName, out FieldBuffer buffer))
    {
        MetadataIdentity metadata = buffer.Target.Identity.Metadata;
        ColumnMetadata columnInfo = GetColumnInfo(i);
        // ...
    }
}
// ...
ColumnMetadata GetColumnInfo(int i) => new ColumnMetadata(dataReader.GetName(i), dataReader.GetFieldType(i), dataReader.GetDataTypeName(i), i);
```

**After**
```csharp
int fieldCount = this.GetFieldCount(dataReader);
List<ColumnName> names = new List<ColumnName>(fieldCount);
List<FieldBuffer> bufferList = new List<FieldBuffer>(fieldCount);

for (int i = 0; i < fieldCount; i++)
{
    string columnName = dataReader.GetName(i);

    if (this.columnHeader.TryGetValue(columnName, out FieldBuffer buffer))
    {
        MetadataIdentity metadata = buffer.Target.Identity.Metadata;
        ColumnMetadata columnInfo = new ColumnMetadata(columnName, dataReader.GetFieldType(i), dataReader.GetDataTypeName(i), i);
        // ...
    }
}
```

**Diff**
```diff
-        List<ColumnName> names = [];
-        List<FieldBuffer> bufferList = [];
+        int fieldCount = this.GetFieldCount(dataReader);
+        List<ColumnName> names = new List<ColumnName>(fieldCount);
+        List<FieldBuffer> bufferList = new List<FieldBuffer>(fieldCount);
 
-        for (int i = 0; i < this.GetFieldCount(dataReader); i++)
+        for (int i = 0; i < fieldCount; i++)
         {
             string columnName = dataReader.GetName(i);
 
             if (this.columnHeader.TryGetValue(columnName, out FieldBuffer buffer))
             {
                 MetadataIdentity metadata = buffer.Target.Identity.Metadata;
-                ColumnMetadata columnInfo = GetColumnInfo(i);
+                ColumnMetadata columnInfo = new ColumnMetadata(columnName, dataReader.GetFieldType(i), dataReader.GetDataTypeName(i), i);
 // ...
-
-        ColumnMetadata GetColumnInfo(int i) => new ColumnMetadata(dataReader.GetName(i), dataReader.GetFieldType(i), dataReader.GetDataTypeName(i), i);
```

### 3.9 Single-probe dedupe and presizing in `CommandBuffer.Prepare`
**File:** [Commands/CommandBuffer.cs:89-124](src/Mvc/Jerrycurl.Cqs/Commands/CommandBuffer.cs#L89-L124) · **Runs:** per parameter · **Expected gain:** Low · **Risk:** none

`Contains` followed by `Add` hashes each parameter name twice with `OrdinalIgnoreCase`, and the list and set both grow from empty. `if (paramSet.Add(...))` hashes once. Both collections can be presized to `paramHeader.Count`, the exact upper bound.

**Before**
```csharp
List<IDbDataParameter> parameters = [];
HashSet<string> paramSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
// ...
    if (!paramSet.Contains(adoParam.ParameterName))
    {
        parameters.Add(adoParam);
        paramSet.Add(adoParam.ParameterName);
    }
```

**After**
```csharp
List<IDbDataParameter> parameters = new List<IDbDataParameter>(this.paramHeader.Count);
HashSet<string> paramSet = new HashSet<string>(this.paramHeader.Count, StringComparer.OrdinalIgnoreCase);
// ...
    if (paramSet.Add(adoParam.ParameterName))
        parameters.Add(adoParam);
```

**Diff**
```diff
-        List<IDbDataParameter> parameters = [];
-        HashSet<string> paramSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
+        List<IDbDataParameter> parameters = new List<IDbDataParameter>(this.paramHeader.Count);
+        HashSet<string> paramSet = new HashSet<string>(this.paramHeader.Count, StringComparer.OrdinalIgnoreCase);
 // ...
-            if (!paramSet.Contains(adoParam.ParameterName))
-            {
-                parameters.Add(adoParam);
-                paramSet.Add(adoParam.ParameterName);
-            }
+            if (paramSet.Add(adoParam.ParameterName))
+                parameters.Add(adoParam);
```

### 3.10 `StringBuilder` in `RelationExtensions.AsCommand(textBuilder)`
**File:** [Language/RelationExtensions.cs:41-60](src/Mvc/Jerrycurl.Cqs/Language/RelationExtensions.cs#L41-L60) · **Runs:** per row · **Expected gain:** High · **Risk:** none; zero rows still leaves `CommandText` null

`command.CommandText += textBuilder(parameters) + Environment.NewLine` copies the whole accumulated text on every relation row, which is O(n²) in total length. Appending to a `StringBuilder` and assigning `CommandText` once is linear. `Append(null)` appends nothing, just as `null + NewLine` concatenation ignores the null. The builder is created only on the first row, so an empty relation still leaves `CommandText` as `null`.

**Before**
```csharp
using IRelationReader reader = relation.GetReader();

while (reader.Read())
{
    IList<IParameter> parameters = store.Add(reader);

    command.CommandText += textBuilder(parameters) + Environment.NewLine;
}

return command;
```

**After**
```csharp
using IRelationReader reader = relation.GetReader();

StringBuilder text = null;

while (reader.Read())
{
    IList<IParameter> parameters = store.Add(reader);

    (text ??= new StringBuilder()).Append(textBuilder(parameters)).Append(Environment.NewLine);
}

if (text != null)
    command.CommandText = text.ToString();

return command;
```

**Diff**
```diff
         using IRelationReader reader = relation.GetReader();
 
+        StringBuilder text = null;
+
         while (reader.Read())
         {
             IList<IParameter> parameters = store.Add(reader);
 
-            command.CommandText += textBuilder(parameters) + Environment.NewLine;
+            (text ??= new StringBuilder()).Append(textBuilder(parameters)).Append(Environment.NewLine);
         }
 
+        if (text != null)
+            command.CommandText = text.ToString();
+
         return command;
```

### 3.11 Close `ConfigureAwait(false)` gaps
**File:** [Commands/CommandBuffer.cs:36-42](src/Mvc/Jerrycurl.Cqs/Commands/CommandBuffer.cs#L36-L42), [Queries/QueryEngine.cs:64](src/Mvc/Jerrycurl.Cqs/Queries/QueryEngine.cs#L64), [Queries/QueryEngine.cs:87](src/Mvc/Jerrycurl.Cqs/Queries/QueryEngine.cs#L87), [Commands/CommandEngine.cs:41](src/Mvc/Jerrycurl.Cqs/Commands/CommandEngine.cs#L41), [Language/QueryExtensions.cs:170-253](src/Mvc/Jerrycurl.Cqs/Language/QueryExtensions.cs#L170-L253), [Language/CommandExtensions.cs:171-272](src/Mvc/Jerrycurl.Cqs/Language/CommandExtensions.cs#L171-L272) · **Runs:** per row (`ReadAsync`) / per query · **Expected gain:** Low · **Risk:** none; the null-session skip on dispose is kept

`CommandBuffer.UpdateAsync` awaits `ReadAsync` once per row on the captured context, unlike the matching `QueryBuffer.InsertAsync`. The engines' `await using` disposals and the 12 relation/source `InsertAsync`/`UpdateAsync` extension awaits also capture the context. Under a `SynchronizationContext`, every one of those continuations is posted back to it. Adding `ConfigureAwait(false)` makes them match the rest of the library. `await using (x.ConfigureAwait(false))` would throw `NullReferenceException` if the virtual `GetAsyncSession()` returned `null`, whereas `await using` on a bare null skips disposal. The engines therefore spell out what `await using` compiles to (`try`/`finally` with a null check) and add `ConfigureAwait(false)` to the `DisposeAsync` await. Behavior stays identical, including for a null session. `await` inside `finally` is also allowed in async iterators, so this works for `EnumerateAsync`.

**Before**
```csharp
while (await dataReader.ReadAsync(cancellationToken))
    updateAction(dataReader);
// QueryEngine.ExecuteAsync / EnumerateAsync, CommandEngine.ExecuteAsync
await using IAsyncSession connection = this.Options.GetAsyncSession();

foreach (IBatch batch in this.FilterBatches(queries))
{
    // ...
}
// QueryExtensions / CommandExtensions (x12)
await buffer.InsertAsync(dataReader, default);
await buffer.UpdateAsync(dataReader);
```

**After**
```csharp
while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
    updateAction(dataReader);
// QueryEngine.ExecuteAsync / EnumerateAsync, CommandEngine.ExecuteAsync
IAsyncSession connection = this.Options.GetAsyncSession();

try
{
    foreach (IBatch batch in this.FilterBatches(queries))
    {
        // ... (unchanged body, including `yield return` in EnumerateAsync)
    }
}
finally
{
    if (connection != null)
        await connection.DisposeAsync().ConfigureAwait(false);
}
// QueryExtensions / CommandExtensions (x12)
await buffer.InsertAsync(dataReader, default).ConfigureAwait(false);
await buffer.UpdateAsync(dataReader).ConfigureAwait(false);
```

**Diff**
```diff
-        while (await dataReader.ReadAsync(cancellationToken))
+        while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
             updateAction(dataReader);
 // QueryEngine (x2), CommandEngine (x1)
-        await using IAsyncSession connection = this.Options.GetAsyncSession();
+        IAsyncSession connection = this.Options.GetAsyncSession();
 
-        foreach (IBatch batch in this.FilterBatches(queries))
-        {
-            // ...
-        }
+        try
+        {
+            foreach (IBatch batch in this.FilterBatches(queries))
+            {
+                // ...
+            }
+        }
+        finally
+        {
+            if (connection != null)
+                await connection.DisposeAsync().ConfigureAwait(false);
+        }
 // QueryExtensions / CommandExtensions (x12)
-        await buffer.InsertAsync(dataReader, default);
+        await buffer.InsertAsync(dataReader, default).ConfigureAwait(false);
-        await buffer.UpdateAsync(dataReader);
+        await buffer.UpdateAsync(dataReader).ConfigureAwait(false);
```

### 3.12 Skip the async state machine in `AsyncSession.ApplyFilters` when there are no filters
**File:** [Sessions/AsyncSession.cs:140-148](src/Mvc/Jerrycurl.Cqs/Sessions/AsyncSession.cs#L140-L148) · **Runs:** per query (connection open and per command, about 4 calls) · **Expected gain:** Low · **Risk:** none; every caller awaits immediately and `FilterContext`'s constructor cannot throw

The outer overload is `async` only to forward to the inner overload. With no filters it still runs a state machine (cheap, but not free), and with filters it adds an extra async layer. A plain `Task`-returning method returns `Task.CompletedTask` or the inner task directly. Exceptions are unchanged because the inner method is still `async` and every caller awaits at once.

**Before**
```csharp
private async Task ApplyFilters(Func<IFilterAsyncHandler, Func<FilterContext, Task>> action, IDbCommand dbCommand = null, Exception exception = null, IBatch batch = null, bool swallowExceptions = false)
{
    if (this.filters.Length > 0)
    {
        FilterContext context = new FilterContext(this.connection, dbCommand, exception, batch);

        await this.ApplyFilters(action, context, swallowExceptions).ConfigureAwait(false);
    }
}
```

**After**
```csharp
private Task ApplyFilters(Func<IFilterAsyncHandler, Func<FilterContext, Task>> action, IDbCommand dbCommand = null, Exception exception = null, IBatch batch = null, bool swallowExceptions = false)
{
    if (this.filters.Length == 0)
        return Task.CompletedTask;

    FilterContext context = new FilterContext(this.connection, dbCommand, exception, batch);

    return this.ApplyFilters(action, context, swallowExceptions);
}
```

**Diff**
```diff
-    private async Task ApplyFilters(Func<IFilterAsyncHandler, Func<FilterContext, Task>> action, IDbCommand dbCommand = null, Exception exception = null, IBatch batch = null, bool swallowExceptions = false)
+    private Task ApplyFilters(Func<IFilterAsyncHandler, Func<FilterContext, Task>> action, IDbCommand dbCommand = null, Exception exception = null, IBatch batch = null, bool swallowExceptions = false)
     {
-        if (this.filters.Length > 0)
-        {
-            FilterContext context = new FilterContext(this.connection, dbCommand, exception, batch);
+        if (this.filters.Length == 0)
+            return Task.CompletedTask;
 
-            await this.ApplyFilters(action, context, swallowExceptions).ConfigureAwait(false);
-        }
+        FilterContext context = new FilterContext(this.connection, dbCommand, exception, batch);
+
+        return this.ApplyFilters(action, context, swallowExceptions);
     }
```

### 3.13 `System.Threading.Lock` in `BufferCache`
**File:** [Queries/Internal/Caching/BufferCache.cs:17](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Caching/BufferCache.cs#L17) · **Runs:** one-time (per query shape, during parsing) · **Expected gain:** Low · **Risk:** none; `Lock` is reentrant like `Monitor`

The three `lock (this.state)` sites guard index allocation while a list query is parsed. On net9 and later, `lock` on a `System.Threading.Lock` compiles to `EnterScope()`, which is lighter than `Monitor` on a plain object and avoids object-header sync-block inflation. The `lock` statements themselves do not change.

**Before**
```csharp
private readonly object state = new object();
```

**After**
```csharp
using System.Threading;
// ...
private readonly Lock state = new Lock();
```

**Diff**
```diff
+using System.Threading;
 // ...
-    private readonly object state = new object();
+    private readonly Lock state = new Lock();
```

### 3.14 Deep tier: bake `helpers`/`schema` in as constants and compile straight to the public delegates
**File:** [Queries/Internal/Compilation/QueryCompiler.cs:22-154](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Compilation/QueryCompiler.cs#L22-L154), [:801-808](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Compilation/QueryCompiler.cs#L801-L808) · **Runs:** per row (`EnumerateFactory`, `ListFactory.WriteOne`) / per result set (`WriteAll`, `Initialize`, aggregate) · **Expected gain:** Medium · **Risk:** low; the expression bodies are unchanged and only the outer lambda is new

Each factory compiles a private 3- to 5-argument delegate and wraps it in a C# lambda that captures `helpers` and `schema` and unpacks `buf.ListData`/`buf.AggregateData`. That adds a closure, a second delegate invocation and argument shuffling on every row for `Enumerate<T>` and `InsertAsync`. Both values are fixed per cached factory, so they can be `Expression.Constant`s. The change wraps each existing body in a block that declares the shared `Arguments.*` parameters as locals: the helpers and schema come from constants, and lists and aggregates come from `IQueryBuffer.ListData`/`AggregateData` (internal interface members, reachable because `Compile()` skips visibility checks, which already lets the generated code call the private `GetInvalidCastException`). The body then compiles straight to `EnumerateFactory<TItem>`, `Action<IQueryBuffer, IDataReader>`, `Action<IQueryBuffer>` or `AggregateFactory`. `ListData`/`AggregateData` are still read on each call because `QueryBuffer.Flush` swaps them on every `Commit`. The aggregate `Initialize`/`WriteAll` keep a thin C# wrapper for `AggregateHeader.AddRange(header)`, which runs once per result set.

**Before**
```csharp
private delegate void ListInternalWriter(IDataReader dataReader, ElasticArray lists, ElasticArray aggregates, ElasticArray helpers, ISchema schema);
private delegate void ListInternalInitializer(ElasticArray lists);
private delegate object AggregateInternalReader(ElasticArray lists, ElasticArray aggregates, ISchema schema);
private delegate TItem EnumerateInternalReader<TItem>(IDataReader dataReader, ElasticArray helpers, ISchema schema);

private ListFactory CompileBuffer(ListResult result, Expression initialize, Expression writeOne, Expression writeAll)
{
    ParameterExpression[] initArgs = [Arguments.Lists];
    ParameterExpression[] writeArgs = [Arguments.DataReader, Arguments.Lists, Arguments.Aggregates, Arguments.Helpers, Arguments.Schema];

    ListInternalInitializer initializeFunc = this.Compile<ListInternalInitializer>(initialize, initArgs);
    ListInternalWriter writeOneFunc = this.Compile<ListInternalWriter>(writeOne, writeArgs);
    ListInternalWriter writeAllFunc = this.Compile<ListInternalWriter>(writeAll, writeArgs);

    ElasticArray helpers = this.GetHelperBuffer(result.Helpers);
    ISchema schema = result.Schema;

    if (result.QueryType == QueryType.Aggregate)
    {
        // ...
            Initialize = buf => { buf.AggregateHeader.AddRange(header); initializeFunc(buf.ListData); },
            WriteOne = (buf, dr) => writeOneFunc(dr, buf.ListData, buf.AggregateData, helpers, schema),
            WriteAll = (buf, dr) => { buf.AggregateHeader.AddRange(header); writeAllFunc(dr, buf.ListData, buf.AggregateData, helpers, schema); },
        // ...
    }
    // ...
        WriteAll = (buf, dr) => writeAllFunc(dr, buf.ListData, buf.AggregateData, helpers, schema),
        WriteOne = (buf, dr) => writeOneFunc(dr, buf.ListData, buf.AggregateData, helpers, schema),
        Initialize = buf => initializeFunc(buf.ListData),
}
// Compile(AggregateResult)
    ParameterExpression[] arguments = [Arguments.Lists, Arguments.Aggregates, Arguments.Schema];
    AggregateInternalReader reader = this.Compile<AggregateInternalReader>(body, arguments);
    ISchema schema = result.Schema;
    return buf => reader(buf.ListData, buf.AggregateData, schema);
// Compile<TItem>(EnumerateResult)
    ParameterExpression[] arguments = [Arguments.DataReader, Arguments.Helpers, Arguments.Schema];
    EnumerateInternalReader<TItem> reader = this.Compile<EnumerateInternalReader<TItem>>(body, arguments);
    ElasticArray helpers = this.GetHelperBuffer(result.Helpers);
    ISchema schema = result.Schema;
    return dr => reader(dr, helpers, schema);
```

**After**
```csharp
private static readonly PropertyInfo listDataProperty = typeof(IQueryBuffer).GetProperty(nameof(IQueryBuffer.ListData), BindingFlags.Instance | BindingFlags.NonPublic);
private static readonly PropertyInfo aggregateDataProperty = typeof(IQueryBuffer).GetProperty(nameof(IQueryBuffer.AggregateData), BindingFlags.Instance | BindingFlags.NonPublic);

private TDelegate CompileBound<TDelegate>(Expression body, ElasticArray helpers, ISchema schema, params ParameterExpression[] arguments)
{
    List<ParameterExpression> locals = [Arguments.Helpers, Arguments.Schema];
    List<Expression> block =
    [
        Expression.Assign(Arguments.Helpers, Expression.Constant(helpers, typeof(ElasticArray))),
        Expression.Assign(Arguments.Schema, Expression.Constant(schema, typeof(ISchema))),
    ];

    if (arguments.Contains(Arguments.Buffer))
    {
        locals.Add(Arguments.Lists);
        locals.Add(Arguments.Aggregates);
        block.Add(Expression.Assign(Arguments.Lists, Expression.Property(Arguments.Buffer, listDataProperty)));
        block.Add(Expression.Assign(Arguments.Aggregates, Expression.Property(Arguments.Buffer, aggregateDataProperty)));
    }

    block.Add(body);

    return Expression.Lambda<TDelegate>(Expression.Block(locals, block), arguments).Compile();
}

private ListFactory CompileBuffer(ListResult result, Expression initialize, Expression writeOne, Expression writeAll)
{
    ElasticArray helpers = this.GetHelperBuffer(result.Helpers);
    ISchema schema = result.Schema;

    Action<IQueryBuffer> initializeFunc = this.CompileBound<Action<IQueryBuffer>>(initialize, helpers, schema, Arguments.Buffer);
    Action<IQueryBuffer, IDataReader> writeOneFunc = this.CompileBound<Action<IQueryBuffer, IDataReader>>(writeOne, helpers, schema, Arguments.Buffer, Arguments.DataReader);
    Action<IQueryBuffer, IDataReader> writeAllFunc = this.CompileBound<Action<IQueryBuffer, IDataReader>>(writeAll, helpers, schema, Arguments.Buffer, Arguments.DataReader);

    if (result.QueryType == QueryType.Aggregate)
    {
        AggregateAttribute[] header = [.. result.Aggregates.Select(a => a.Attribute).NotNull()];

        return new ListFactory()
        {
            Initialize = buf => { buf.AggregateHeader.AddRange(header); initializeFunc(buf); },
            WriteOne = writeOneFunc,
            WriteAll = (buf, dr) => { buf.AggregateHeader.AddRange(header); writeAllFunc(buf, dr); },
        };
    }

    return new ListFactory() { WriteAll = writeAllFunc, WriteOne = writeOneFunc, Initialize = initializeFunc };
}
// Compile(AggregateResult)
    return this.CompileBound<AggregateFactory>(body, null, result.Schema, Arguments.Buffer);
// Compile<TItem>(EnumerateResult)
    ElasticArray helpers = this.GetHelperBuffer(result.Helpers);

    return this.CompileBound<EnumerateFactory<TItem>>(this.GetBlockOrExpression(body), helpers, result.Schema, Arguments.DataReader);
// Arguments
    public static ParameterExpression Buffer { get; } = Expression.Parameter(typeof(IQueryBuffer), "buffer");
```

**Diff**
```diff
-    private delegate void ListInternalWriter(IDataReader dataReader, ElasticArray lists, ElasticArray aggregates, ElasticArray helpers, ISchema schema);
-    private delegate void ListInternalInitializer(ElasticArray lists);
-    private delegate object AggregateInternalReader(ElasticArray lists, ElasticArray aggregates, ISchema schema);
-    private delegate TItem EnumerateInternalReader<TItem>(IDataReader dataReader, ElasticArray helpers, ISchema schema);
+    private static readonly PropertyInfo listDataProperty = typeof(IQueryBuffer).GetProperty(nameof(IQueryBuffer.ListData), BindingFlags.Instance | BindingFlags.NonPublic);
+    private static readonly PropertyInfo aggregateDataProperty = typeof(IQueryBuffer).GetProperty(nameof(IQueryBuffer.AggregateData), BindingFlags.Instance | BindingFlags.NonPublic);
+
+    private TDelegate CompileBound<TDelegate>(Expression body, ElasticArray helpers, ISchema schema, params ParameterExpression[] arguments)
+    { /* see After */ }
 
     private ListFactory CompileBuffer(ListResult result, Expression initialize, Expression writeOne, Expression writeAll)
     {
-        ParameterExpression[] initArgs = [Arguments.Lists];
-        ParameterExpression[] writeArgs = [Arguments.DataReader, Arguments.Lists, Arguments.Aggregates, Arguments.Helpers, Arguments.Schema];
-
-        ListInternalInitializer initializeFunc = this.Compile<ListInternalInitializer>(initialize, initArgs);
-        ListInternalWriter writeOneFunc = this.Compile<ListInternalWriter>(writeOne, writeArgs);
-        ListInternalWriter writeAllFunc = this.Compile<ListInternalWriter>(writeAll, writeArgs);
-
         ElasticArray helpers = this.GetHelperBuffer(result.Helpers);
         ISchema schema = result.Schema;
+
+        Action<IQueryBuffer> initializeFunc = this.CompileBound<Action<IQueryBuffer>>(initialize, helpers, schema, Arguments.Buffer);
+        Action<IQueryBuffer, IDataReader> writeOneFunc = this.CompileBound<Action<IQueryBuffer, IDataReader>>(writeOne, helpers, schema, Arguments.Buffer, Arguments.DataReader);
+        Action<IQueryBuffer, IDataReader> writeAllFunc = this.CompileBound<Action<IQueryBuffer, IDataReader>>(writeAll, helpers, schema, Arguments.Buffer, Arguments.DataReader);
 
         if (result.QueryType == QueryType.Aggregate)
         {
 // ...
-                Initialize = buf => { buf.AggregateHeader.AddRange(header); initializeFunc(buf.ListData); },
-                WriteOne = (buf, dr) => writeOneFunc(dr, buf.ListData, buf.AggregateData, helpers, schema),
-                WriteAll = (buf, dr) => { buf.AggregateHeader.AddRange(header); writeAllFunc(dr, buf.ListData, buf.AggregateData, helpers, schema); },
+                Initialize = buf => { buf.AggregateHeader.AddRange(header); initializeFunc(buf); },
+                WriteOne = writeOneFunc,
+                WriteAll = (buf, dr) => { buf.AggregateHeader.AddRange(header); writeAllFunc(buf, dr); },
 // ...
-                WriteAll = (buf, dr) => writeAllFunc(dr, buf.ListData, buf.AggregateData, helpers, schema),
-                WriteOne = (buf, dr) => writeOneFunc(dr, buf.ListData, buf.AggregateData, helpers, schema),
-                Initialize = buf => initializeFunc(buf.ListData),
+                WriteAll = writeAllFunc,
+                WriteOne = writeOneFunc,
+                Initialize = initializeFunc,
 // Compile(AggregateResult)
-        ParameterExpression[] arguments = [Arguments.Lists, Arguments.Aggregates, Arguments.Schema];
-        AggregateInternalReader reader = this.Compile<AggregateInternalReader>(body, arguments);
-
-        ISchema schema = result.Schema;
-
-        return buf => reader(buf.ListData, buf.AggregateData, schema);
+        return this.CompileBound<AggregateFactory>(body, null, result.Schema, Arguments.Buffer);
 // Compile<TItem>(EnumerateResult)
-        ParameterExpression[] arguments = [Arguments.DataReader, Arguments.Helpers, Arguments.Schema];
-        EnumerateInternalReader<TItem> reader = this.Compile<EnumerateInternalReader<TItem>>(body, arguments);
-
         ElasticArray helpers = this.GetHelperBuffer(result.Helpers);
-        ISchema schema = result.Schema;
-
-        return dr => reader(dr, helpers, schema);
+
+        return this.CompileBound<EnumerateFactory<TItem>>(this.GetBlockOrExpression(body), helpers, result.Schema, Arguments.DataReader);
 // Arguments
+        public static ParameterExpression Buffer { get; } = Expression.Parameter(typeof(IQueryBuffer), "buffer");
```
The `_ => default` early return for `result.Value == null` stays. Return-type checks are unchanged: `AggregateFactory` returns `object`, as `AggregateInternalReader` did, and `Action<...>` accepts any body type, as the `void` internal delegates did.

### 3.15 Deep tier: single-lookup join buffer via `CollectionsMarshal`
**File:** [Queries/Internal/Compilation/QueryCompiler.cs:291-323](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Compilation/QueryCompiler.cs#L291-L323), [:672-684](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Compilation/QueryCompiler.cs#L672-L684) · **Runs:** per row (per join) · **Expected gain:** Medium · **Risk:** none; same result, and a null dictionary or key still throws

The generated code is `if (!dict.TryGetValue(key, out _joinbuf)) dict.Add(key, _joinbuf = new ElasticArray())`. On every miss (each new parent key) that hashes and compares twice. Because `dictKey` is the `Assign(key.Variable, new CompositeKey(...))` expression, it is also evaluated twice, so the composite key is built twice (`dictKey2` is never used). The change emits one call to a private static generic helper that uses `CollectionsMarshal.GetValueRefOrAddDefault`: one hash per row, and the key is built once. Values in these dictionaries are only ever `new ElasticArray()`, and an existing entry is returned as-is, so results are identical. `System.Runtime.InteropServices` is already imported.

**Before**
```csharp
Expression dictKey, dictKey2;

if (join.Key.Variable != null)
{
    Expression newKey = this.GetNewCompositeKeyExpression(join.Key);

    dictKey = Expression.Assign(join.Key.Variable, newKey);
    dictKey2 = join.Key.Variable;
}
else
    dictKey = dictKey2 = join.Key.Values[0].Variable;

Expression tryGet = this.GetDictionaryTryGetExpression(join.List.Variable, dictKey, join.Buffer);
Expression newBuffer = Expression.New(typeof(ElasticArray));
Expression bufferIndex = this.GetElasticIndexExpression(join.Buffer, join.Index);
Expression assignBuffer = Expression.Assign(join.Buffer, newBuffer);
Expression addArray = this.GetDictionaryAddExpression(join.List.Variable, dictKey, assignBuffer);
Expression getOrAdd = Expression.IfThen(Expression.Not(tryGet), addArray);
```

**After**
```csharp
private static readonly MethodInfo getOrAddJoinBufferMethod = typeof(QueryCompiler).GetMethod(nameof(GetOrAddJoinBuffer), BindingFlags.NonPublic | BindingFlags.Static);

private static ElasticArray GetOrAddJoinBuffer<TKey>(Dictionary<TKey, ElasticArray> dictionary, TKey key)
{
    ref ElasticArray buffer = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out bool exists);

    if (!exists)
        buffer = new ElasticArray();

    return buffer;
}

// GetKeyInitializeBufferExpression
Expression dictKey;

if (join.Key.Variable != null)
    dictKey = Expression.Assign(join.Key.Variable, this.GetNewCompositeKeyExpression(join.Key));
else
    dictKey = join.Key.Values[0].Variable;

Type keyType = join.List.Variable.Type.GetGenericArguments()[0];
MethodInfo getOrAddMethod = getOrAddJoinBufferMethod.MakeGenericMethod(keyType);
Expression getOrAdd = Expression.Assign(join.Buffer, Expression.Call(getOrAddMethod, join.List.Variable, dictKey));
// isNullVars / IfThenElse(isNull, setNull, getOrAdd) unchanged
```

**Diff**
```diff
-        Expression dictKey, dictKey2;
+        Expression dictKey;
 
         if (join.Key.Variable != null)
-        {
-            Expression newKey = this.GetNewCompositeKeyExpression(join.Key);
-
-            dictKey = Expression.Assign(join.Key.Variable, newKey);
-            dictKey2 = join.Key.Variable;
-        }
+            dictKey = Expression.Assign(join.Key.Variable, this.GetNewCompositeKeyExpression(join.Key));
         else
-            dictKey = dictKey2 = join.Key.Values[0].Variable;
-        
-        Expression tryGet = this.GetDictionaryTryGetExpression(join.List.Variable, dictKey, join.Buffer);
-        Expression newBuffer = Expression.New(typeof(ElasticArray));
-        Expression bufferIndex = this.GetElasticIndexExpression(join.Buffer, join.Index);
-        Expression assignBuffer = Expression.Assign(join.Buffer, newBuffer);
-        Expression addArray = this.GetDictionaryAddExpression(join.List.Variable, dictKey, assignBuffer);
-        Expression getOrAdd = Expression.IfThen(Expression.Not(tryGet), addArray);
+            dictKey = join.Key.Values[0].Variable;
+
+        Type keyType = join.List.Variable.Type.GetGenericArguments()[0];
+        MethodInfo getOrAddMethod = getOrAddJoinBufferMethod.MakeGenericMethod(keyType);
+        Expression getOrAdd = Expression.Assign(join.Buffer, Expression.Call(getOrAddMethod, join.List.Variable, dictKey));
 // ...
+    private static ElasticArray GetOrAddJoinBuffer<TKey>(Dictionary<TKey, ElasticArray> dictionary, TKey key)
+    {
+        ref ElasticArray buffer = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out bool exists);
+
+        if (!exists)
+            buffer = new ElasticArray();
+
+        return buffer;
+    }
```
`GetDictionaryAddExpression`/`GetDictionaryTryGetExpression` become unused and can be removed. `Expression.Call` requires `dictKey.Type` to be assignable to `TKey`, which is the same constraint `TryGetValue`/`Add` impose today.

### 3.16 Deep tier: `ElasticArray` backed by a growable array
**File:** [Queries/Internal/ElasticArray.cs:6-36](src/Mvc/Jerrycurl.Cqs/Queries/Internal/ElasticArray.cs#L6-L36), [Queries/Internal/Compilation/QueryCompiler.cs:291-323](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Compilation/QueryCompiler.cs#L291-L323) · **Runs:** per cell (every generated list/aggregate/helper/join slot access), plus one allocation per new join key · **Expected gain:** Medium · **Risk:** low; the message text for a negative index differs

Today each indexer access calls `EnsureIndex` and then the `List<T>` indexer, which means two bounds checks and a call. Growth allocates a temporary `TValue[]` just to `AddRange` defaults. Every join buffer (created per distinct parent key) costs a `List` plus a 2-slot array. The change keeps an explicit `count` and backs it with a `TValue[]`. **Indexer semantics are preserved:** a get or set at `index >= count` extends `count` to `index + 1`, filling with `default`, and a get returns `default(TValue)`/`null`. A negative index still throws `ArgumentOutOfRangeException`, but with a different message (the generated code only uses non-negative constant indices). Enumeration still yields exactly `count` items; nothing enumerates `ElasticArray` today. The parameterless constructor keeps the old capacity of 2. A capacity constructor lets join buffers be presized to `join.Index + 1`, which is known after parsing, building on 3.15. The indexer is still a property named `Item`, so `GetElasticIndexExpression` and `typeof(ElasticArray)` usages are unchanged.

**Before**
```csharp
internal class ElasticArray<TValue> : IEnumerable<TValue>
{
    private readonly List<TValue> innerList = new List<TValue>(2);

    public TValue this[int index]
    {
        get
        {
            this.EnsureIndex(index);

            return this.innerList[index];
        }
        set
        {
            this.EnsureIndex(index);

            this.innerList[index] = value;
        }
    }

    private void EnsureIndex(int index)
    {
        if (index >= this.innerList.Count)
            this.innerList.AddRange(new TValue[index - this.innerList.Count + 1]);
    }

    public IEnumerator<TValue> GetEnumerator() => this.innerList.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}

internal class ElasticArray : ElasticArray<object> { }
```

**After**
```csharp
internal class ElasticArray<TValue> : IEnumerable<TValue>
{
    private TValue[] items;
    private int count;

    public ElasticArray() : this(2) { }
    public ElasticArray(int capacity) => this.items = new TValue[Math.Max(capacity, 0)];

    public TValue this[int index]
    {
        get
        {
            if ((uint)index >= (uint)this.count)
                this.EnsureIndex(index);

            return this.items[index];
        }
        set
        {
            if ((uint)index >= (uint)this.count)
                this.EnsureIndex(index);

            this.items[index] = value;
        }
    }

    private void EnsureIndex(int index)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (index >= this.items.Length)
            Array.Resize(ref this.items, Math.Max(index + 1, this.items.Length * 2));

        this.count = index + 1;
    }

    public IEnumerator<TValue> GetEnumerator()
    {
        for (int i = 0; i < this.count; i++)
            yield return this.items[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}

internal class ElasticArray : ElasticArray<object>
{
    public ElasticArray() { }
    public ElasticArray(int capacity) : base(capacity) { }
}

// QueryCompiler (builds on 3.15): presize join buffers
private static ElasticArray GetOrAddJoinBuffer<TKey>(Dictionary<TKey, ElasticArray> dictionary, TKey key, int capacity)
{
    ref ElasticArray buffer = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out bool exists);

    if (!exists)
        buffer = new ElasticArray(capacity);

    return buffer;
}
// ...
Expression getOrAdd = Expression.Assign(join.Buffer, Expression.Call(getOrAddMethod, join.List.Variable, dictKey, Expression.Constant(join.Index + 1)));
```

**Diff**
```diff
 internal class ElasticArray<TValue> : IEnumerable<TValue>
 {
-    private readonly List<TValue> innerList = new List<TValue>(2);
+    private TValue[] items;
+    private int count;
+
+    public ElasticArray() : this(2) { }
+    public ElasticArray(int capacity) => this.items = new TValue[Math.Max(capacity, 0)];
 
     public TValue this[int index]
     {
         get
         {
-            this.EnsureIndex(index);
+            if ((uint)index >= (uint)this.count)
+                this.EnsureIndex(index);
 
-            return this.innerList[index];
+            return this.items[index];
         }
         set
         {
-            this.EnsureIndex(index);
+            if ((uint)index >= (uint)this.count)
+                this.EnsureIndex(index);
 
-            this.innerList[index] = value;
+            this.items[index] = value;
         }
     }
 
     private void EnsureIndex(int index)
     {
-        if (index >= this.innerList.Count)
-            this.innerList.AddRange(new TValue[index - this.innerList.Count + 1]);
+        if (index < 0)
+            throw new ArgumentOutOfRangeException(nameof(index));
+
+        if (index >= this.items.Length)
+            Array.Resize(ref this.items, Math.Max(index + 1, this.items.Length * 2));
+
+        this.count = index + 1;
     }
 
-    public IEnumerator<TValue> GetEnumerator() => this.innerList.GetEnumerator();
+    public IEnumerator<TValue> GetEnumerator()
+    {
+        for (int i = 0; i < this.count; i++)
+            yield return this.items[i];
+    }
+
     IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
 }
 
-internal class ElasticArray : ElasticArray<object> { }
+internal class ElasticArray : ElasticArray<object>
+{
+    public ElasticArray() { }
+    public ElasticArray(int capacity) : base(capacity) { }
+}
 // QueryCompiler (on top of 3.15)
-    private static ElasticArray GetOrAddJoinBuffer<TKey>(Dictionary<TKey, ElasticArray> dictionary, TKey key)
+    private static ElasticArray GetOrAddJoinBuffer<TKey>(Dictionary<TKey, ElasticArray> dictionary, TKey key, int capacity)
 // ...
-            buffer = new ElasticArray();
+            buffer = new ElasticArray(capacity);
 // ...
-        Expression getOrAdd = Expression.Assign(join.Buffer, Expression.Call(getOrAddMethod, join.List.Variable, dictKey));
+        Expression getOrAdd = Expression.Assign(join.Buffer, Expression.Call(getOrAddMethod, join.List.Variable, dictKey, Expression.Constant(join.Index + 1)));
```
`ElasticArray.cs` needs `using System;` for `Math`, `Array` and `ArgumentOutOfRangeException`. The enumerator no longer throws `InvalidOperationException` on modification during enumeration, but nothing enumerates the type.

### 3.17 Deep tier: cache reflection lookups in `QueryCompiler` and `CommandCompiler`
**File:** [Queries/Internal/Compilation/QueryCompiler.cs:495-500](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Compilation/QueryCompiler.cs#L495-L500), [:549-563](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Compilation/QueryCompiler.cs#L549-L563), [:662-670](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Compilation/QueryCompiler.cs#L662-L670), [:709-722](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Compilation/QueryCompiler.cs#L709-L722), [:753-758](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Compilation/QueryCompiler.cs#L753-L758), [Commands/Internal/Compilation/CommandCompiler.cs:115-159](src/Mvc/Jerrycurl.Cqs/Commands/Internal/Compilation/CommandCompiler.cs#L115-L159) · **Runs:** one-time (per query shape compile) · **Expected gain:** Low · **Risk:** none

Expression building calls `GetMethod`/`GetProperty`/`GetField` with argument-type arrays for every column, every slot access and every try/catch: `IDataRecord.IsDBNull`, `IDataRecord.GetValue`, `IDataReader.Read`, `GetInvalidCastException`, the `ElasticArray` indexer, `DBNull.Value` and `FieldBuffer.Write`. These are fixed members, so `private static readonly` fields hold them. The indexer keeps a fallback for any non-`ElasticArray` expression type, so behavior is the same even for unexpected inputs. Dictionary `TryGetValue`/`Add` lookups depend on the closed generic type and are removed by 3.15 anyway.

**Before**
```csharp
// QueryCompiler
MethodInfo isNullMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), [typeof(int)]);
// ...
    readMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), [typeof(int)]);
// ...
Expression callRead = Expression.Call(Arguments.DataReader, typeof(IDataReader).GetMethod(nameof(IDataReader.Read)));
// ...
MethodInfo constructor = typeof(QueryCompiler).GetStaticMethod(nameof(QueryCompiler.GetInvalidCastException), typeof(ISchema), typeof(string), typeof(Exception));
// ...
PropertyInfo indexer = arrayExpression.Type.GetProperty("Item");

// CommandCompiler
MethodInfo nullMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), [typeof(int)]);
Expression dbNull = Expression.Field(null, typeof(DBNull).GetField(nameof(DBNull.Value)));
MethodInfo writeMethod = typeof(FieldBuffer).GetMethod(nameof(FieldBuffer.Write));
    readMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), [typeof(int)]);
```

**After**
```csharp
// QueryCompiler
private static readonly MethodInfo isDbNullMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), [typeof(int)]);
private static readonly MethodInfo getValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), [typeof(int)]);
private static readonly MethodInfo dataReaderReadMethod = typeof(IDataReader).GetMethod(nameof(IDataReader.Read));
private static readonly MethodInfo invalidCastMethod = typeof(QueryCompiler).GetStaticMethod(nameof(GetInvalidCastException), typeof(ISchema), typeof(string), typeof(Exception));
private static readonly PropertyInfo elasticIndexer = typeof(ElasticArray).GetProperty("Item");
// ...
MethodInfo isNullMethod = isDbNullMethod;
// ...
    readMethod = getValueMethod;
// ...
Expression callRead = Expression.Call(Arguments.DataReader, dataReaderReadMethod);
// ...
MethodInfo constructor = invalidCastMethod;
// ...
PropertyInfo indexer = arrayExpression.Type == typeof(ElasticArray) ? elasticIndexer : arrayExpression.Type.GetProperty("Item");

// CommandCompiler
private static readonly MethodInfo isDbNullMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), [typeof(int)]);
private static readonly MethodInfo getValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), [typeof(int)]);
private static readonly FieldInfo dbNullValueField = typeof(DBNull).GetField(nameof(DBNull.Value));
private static readonly MethodInfo fieldBufferWriteMethod = typeof(FieldBuffer).GetMethod(nameof(FieldBuffer.Write));
// ...
MethodInfo nullMethod = isDbNullMethod;
Expression dbNull = Expression.Field(null, dbNullValueField);
MethodInfo writeMethod = fieldBufferWriteMethod;
    readMethod = getValueMethod;
```

**Diff**
```diff
 // QueryCompiler
+    private static readonly MethodInfo isDbNullMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), [typeof(int)]);
+    private static readonly MethodInfo getValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), [typeof(int)]);
+    private static readonly MethodInfo dataReaderReadMethod = typeof(IDataReader).GetMethod(nameof(IDataReader.Read));
+    private static readonly MethodInfo invalidCastMethod = typeof(QueryCompiler).GetStaticMethod(nameof(GetInvalidCastException), typeof(ISchema), typeof(string), typeof(Exception));
+    private static readonly PropertyInfo elasticIndexer = typeof(ElasticArray).GetProperty("Item");
 // ...
-        MethodInfo isNullMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), [typeof(int)]);
+        MethodInfo isNullMethod = isDbNullMethod;
 // ...
-            readMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), [typeof(int)]);
+            readMethod = getValueMethod;
 // ...
-        Expression callRead = Expression.Call(Arguments.DataReader, typeof(IDataReader).GetMethod(nameof(IDataReader.Read)));
+        Expression callRead = Expression.Call(Arguments.DataReader, dataReaderReadMethod);
 // ...
-        MethodInfo constructor = typeof(QueryCompiler).GetStaticMethod(nameof(QueryCompiler.GetInvalidCastException), typeof(ISchema), typeof(string), typeof(Exception));
+        MethodInfo constructor = invalidCastMethod;
 // ...
-        PropertyInfo indexer = arrayExpression.Type.GetProperty("Item");
+        PropertyInfo indexer = arrayExpression.Type == typeof(ElasticArray) ? elasticIndexer : arrayExpression.Type.GetProperty("Item");
 // CommandCompiler
+    private static readonly MethodInfo isDbNullMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), [typeof(int)]);
+    private static readonly MethodInfo getValueMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), [typeof(int)]);
+    private static readonly FieldInfo dbNullValueField = typeof(DBNull).GetField(nameof(DBNull.Value));
+    private static readonly MethodInfo fieldBufferWriteMethod = typeof(FieldBuffer).GetMethod(nameof(FieldBuffer.Write));
 // ...
-        MethodInfo nullMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), [typeof(int)]);
+        MethodInfo nullMethod = isDbNullMethod;
-        Expression dbNull = Expression.Field(null, typeof(DBNull).GetField(nameof(DBNull.Value)));
+        Expression dbNull = Expression.Field(null, dbNullValueField);
-        MethodInfo writeMethod = typeof(FieldBuffer).GetMethod(nameof(FieldBuffer.Write));
+        MethodInfo writeMethod = fieldBufferWriteMethod;
-            readMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), [typeof(int)]);
+            readMethod = getValueMethod;
```

### 3.18 Deep tier: bake the helper object into the `CommandCompiler` converter
**File:** [Commands/Internal/Compilation/CommandCompiler.cs:16-85](src/Mvc/Jerrycurl.Cqs/Commands/Internal/Compilation/CommandCompiler.cs#L16-L85) · **Runs:** per parameter (each bound field on `Commit`, via `FieldBuffer.Bind`) · **Expected gain:** Medium · **Risk:** none; same conversion, and a null helper still converts to null

`Compile(MetadataIdentity, ColumnMetadata)` compiles a private two-argument `BufferInternalConverter(object value, object helper)` and returns the C# closure `value => innerFunc(value, helperObject)`. Every converted field therefore pays two delegate invocations. `helperObject` (`binding?.Helper?.Object`) is fixed for the cached converter, so it can become an `Expression.Constant(helperObject, typeof(object))` in place of `helperParam`, using the same constant-baking trick as 3.14. The lambda then compiles directly to `BufferConverter`. The `Convert(object → helperType)` and the block that assigns `helperVariable` are unchanged, so a null helper still converts to null exactly as before. The body is still object-typed or reference-assignable to `object` (`GetObjectExpression` boxes value types), so `Expression.Lambda<BufferConverter>` accepts it just as `BufferInternalConverter` did.

**Before**
```csharp
private delegate object BufferInternalConverter(object value, object helper);

public BufferConverter Compile(MetadataIdentity metadata, ColumnMetadata columnInfo)
{
    IBindingMetadata binding = metadata.Lookup<IBindingMetadata>();

    ParameterExpression inputParam = Expression.Parameter(typeof(object));
    ParameterExpression helperParam = Expression.Parameter(typeof(object));
    ParameterExpression helperVariable = this.GetHelperVariable(binding);
    // ...
    value = this.GetObjectExpression(value);

    if (helperVariable != null)
    {
        Expression typedParam = Expression.Convert(helperParam, helperVariable.Type);
        Expression assignHelper = Expression.Assign(helperVariable, typedParam);

        value = Expression.Block([helperVariable], assignHelper, value);
    }

    BufferInternalConverter innerFunc = Expression.Lambda<BufferInternalConverter>(value, inputParam, helperParam).Compile();

    object helperObject = binding?.Helper?.Object;

    return value => innerFunc(value, helperObject);
}
```

**After**
```csharp
public BufferConverter Compile(MetadataIdentity metadata, ColumnMetadata columnInfo)
{
    IBindingMetadata binding = metadata.Lookup<IBindingMetadata>();

    ParameterExpression inputParam = Expression.Parameter(typeof(object));
    ParameterExpression helperVariable = this.GetHelperVariable(binding);
    // ...
    value = this.GetObjectExpression(value);

    if (helperVariable != null)
    {
        object helperObject = binding?.Helper?.Object;

        Expression helperConstant = Expression.Constant(helperObject, typeof(object));
        Expression typedParam = Expression.Convert(helperConstant, helperVariable.Type);
        Expression assignHelper = Expression.Assign(helperVariable, typedParam);

        value = Expression.Block([helperVariable], assignHelper, value);
    }

    return Expression.Lambda<BufferConverter>(value, inputParam).Compile();
}
```

**Diff**
```diff
-    private delegate object BufferInternalConverter(object value, object helper);
-
     public BufferConverter Compile(MetadataIdentity metadata, ColumnMetadata columnInfo)
     {
         IBindingMetadata binding = metadata.Lookup<IBindingMetadata>();
 
         ParameterExpression inputParam = Expression.Parameter(typeof(object));
-        ParameterExpression helperParam = Expression.Parameter(typeof(object));
         ParameterExpression helperVariable = this.GetHelperVariable(binding);
 // ...
         value = this.GetObjectExpression(value);
 
         if (helperVariable != null)
         {
-            Expression typedParam = Expression.Convert(helperParam, helperVariable.Type);
+            object helperObject = binding?.Helper?.Object;
+
+            Expression helperConstant = Expression.Constant(helperObject, typeof(object));
+            Expression typedParam = Expression.Convert(helperConstant, helperVariable.Type);
             Expression assignHelper = Expression.Assign(helperVariable, typedParam);
 
             value = Expression.Block([helperVariable], assignHelper, value);
         }
 
-        BufferInternalConverter innerFunc = Expression.Lambda<BufferInternalConverter>(value, inputParam, helperParam).Compile();
-
-        object helperObject = binding?.Helper?.Object;
-
-        return value => innerFunc(value, helperObject);
+        return Expression.Lambda<BufferConverter>(value, inputParam).Compile();
     }
```
When `helperVariable` is null, the old `helperParam` was never referenced and the helper object was passed in but ignored, so dropping it changes nothing. This entry is independent of 3.3: that one removes the per-call closure in the cache lookup, and this one removes the per-call delegate hop inside the cached converter.

---

## 4. Mvc

These changes cover locating, rendering and buffering in `src/Mvc/Jerrycurl.Mvc*`. Public and protected signatures stay the same, and output SQL, parameter names and parameter order do not change.

### 4.1 Cache the domain type per origin type in ProcLocator
**File:** [Jerrycurl.Mvc/ProcLocator.cs:103-121](src/Mvc/Jerrycurl.Mvc/ProcLocator.cs#L103-L121), [Jerrycurl.Mvc/Internal/ProcCache.cs:13](src/Mvc/Jerrycurl.Mvc/Internal/ProcCache.cs#L13) · **Runs:** per query · **Expected gain:** Medium · **Risk:** cache never invalidates

`GetDomainType` copies every type in the origin assembly to an array. It then filters that array once for each enclosing namespace. `Create` calls it on every query (through `ProcPage.Query` → `ExecuteAndGetResult` → `locator.Create(pageType)`), and so does every `FindDomain` call, which runs the scan before its own `DomainDescriptors` cache is checked. The method is private and non-virtual. It depends only on `originType`: the private `IsDomainType` check and `originType.Assembly`. Its result can therefore go in a static cache keyed by `Type`. `null` is cached as a valid "no domain" result. If the multiple-domains `InvalidOperationException` is thrown, `GetOrAdd` stores nothing, so the exception is thrown again on every call, as it is today. Caveat: types added later to a dynamic `AssemblyBuilder` are not seen after the first lookup. Like the existing `ProcCache` dictionaries, the cache roots `Type` keys.

**Before**
```csharp
private Type GetDomainType(Type originType)
{
    if (this.IsDomainType(originType))
        return originType;

    Type[] allTypes = [.. originType.Assembly.DefinedTypes.Select(ti => ti.AsType())];
    // ...
    return null;
}
```

**After**
```csharp
// ProcCache.cs
public static ConcurrentDictionary<Type, Type> DomainTypes { get; } = new ConcurrentDictionary<Type, Type>();

// ProcLocator.cs
private Type GetDomainType(Type originType)
    => ProcCache.DomainTypes.GetOrAdd(originType, static (t, self) => self.FindDomainType(t), this);

private Type FindDomainType(Type originType)
{
    if (this.IsDomainType(originType))
        return originType;

    Type[] allTypes = [.. originType.Assembly.DefinedTypes.Select(ti => ti.AsType())];
    // ... unchanged ...
    return null;
}
```

**Diff**
```diff
+    public static ConcurrentDictionary<Type, Type> DomainTypes { get; } = new ConcurrentDictionary<Type, Type>();
 ...
-    private Type GetDomainType(Type originType)
+    private Type GetDomainType(Type originType)
+        => ProcCache.DomainTypes.GetOrAdd(originType, static (t, self) => self.FindDomainType(t), this);
+
+    private Type FindDomainType(Type originType)
     {
         if (this.IsDomainType(originType))
             return originType;
```

### 4.2 Static lambdas in FindPage and FindDomain
**File:** [Jerrycurl.Mvc/ProcLocator.cs:38-57](src/Mvc/Jerrycurl.Mvc/ProcLocator.cs#L38-L57) · **Runs:** per query · **Expected gain:** Low · **Risk:** none

`Accessor` calls `FindPage` on every query, and `ProcRenderer.Partial` calls it on every partial. Both lambdas capture `this` and other locals, so the compiler allocates a closure and a delegate on every call, even when the cache already has the entry. The `GetOrAdd<TArg>` overload passes that state as an argument, so the delegate can be cached.

**Before**
```csharp
PageLocatorKey key = new PageLocatorKey(procName, originType);

return ProcCache.PageDescriptors.GetOrAdd(key, _ => this.CreateDescriptor(procName, originType));
// ...
return ProcCache.DomainDescriptors.GetOrAdd(originType, _ => new DomainDescriptor()
{
    DomainType = domainType,
    OriginType = originType,
});
```

**After**
```csharp
PageLocatorKey key = new PageLocatorKey(procName, originType);

return ProcCache.PageDescriptors.GetOrAdd(key, static (k, self) => self.CreateDescriptor(k.ProcName, k.OriginType), this);
// ...
return ProcCache.DomainDescriptors.GetOrAdd(originType, static (t, d) => new DomainDescriptor()
{
    DomainType = d,
    OriginType = t,
}, domainType);
```

**Diff**
```diff
-        return ProcCache.PageDescriptors.GetOrAdd(key, _ => this.CreateDescriptor(procName, originType));
+        return ProcCache.PageDescriptors.GetOrAdd(key, static (k, self) => self.CreateDescriptor(k.ProcName, k.OriginType), this);
 ...
-        return ProcCache.DomainDescriptors.GetOrAdd(originType, _ => new DomainDescriptor()
+        return ProcCache.DomainDescriptors.GetOrAdd(originType, static (t, d) => new DomainDescriptor()
         {
-            DomainType = domainType,
-            OriginType = originType,
-        });
+            DomainType = d,
+            OriginType = t,
+        }, domainType);
```

### 4.3 Ordinal suffix and prefix checks in ProcLocator
**File:** [Jerrycurl.Mvc/ProcLocator.cs:182-216](src/Mvc/Jerrycurl.Mvc/ProcLocator.cs#L182-L216) · **Runs:** one-time · **Expected gain:** Low · **Risk:** culture-ignorable characters

`EndsWith("Accessor")`, `EndsWith("Async")`, `EndsWith("_cssql")` and `StartsWith(prefix)` use the current culture, which is slower and depends on the locale. They run only when a page descriptor is created, which happens on a cache miss. The arguments are C# type names and proc paths compared with ASCII literals. Caveat: culture comparison ignores zero-width characters and ordinal comparison does not, which only matters for such characters in a name.

**Before**
```csharp
private bool IsAccessorType(Type type) => (typeof(Accessor).IsAssignableFrom(type) || type.Name.EndsWith("Accessor"));
// ...
    if (type.Name.EndsWith("Accessor") && type.Name.Length > "Accessor".Length)
// ...
    if (typeName.EndsWith("Async"))
// ...
    if (!typeName.EndsWith("_cssql"))
// ...
    if (!procName.StartsWith(prefix) || procName.Length <= prefix.Length)
```

**After**
```csharp
private bool IsAccessorType(Type type) => (typeof(Accessor).IsAssignableFrom(type) || type.Name.EndsWith("Accessor", StringComparison.Ordinal));
// ...
    if (type.Name.EndsWith("Accessor", StringComparison.Ordinal) && type.Name.Length > "Accessor".Length)
// ...
    if (typeName.EndsWith("Async", StringComparison.Ordinal))
// ...
    if (!typeName.EndsWith("_cssql", StringComparison.Ordinal))
// ...
    if (!procName.StartsWith(prefix, StringComparison.Ordinal) || procName.Length <= prefix.Length)
```

**Diff**
```diff
-    private bool IsAccessorType(Type type) => (typeof(Accessor).IsAssignableFrom(type) || type.Name.EndsWith("Accessor"));
+    private bool IsAccessorType(Type type) => (typeof(Accessor).IsAssignableFrom(type) || type.Name.EndsWith("Accessor", StringComparison.Ordinal));
 ...
-        if (type.Name.EndsWith("Accessor") && type.Name.Length > "Accessor".Length)
+        if (type.Name.EndsWith("Accessor", StringComparison.Ordinal) && type.Name.Length > "Accessor".Length)
 ...
-        if (typeName.EndsWith("Async"))
+        if (typeName.EndsWith("Async", StringComparison.Ordinal))
 ...
-        if (!typeName.EndsWith("_cssql"))
+        if (!typeName.EndsWith("_cssql", StringComparison.Ordinal))
 ...
-        if (!procName.StartsWith(prefix) || procName.Length <= prefix.Length)
+        if (!procName.StartsWith(prefix, StringComparison.Ordinal) || procName.Length <= prefix.Length)
```

### 4.4 ProcEngine per-call allocations (GetOrAdd lambdas, options lookup, execution array)
**File:** [Jerrycurl.Mvc/ProcEngine.cs:44-62](src/Mvc/Jerrycurl.Mvc/ProcEngine.cs#L44-L62), [ProcEngine.cs:155-165](src/Mvc/Jerrycurl.Mvc/ProcEngine.cs#L155-L165), [ProcEngine.cs:340-345](src/Mvc/Jerrycurl.Mvc/ProcEngine.cs#L340-L345) · **Runs:** per query · **Expected gain:** Low · **Risk:** none

`Proc`, `Page` and `Options` each allocate a capturing closure and a delegate before calling `GetOrAdd`, even when the entry is cached. `Page` runs once per partial render. Inside `procFactory`, `CreateContext` calls `this.Options(descriptor.DomainType)` again, which costs a second `ConcurrentDictionary` lookup and closure. That value is the same `Lazy` value already captured in `options`: `Options` is non-virtual and `ProcCache.Domains` is never cleared. The execution array is also built with LINQ (`Select` + spread). The change uses the argument-passing `GetOrAdd`, reuses `options`, and fills the array with a `for` loop. `CreateContext` is private and has no other callers, so it is removed.

**Before**
```csharp
return ProcCache.Procs.GetOrAdd(key, _ => this.CreateProcFactory(descriptor, args));
// ...
return ProcCache.Pages.GetOrAdd(pageType, _ => this.CreatePageFactory(pageType));
// ...
return ProcCache.Domains.GetOrAdd(domainType, _ => new Lazy<IDomainOptions>(() => this.GetConfiguredOptions(domainType))).Value;
// ...
    ProcContext context = this.CreateContext(descriptor);
    // ...
    PageExecutionContext[] execution = [.. template.Select(t => new PageExecutionContext() { Page = t })];
// ...
private ProcContext CreateContext(PageDescriptor descriptor)
{
    IDomainOptions options = this.Options(descriptor.DomainType);

    return new ProcContext(descriptor, options);
}
```

**After**
```csharp
return ProcCache.Procs.GetOrAdd(key, static (_, s) => s.self.CreateProcFactory(s.descriptor, s.args), (self: this, descriptor, args));
// ...
return ProcCache.Pages.GetOrAdd(pageType, static (t, self) => self.CreatePageFactory(t), this);
// ...
return ProcCache.Domains.GetOrAdd(domainType, static (t, self) => new Lazy<IDomainOptions>(() => self.GetConfiguredOptions(t)), this).Value;
// ...
    ProcContext context = new ProcContext(descriptor, options);
    // ...
    PageExecutionContext[] execution = new PageExecutionContext[template.Length];

    for (int i = 0; i < template.Length; i++)
        execution[i] = new PageExecutionContext() { Page = template[i] };
```

**Diff**
```diff
-        return ProcCache.Procs.GetOrAdd(key, _ => this.CreateProcFactory(descriptor, args));
+        return ProcCache.Procs.GetOrAdd(key, static (_, s) => s.self.CreateProcFactory(s.descriptor, s.args), (self: this, descriptor, args));
 ...
-        return ProcCache.Pages.GetOrAdd(pageType, _ => this.CreatePageFactory(pageType));
+        return ProcCache.Pages.GetOrAdd(pageType, static (t, self) => self.CreatePageFactory(t), this);
 ...
-        return ProcCache.Domains.GetOrAdd(domainType, _ => new Lazy<IDomainOptions>(() => this.GetConfiguredOptions(domainType))).Value;
+        return ProcCache.Domains.GetOrAdd(domainType, static (t, self) => new Lazy<IDomainOptions>(() => self.GetConfiguredOptions(t)), this).Value;
 ...
-            ProcContext context = this.CreateContext(descriptor);
+            ProcContext context = new ProcContext(descriptor, options);
 ...
-            PageExecutionContext[] execution = [.. template.Select(t => new PageExecutionContext() { Page = t })];
+            PageExecutionContext[] execution = new PageExecutionContext[template.Length];
+
+            for (int i = 0; i < template.Length; i++)
+                execution[i] = new PageExecutionContext() { Page = template[i] };
 ...
-    private ProcContext CreateContext(PageDescriptor descriptor)
-    {
-        IDomainOptions options = this.Options(descriptor.DomainType);
-
-        return new ProcContext(descriptor, options);
-    }
```

### 4.5 Cache compiled page constructors per page type
**File:** [Jerrycurl.Mvc/ProcEngine.cs:73](src/Mvc/Jerrycurl.Mvc/ProcEngine.cs#L73), [ProcEngine.cs:143](src/Mvc/Jerrycurl.Mvc/ProcEngine.cs#L143), [ProcEngine.cs:270-305](src/Mvc/Jerrycurl.Mvc/ProcEngine.cs#L270-L305) · **Runs:** one-time · **Expected gain:** Low · **Risk:** none

`CreatePageConstructor` compiles a new expression tree every time it is called. That happens once for each `(page, model type, result type)` proc key, for every template in the hierarchy, and once more for each `PageFactory`. A shared layout page is therefore compiled many times. The method is private and depends only on `pageType`, so its result can be cached in a static field. `GetInjectionProperties` returns a lazy `Where` sequence that is enumerated twice (`Any()`, then `Select`), so it runs `GetProperties` and `GetCustomAttribute` twice. The change materializes it once.

**Before**
```csharp
PageConstructor constructor = this.CreatePageConstructor(pageType);
// ...
PageConstructor[] factories = [.. template.Select(t => this.CreatePageConstructor(t.PageType)), .. new PageConstructor[1]];
// ...
IEnumerable<PropertyInfo> injections = this.GetInjectionProperties(pageType);

if (injections.Any())
{
```

**After**
```csharp
private static readonly ConcurrentDictionary<Type, PageConstructor> pageConstructors = new ConcurrentDictionary<Type, PageConstructor>();

private PageConstructor GetPageConstructor(Type pageType)
    => pageConstructors.GetOrAdd(pageType, static (t, self) => self.CreatePageConstructor(t), this);
// ...
PageConstructor constructor = this.GetPageConstructor(pageType);
// ...
PageConstructor[] factories = [.. template.Select(t => this.GetPageConstructor(t.PageType)), .. new PageConstructor[1]];
// ...
PropertyInfo[] injections = [.. this.GetInjectionProperties(pageType)];

if (injections.Length > 0)
{
```

**Diff**
```diff
+    private static readonly ConcurrentDictionary<Type, PageConstructor> pageConstructors = new ConcurrentDictionary<Type, PageConstructor>();
+
+    private PageConstructor GetPageConstructor(Type pageType)
+        => pageConstructors.GetOrAdd(pageType, static (t, self) => self.CreatePageConstructor(t), this);
 ...
-        PageConstructor constructor = this.CreatePageConstructor(pageType);
+        PageConstructor constructor = this.GetPageConstructor(pageType);
 ...
-        PageConstructor[] factories = [.. template.Select(t => this.CreatePageConstructor(t.PageType)), .. new PageConstructor[1]];
+        PageConstructor[] factories = [.. template.Select(t => this.GetPageConstructor(t.PageType)), .. new PageConstructor[1]];
 ...
-        IEnumerable<PropertyInfo> injections = this.GetInjectionProperties(pageType);
+        PropertyInfo[] injections = [.. this.GetInjectionProperties(pageType)];
 
-        if (injections.Any())
+        if (injections.Length > 0)
```

### 4.6 Single hash lookup per name in ProcLookup.FromKey
**File:** [Jerrycurl.Mvc/ProcLookup.cs:15-27](src/Mvc/Jerrycurl.Mvc/ProcLookup.cs#L15-L27) · **Runs:** per parameter · **Expected gain:** Medium · **Risk:** none

Every `Par()`, `Ali()` and `Var()` call goes through `FromKey`. On a miss, which is the normal case for a bulk insert, `FromKey` hashes the key twice in `nameMap` and the prefix twice in `prefixCount`. Hashing the key walks `ProjectionIdentity`, `MetadataIdentity` and `IField.GetHashCode()`. `CollectionsMarshal.GetValueRefOrAddDefault` does one lookup per map. When `!key.HasValue`, the key uses reference equality and is a new instance from `Custom`, so it can never be found again. The change skips `nameMap` for those keys and stops storing dead entries. Names are still assigned in the same order, so the output is identical. (`key.Prefix + prefixes` already compiles to `string.Concat(string, string)`, so the planned `string.Concat` rewrite would not change anything and is dropped.)

**Before**
```csharp
private string FromKey(ProcLookupKey key)
{
    if (key == null)
        throw new ArgumentNullException(nameof(key));

    if (this.nameMap.TryGetValue(key, out string alias))
        return alias;

    this.prefixCount.TryGetValue(key.Prefix, out int prefixes);
    this.prefixCount[key.Prefix] = prefixes + 1;

    return this.nameMap[key] = key.Prefix + prefixes;
}
```

**After**
```csharp
private string FromKey(ProcLookupKey key)
{
    if (key == null)
        throw new ArgumentNullException(nameof(key));

    if (!key.HasValue)
        return this.NextAlias(key.Prefix);

    ref string alias = ref CollectionsMarshal.GetValueRefOrAddDefault(this.nameMap, key, out bool exists);

    if (!exists)
        alias = this.NextAlias(key.Prefix);

    return alias;
}

private string NextAlias(string prefix)
{
    ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(this.prefixCount, prefix, out _);

    return prefix + count++;
}
```

**Diff**
```diff
     if (key == null)
         throw new ArgumentNullException(nameof(key));
 
-    if (this.nameMap.TryGetValue(key, out string alias))
-        return alias;
+    if (!key.HasValue)
+        return this.NextAlias(key.Prefix);
 
-    this.prefixCount.TryGetValue(key.Prefix, out int prefixes);
-    this.prefixCount[key.Prefix] = prefixes + 1;
+    ref string alias = ref CollectionsMarshal.GetValueRefOrAddDefault(this.nameMap, key, out bool exists);
 
-    return this.nameMap[key] = key.Prefix + prefixes;
+    if (!exists)
+        alias = this.NextAlias(key.Prefix);
+
+    return alias;
+}
+
+private string NextAlias(string prefix)
+{
+    ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(this.prefixCount, prefix, out _);
+
+    return prefix + count++;
 }
```

### 4.7 TryPeek in ProcExecutionStack.Push
**File:** [Jerrycurl.Mvc/ProcExecutionStack.cs:22-34](src/Mvc/Jerrycurl.Mvc/ProcExecutionStack.cs#L22-L34) · **Runs:** per render · **Expected gain:** Low · **Risk:** none

`Stack<T>` does not implement `IList<T>`, so `FirstOrDefault()` boxes the enumerator to read the top element. `TryPeek` returns the same element without allocating. `PageExecutionContext`'s properties are non-virtual, so reading them through the concrete type instead of `IPageExecutionContext` calls the same code.

**Before**
```csharp
IPageExecutionContext currentContext = this.stack.FirstOrDefault();
```

**After**
```csharp
this.stack.TryPeek(out PageExecutionContext currentContext);
```

**Diff**
```diff
-        IPageExecutionContext currentContext = this.stack.FirstOrDefault();
+        this.stack.TryPeek(out PageExecutionContext currentContext);
```

### 4.8 Read IsoDialect's virtual quote and prefix once
**File:** [Jerrycurl.Mvc/IsoDialect.cs:47-70](src/Mvc/Jerrycurl.Mvc/IsoDialect.cs#L47-L70) · **Runs:** per parameter · **Expected gain:** Low · **Risk:** none

`Identifier` reads the virtual `IdentifierQuote` three times on success. `Parameter` reads `ParameterPrefix` up to three times. Both then build the string from `char` and `char?` concatenation, which creates a temporary string for the character. The change reads each virtual once into a local, after the argument checks so the call order on error paths stays the same. It then uses span `string.Concat` with no temporary. On .NET 8+ single ASCII-character strings are cached, so the saving is mostly the repeated virtual calls. `Variable` gets the same treatment. `String` is left alone because its null-check bug is a section 5 fix.

**Before**
```csharp
    else if (identifier.Contains(this.IdentifierQuote))
        throw new ArgumentException($"Identifier cannot contain the {this.IdentifierQuote} character.", nameof(identifier));

    return this.IdentifierQuote + identifier + this.IdentifierQuote;
// ...
    if (this.ParameterPrefix == null || parameterName[0] == this.ParameterPrefix)
        return parameterName;

    return this.ParameterPrefix + parameterName;
```

**After**
```csharp
    char quote = this.IdentifierQuote;

    if (identifier.Contains(quote))
        throw new ArgumentException($"Identifier cannot contain the {quote} character.", nameof(identifier));

    return string.Concat(new ReadOnlySpan<char>(in quote), identifier, new ReadOnlySpan<char>(in quote));
// ...
    char? prefix = this.ParameterPrefix;

    if (prefix == null || parameterName[0] == prefix)
        return parameterName;

    char p = prefix.GetValueOrDefault();

    return string.Concat(new ReadOnlySpan<char>(in p), parameterName);
```

**Diff**
```diff
         else if (string.IsNullOrWhiteSpace(identifier))
             throw new ArgumentException("Identifier cannot be blank.", nameof(identifier));
-        else if (identifier.Contains(this.IdentifierQuote))
-            throw new ArgumentException($"Identifier cannot contain the {this.IdentifierQuote} character.", nameof(identifier));
 
-        return this.IdentifierQuote + identifier + this.IdentifierQuote;
+        char quote = this.IdentifierQuote;
+
+        if (identifier.Contains(quote))
+            throw new ArgumentException($"Identifier cannot contain the {quote} character.", nameof(identifier));
+
+        return string.Concat(new ReadOnlySpan<char>(in quote), identifier, new ReadOnlySpan<char>(in quote));
 ...
-        if (this.ParameterPrefix == null || parameterName[0] == this.ParameterPrefix)
+        char? prefix = this.ParameterPrefix;
+
+        if (prefix == null || parameterName[0] == prefix)
             return parameterName;
 
-        return this.ParameterPrefix + parameterName;
+        char p = prefix.GetValueOrDefault();
+
+        return string.Concat(new ReadOnlySpan<char>(in p), parameterName);
```

### 4.9 Static readonly separator strings
**File:** [Jerrycurl.Mvc/Sql/OperatorExtensions.cs:13-34](src/Mvc/Jerrycurl.Mvc/Sql/OperatorExtensions.cs#L13-L34), [Jerrycurl.Mvc/Sql/ParameterExtensions.cs:31-34](src/Mvc/Jerrycurl.Mvc/Sql/ParameterExtensions.cs#L31-L34) · **Runs:** per render · **Expected gain:** Low · **Risk:** none

`Environment.NewLine` is not a constant, so `IsEq()` and `Eq()` rebuild their separator string on every call. The change computes each string once.

**Before**
```csharp
Separator = Environment.NewLine + "AND" + Environment.NewLine,
// ...
Separator = "," + Environment.NewLine,
```

**After**
```csharp
// OperatorExtensions
internal static readonly string AndSeparator = Environment.NewLine + "AND" + Environment.NewLine;
internal static readonly string CommaSeparator = "," + Environment.NewLine;
// ...
Separator = AndSeparator,
// ...
Separator = CommaSeparator,

// ParameterExtensions.IsEq
Separator = OperatorExtensions.AndSeparator,
```

**Diff**
```diff
+    internal static readonly string AndSeparator = Environment.NewLine + "AND" + Environment.NewLine;
+    internal static readonly string CommaSeparator = "," + Environment.NewLine;
 ...
-            Separator = Environment.NewLine + "AND" + Environment.NewLine,
+            Separator = AndSeparator,
 ...
-            Separator = "," + Environment.NewLine,
+            Separator = CommaSeparator,
 ...
 // ParameterExtensions.cs
-            Separator = Environment.NewLine + "AND" + Environment.NewLine,
+            Separator = OperatorExtensions.AndSeparator,
```

### 4.10 Loop over table name parts in TblName
**File:** [Jerrycurl.Mvc/Sql/TableExtensions.cs:16-24](src/Mvc/Jerrycurl.Mvc/Sql/TableExtensions.cs#L16-L24) · **Runs:** per render · **Expected gain:** Low · **Risk:** none

Today the code reads `Context.Domain.Dialect` twice, allocates a method-group delegate and a `Select` iterator, and calls `string.Join(string, IEnumerable<string>)`. The change reads the dialect into a local once and fills a `string[]` by calling `dialect.Identifier` for each part, in the same order. It keeps `string.Join`, so null or empty results from a custom dialect are handled exactly as before.

**Before**
```csharp
ITableMetadata table = ProjectionHelper.GetTableMetadata(attribute);

string qualifier = attribute.Context.Domain.Dialect.Qualifier;
string tableName = string.Join(qualifier, table.TableName.Select(attribute.Context.Domain.Dialect.Identifier));

return attribute.Append(tableName);
```

**After**
```csharp
ITableMetadata table = ProjectionHelper.GetTableMetadata(attribute);

IDialect dialect = attribute.Context.Domain.Dialect;
string qualifier = dialect.Qualifier;
IReadOnlyList<string> parts = table.TableName;
string[] quoted = new string[parts.Count];

for (int i = 0; i < quoted.Length; i++)
    quoted[i] = dialect.Identifier(parts[i]);

return attribute.Append(string.Join(qualifier, quoted));
```

**Diff**
```diff
     ITableMetadata table = ProjectionHelper.GetTableMetadata(attribute);
 
-    string qualifier = attribute.Context.Domain.Dialect.Qualifier;
-    string tableName = string.Join(qualifier, table.TableName.Select(attribute.Context.Domain.Dialect.Identifier));
+    IDialect dialect = attribute.Context.Domain.Dialect;
+    string qualifier = dialect.Qualifier;
+    IReadOnlyList<string> parts = table.TableName;
+    string[] quoted = new string[parts.Count];
 
-    return attribute.Append(tableName);
+    for (int i = 0; i < quoted.Length; i++)
+        quoted[i] = dialect.Identifier(parts[i]);
+
+    return attribute.Append(string.Join(qualifier, quoted));
```

### 4.11 Copy parameters and bindings once in SqlBuffer.Read and ProcBuffer
**File:** [Jerrycurl.Mvc/SqlBuffer.cs:137-193](src/Mvc/Jerrycurl.Mvc/SqlBuffer.cs#L137-L193), [Jerrycurl.Mvc/ProcBuffer.cs:9-33](src/Mvc/Jerrycurl.Mvc/ProcBuffer.cs#L9-L33) · **Runs:** per query · **Expected gain:** Medium · **Risk:** batch slices become eager snapshots

Today the parameter and binding lists are copied twice. `SqlBuffer.Read` copies them (`[.. buffer.Parameters]`, or a lazy `Skip/Take` for split batches), and then `ProcBuffer` copies them again into `Command`/`Query` (`[.. content.Parameters]`). For a 10k-parameter bulk insert that is two full list copies, plus a `Skip` walk per batch. After the change, `Read` always produces a fresh `List<T>`, using `new List<T>(list)` or `GetRange` (an array copy, with no `Skip` walk). `ProcBuffer` then hands that list to `Command`/`Query` directly. `SqlBuffer.Read` is public but non-virtual, so `ProcBuffer` always gets these fresh lists. The `??` fallback covers any other enumerable. Caveats: split batches now snapshot the list at yield time instead of enumerating lazily, which matters only if a caller keeps appending to the buffer while it holds a yielded content. Offsets only grow, so `GetRange` counts are never negative.

**Before**
```csharp
// SqlBuffer.Read
    Bindings = [.. buffer.Bindings],
    Parameters = [.. buffer.Parameters],
// ...
    Bindings = buffer.Bindings.Skip(yieldedBindings).Take(offset.NumberOfBindings - yieldedBindings),
    Parameters = buffer.Parameters.Skip(yieldedParams).Take(offset.NumberOfParams - yieldedParams),
// ...
    Bindings = buffer.Bindings.Skip(yieldedBindings),
    Parameters = buffer.Parameters.Skip(yieldedParams),

// ProcBuffer
    Parameters = [.. content.Parameters],
    Bindings = [.. content.Bindings],
// ...
    Parameters = [.. content.Parameters],
```

**After**
```csharp
// SqlBuffer.Read
    Bindings = new List<IUpdateBinding>(buffer.Bindings),
    Parameters = new List<IParameter>(buffer.Parameters),
// ...
    Bindings = buffer.Bindings.GetRange(yieldedBindings, offset.NumberOfBindings - yieldedBindings),
    Parameters = buffer.Parameters.GetRange(yieldedParams, offset.NumberOfParams - yieldedParams),
// ...
    Bindings = buffer.Bindings.GetRange(yieldedBindings, buffer.Bindings.Count - yieldedBindings),
    Parameters = buffer.Parameters.GetRange(yieldedParams, buffer.Parameters.Count - yieldedParams),

// ProcBuffer
    Parameters = content.Parameters as List<IParameter> ?? [.. content.Parameters],
    Bindings = content.Bindings as List<IUpdateBinding> ?? [.. content.Bindings],
// ...
    Parameters = content.Parameters as List<IParameter> ?? [.. content.Parameters],
```

**Diff**
```diff
-                Bindings = [.. buffer.Bindings],
-                Parameters = [.. buffer.Parameters],
+                Bindings = new List<IUpdateBinding>(buffer.Bindings),
+                Parameters = new List<IParameter>(buffer.Parameters),
 ...
-                    Bindings = buffer.Bindings.Skip(yieldedBindings).Take(offset.NumberOfBindings - yieldedBindings),
-                    Parameters = buffer.Parameters.Skip(yieldedParams).Take(offset.NumberOfParams - yieldedParams),
+                    Bindings = buffer.Bindings.GetRange(yieldedBindings, offset.NumberOfBindings - yieldedBindings),
+                    Parameters = buffer.Parameters.GetRange(yieldedParams, offset.NumberOfParams - yieldedParams),
 ...
-                    Bindings = buffer.Bindings.Skip(yieldedBindings),
-                    Parameters = buffer.Parameters.Skip(yieldedParams),
+                    Bindings = buffer.Bindings.GetRange(yieldedBindings, buffer.Bindings.Count - yieldedBindings),
+                    Parameters = buffer.Parameters.GetRange(yieldedParams, buffer.Parameters.Count - yieldedParams),
 ...
 // ProcBuffer.cs
-                Parameters = [.. content.Parameters],
-                Bindings = [.. content.Bindings],
+                Parameters = content.Parameters as List<IParameter> ?? [.. content.Parameters],
+                Bindings = content.Bindings as List<IUpdateBinding> ?? [.. content.Bindings],
 ...
-                Parameters = [.. content.Parameters],
+                Parameters = content.Parameters as List<IParameter> ?? [.. content.Parameters],
```

### 4.12 SqlBuffer.ReserveBuffer and ReadToEnd sizing
**File:** [Jerrycurl.Mvc/SqlBuffer.cs:53-61](src/Mvc/Jerrycurl.Mvc/SqlBuffer.cs#L53-L61), [SqlBuffer.cs:109-128](src/Mvc/Jerrycurl.Mvc/SqlBuffer.cs#L109-L128) · **Runs:** per query · **Expected gain:** Low · **Risk:** none

`ReserveBuffer` pads the list with nulls through `Enumerable.Range(...).Select(...)` and `AddRange`. `CollectionsMarshal.SetCount` grows the count directly. The new slots are null because `innerBuffers` never removes items, so slots past `Count` have never been written. `ReadToEnd` grows its lists and `StringBuilder` from empty. Summing the sizes first lets it allocate them once.

**Before**
```csharp
if (batchIndex >= this.innerBuffers.Count)
{
    IEnumerable<IndexedBuffer> defaults = Enumerable.Range(0, batchIndex + 1 - this.innerBuffers.Count).Select(_ => (IndexedBuffer)null);

    this.innerBuffers.AddRange(defaults);
}
// ...
List<IParameter> parameters = [];
List<IUpdateBinding> bindings = [];
StringBuilder text = new StringBuilder();
```

**After**
```csharp
if (batchIndex >= this.innerBuffers.Count)
    CollectionsMarshal.SetCount(this.innerBuffers, batchIndex + 1);
// ...
int paramCount = 0, bindingCount = 0, textLength = 0;

foreach (IndexedBuffer buffer in this.innerBuffers.NotNull())
{
    paramCount += buffer.Parameters.Count;
    bindingCount += buffer.Bindings.Count;
    textLength += buffer.Text.Length;
}

List<IParameter> parameters = new List<IParameter>(paramCount);
List<IUpdateBinding> bindings = new List<IUpdateBinding>(bindingCount);
StringBuilder text = new StringBuilder(textLength);
```

**Diff**
```diff
         if (batchIndex >= this.innerBuffers.Count)
-        {
-            IEnumerable<IndexedBuffer> defaults = Enumerable.Range(0, batchIndex + 1 - this.innerBuffers.Count).Select(_ => (IndexedBuffer)null);
-
-            this.innerBuffers.AddRange(defaults);
-        }
+            CollectionsMarshal.SetCount(this.innerBuffers, batchIndex + 1);
 ...
-        List<IParameter> parameters = [];
-        List<IUpdateBinding> bindings = [];
-        StringBuilder text = new StringBuilder();
+        int paramCount = 0, bindingCount = 0, textLength = 0;
+
+        foreach (IndexedBuffer buffer in this.innerBuffers.NotNull())
+        {
+            paramCount += buffer.Parameters.Count;
+            bindingCount += buffer.Bindings.Count;
+            textLength += buffer.Text.Length;
+        }
+
+        List<IParameter> parameters = new List<IParameter>(paramCount);
+        List<IUpdateBinding> bindings = new List<IUpdateBinding>(bindingCount);
+        StringBuilder text = new StringBuilder(textLength);
```

### 4.13 Build the attribute directly in ProcPage.Write<T>
**File:** [Jerrycurl.Mvc/ProcPage.cs:35-49](src/Mvc/Jerrycurl.Mvc/ProcPage.cs#L35-L49) · **Runs:** per parameter · **Expected gain:** Low · **Risk:** none

Each `@value` that is not an `ISqlWritable` creates a full `Projection` and its default-header iterator, which is never enumerated. `projection.Par()` then only does `new ProjectionAttribute(projection).Par()`. Constructing the attribute with the same identity, context, metadata and data (`ProjectionData.Resolve(ProjectionData.Resolve(identity), metadata)`, which is what the `Projection` and attribute constructors compute) gives an identical attribute without those two allocations. Null checks still throw `ProjectionException.ArgumentNull` with the same argument names. The planned per-schema `IProjectionMetadata` cache is dropped (see the note at the end of this section).

**Before**
```csharp
IField model = this.Context.Domain.Schemas.From(value);

ProjectionIdentity identity = new ProjectionIdentity(model);
IProjectionMetadata metadata = model.Identity.Schema.Require<IProjectionMetadata>();
Projection projection = new Projection(identity, this.Context, metadata);

this.Write(projection.Par());
```

**After**
```csharp
IField model = this.Context.Domain.Schemas.From(value);

ProjectionIdentity identity = new ProjectionIdentity(model);
IProjectionMetadata metadata = model.Identity.Schema.Require<IProjectionMetadata>();
IProjectionData data = ProjectionData.Resolve(ProjectionData.Resolve(identity), metadata);
ProjectionAttribute attribute = new ProjectionAttribute(identity, this.Context, metadata, data);

this.Write(attribute.Par());
```

**Diff**
```diff
             ProjectionIdentity identity = new ProjectionIdentity(model);
             IProjectionMetadata metadata = model.Identity.Schema.Require<IProjectionMetadata>();
-            Projection projection = new Projection(identity, this.Context, metadata);
+            IProjectionData data = ProjectionData.Resolve(ProjectionData.Resolve(identity), metadata);
+            ProjectionAttribute attribute = new ProjectionAttribute(identity, this.Context, metadata, data);
 
-            this.Write(projection.Par());
+            this.Write(attribute.Par());
```

### 4.14 Read the execution buffer once per ProjectionValues enumeration
**File:** [Jerrycurl.Mvc/Projections/ProjectionValues.cs:25-42](src/Mvc/Jerrycurl.Mvc/Projections/ProjectionValues.cs#L25-L42) · **Runs:** per item · **Expected gain:** Low · **Risk:** low (unbalanced stack only)

Today every item resolves `Context.Execution.Buffer` again: an interface call, `Stack.Current` with its empty check and `Peek`, then `.Buffer`. Within one `ProcContext`, `Buffer` is set only on `execution[0]` and every later `Push` inherits it. `ProcRenderer.Partial` and `Body` push and pop in matched pairs, and user code cannot reach `ProcContext.Stack`. The buffer is therefore the same object for the whole enumeration, so reading it once into a local is equivalent. It also guarantees that `Push` and `Pop` hit the same buffer even after an exception has left the stack unbalanced.

**Before**
```csharp
this.Context.Execution.Buffer.Push(this.BatchIndex);

try
{
    foreach (IProjection<TItem> item in this.Items)
    {
        yield return item;

        this.Context.Execution.Buffer.Mark();
    }
}
finally
{
    this.Context.Execution.Buffer.Pop();
}
```

**After**
```csharp
ISqlBuffer buffer = this.Context.Execution.Buffer;

buffer.Push(this.BatchIndex);

try
{
    foreach (IProjection<TItem> item in this.Items)
    {
        yield return item;

        buffer.Mark();
    }
}
finally
{
    buffer.Pop();
}
```

**Diff**
```diff
-        this.Context.Execution.Buffer.Push(this.BatchIndex);
+        ISqlBuffer buffer = this.Context.Execution.Buffer;
+
+        buffer.Push(this.BatchIndex);
 
         try
         {
             foreach (IProjection<TItem> item in this.Items)
             {
                 yield return item;
 
-                this.Context.Execution.Buffer.Mark();
+                buffer.Mark();
             }
         }
         finally
         {
-            this.Context.Execution.Buffer.Pop();
+            buffer.Pop();
         }
```

### 4.15 Deep tier: index-filled row data and an array row header in Vals
**File:** [Jerrycurl.Mvc/Sql/ValueExtensions.cs:28-44](src/Mvc/Jerrycurl.Mvc/Sql/ValueExtensions.cs#L28-L44), [Jerrycurl.Mvc/Projections/ProjectionReader.cs:34-40](src/Mvc/Jerrycurl.Mvc/Projections/ProjectionReader.cs#L34-L40) · **Runs:** per item · **Expected gain:** High · **Risk:** row header attributes built eagerly

For each row, the code spreads the `GetData()` iterator into a new array and then builds a lazy `Zip(Skip(1)).Select(...)` chain for the row header. Every enumeration of that row's `Header` (`Pars()`, `ColNames()` and so on) runs the zip again and creates new attributes. The change adds an internal `ProjectionReader.CopyData(IProjectionData[])`, which makes the same `ITuple` indexer calls in the same order. Vals reuses one data array across rows, which is safe because no element outlives the row. The row header becomes an array built once per row. This is safe because `ProjectionAttribute.With(data:)` has no side effects: with non-null `data` and no `metadata`, it skips `ProjectionData.Resolve` and only allocates a new attribute from the parent's fields. Only the local row header is materialized. `projection.Header` is still enumerated exactly once, when the column metadata is built, so user `Map` side effects and `ProcLookup` naming order are unchanged. Caveats: the row header is built even when the template only uses `For(expr)` on the row (N small allocations that were lazy before). The same attribute instances are returned on every header enumeration; `ProjectionAttribute` has no identity-based semantics.

**Before**
```csharp
using ProjectionReader reader = new ProjectionReader(projection.Data.Source, header);

while (reader.Read())
{
    IProjectionData[] dataSet = [.. reader.GetData()];

    if (dataSet[0].Source.Snapshot != null)
    {
        IEnumerable<IProjectionAttribute> valueHeader = attributes.Zip(dataSet.Skip(1)).Select(t => t.First.With(data: t.Second));

        yield return projection.With(data: dataSet[0], header: valueHeader);
    }
}
```

**After**
```csharp
using ProjectionReader reader = new ProjectionReader(projection.Data.Source, header);

IProjectionData[] dataSet = new IProjectionData[header.Length];

while (reader.Read())
{
    reader.CopyData(dataSet);

    if (dataSet[0].Source.Snapshot != null)
    {
        IProjectionAttribute[] valueHeader = new IProjectionAttribute[attributes.Length];

        for (int i = 0; i < attributes.Length; i++)
            valueHeader[i] = attributes[i].With(data: dataSet[i + 1]);

        yield return projection.With(data: dataSet[0], header: valueHeader);
    }
}

// ProjectionReader (internal class)
public void CopyData(IProjectionData[] target)
{
    ITuple data = this.innerReader;

    for (int i = 0, j = 0; i < this.indexHeader.Count; i += 3, j++)
        target[j] = new ProjectionData(data[this.indexHeader[i]], data[this.indexHeader[i + 1]], data[this.indexHeader[i + 2]]);
}
```

**Diff**
```diff
             using ProjectionReader reader = new ProjectionReader(projection.Data.Source, header);
 
+            IProjectionData[] dataSet = new IProjectionData[header.Length];
+
             while (reader.Read())
             {
-                IProjectionData[] dataSet = [.. reader.GetData()];
+                reader.CopyData(dataSet);
 
                 if (dataSet[0].Source.Snapshot != null)
                 {
-                    IEnumerable<IProjectionAttribute> valueHeader = attributes.Zip(dataSet.Skip(1)).Select(t => t.First.With(data: t.Second));
+                    IProjectionAttribute[] valueHeader = new IProjectionAttribute[attributes.Length];
+
+                    for (int i = 0; i < attributes.Length; i++)
+                        valueHeader[i] = attributes[i].With(data: dataSet[i + 1]);
 
                     yield return projection.With(data: dataSet[0], header: valueHeader);
                 }
             }
 ...
 // ProjectionReader.cs
+    public void CopyData(IProjectionData[] target)
+    {
+        ITuple data = this.innerReader;
+
+        for (int i = 0, j = 0; i < this.indexHeader.Count; i += 3, j++)
+            target[j] = new ProjectionData(data[this.indexHeader[i]], data[this.indexHeader[i + 1]], data[this.indexHeader[i + 2]]);
+    }
```

### 4.16 Deep tier: lazy append chain in SqlContent (fixes quadratic ParList/LitList)
**File:** [Jerrycurl.Mvc.Abstractions/SqlContent.cs:9-78](src/Mvc/Jerrycurl.Mvc.Abstractions/SqlContent.cs#L9-L78) · **Runs:** per item · **Expected gain:** High · **Risk:** subtle; needs ParList/IN-list tests

`ValList` (behind `ParList` and `LitList`, [ValueExtensions.cs:59-86](src/Mvc/Jerrycurl.Mvc/Sql/ValueExtensions.cs#L59-L86)) appends `", "`, the parameter name and the `Parameter` once per item to the same accumulating attribute. The user's `itemFactory` receives that accumulated attribute, so the call shape stays the same. Each `SqlContent.Append(string)` copies the whole text so far (`this.Text + text`), so an N-item list copies O(N²) characters. Each `Append(parameters)` wraps the previous sequence in another `Concat`, and enumerating N chained `Concat` sources walks the chain back for each source, which is also O(N²). The fix is inside `SqlContent`. An appended content keeps a reference to its parent plus one suffix. `Text` is built once with a pre-sized `StringBuilder` and cached. It walks up only as far as the nearest ancestor whose text is already built. `Parameters` and `Bindings` return a chain enumerable that collects the suffixes on each enumeration and yields base-then-suffixes in append order, so the total is O(N). The public and `ISqlContent` surface (properties, `internal set`, `Append` overloads, `WriteTo`, `ToString`, `Empty`) is unchanged.

Behavior notes:
- **Parameters and bindings are not snapshotted.** They are deliberately not flattened into a cached list, which differs from the original plan. Every enumeration re-enumerates the user-supplied sequences lazily, in the same order, exactly like the old `Concat` chain. Only `Text` is cached, and strings are immutable.
- **Internal setters.** `SqlBuffer` only uses the setters in object initializers on new instances. A set value acts as the chain base for that node: the walk stops at the first node whose field is non-null. A root whose `Parameters` was set to `null` used to throw on `Append(parameters)` (because `Concat(null, …)` throws) and now yields nothing; no code in the repo sets `null`.
- **Thread safety.** The caches (`text`, `parameterChain`, `bindingChain`) are filled without locks. The races are benign: every thread computes an equal value and publishes it with a single reference write. The chain enumerable holds no mutable state.
- **Memory and recursion.** An appended content keeps its parent chain alive, about 7 references per node and linear in the number of appends. Before, each node held its own copy of the full text, which was quadratic in total. All walks are loops, so long lists cannot overflow the stack.

**Before**
```csharp
public class SqlContent : ISqlContent
{
    public IEnumerable<IUpdateBinding> Bindings { get; internal set; } = [];
    public IEnumerable<IParameter> Parameters { get; internal set; } = [];
    public string Text { get; internal set; } = "";

    public static SqlContent Empty { get; } = new SqlContent();

    public SqlContent()
    {

    }

    public SqlContent Append(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        return new SqlContent()
        {
            Text = this.Text + text,
            Bindings = this.Bindings,
            Parameters = this.Parameters,
        };
    }

    public SqlContent Append(IEnumerable<IParameter> parameters)
    {
        // ...
        return new SqlContent()
        {
            Text = this.Text,
            Bindings = this.Bindings,
            Parameters = this.Parameters.Concat(parameters),
        };
    }

    public SqlContent Append(IEnumerable<IUpdateBinding> bindings)
    {
        // ...
        return new SqlContent()
        {
            Text = this.Text,
            Bindings = this.Bindings.Concat(bindings),
            Parameters = this.Parameters,
        };
    }
    // ... params overloads, ToString, WriteTo, explicit ISqlContent members unchanged
}
```

**After**
```csharp
public class SqlContent : ISqlContent
{
    // Append chain: non-null for appended content, which carries exactly one suffix.
    private readonly SqlContent parent;
    private readonly string textSuffix;
    private readonly IEnumerable<IParameter> parameterSuffix;
    private readonly IEnumerable<IUpdateBinding> bindingSuffix;

    // Base values (root content or internal setter); 'text' is also the lazy cache for appended content.
    private string text;
    private IEnumerable<IParameter> parameters;
    private IEnumerable<IUpdateBinding> bindings;

    private ChainEnumerable<IParameter> parameterChain;
    private ChainEnumerable<IUpdateBinding> bindingChain;

    public IEnumerable<IUpdateBinding> Bindings
    {
        get => this.bindings ?? (this.parent == null ? null : this.bindingChain ??= new ChainEnumerable<IUpdateBinding>(this, static n => n.bindingSuffix, static n => n.bindings));
        internal set => this.bindings = value;
    }

    public IEnumerable<IParameter> Parameters
    {
        get => this.parameters ?? (this.parent == null ? null : this.parameterChain ??= new ChainEnumerable<IParameter>(this, static n => n.parameterSuffix, static n => n.parameters));
        internal set => this.parameters = value;
    }

    public string Text
    {
        get => this.text ??= this.BuildText();
        internal set => this.text = value;
    }

    public static SqlContent Empty { get; } = new SqlContent();

    public SqlContent()
    {
        this.text = "";
        this.parameters = [];
        this.bindings = [];
    }

    private SqlContent(SqlContent parent, string text, IEnumerable<IParameter> parameters, IEnumerable<IUpdateBinding> bindings)
    {
        this.parent = parent;
        this.textSuffix = text;
        this.parameterSuffix = parameters;
        this.bindingSuffix = bindings;
    }

    public SqlContent Append(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        return new SqlContent(this, text, null, null);
    }

    public SqlContent Append(IEnumerable<IParameter> parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        return new SqlContent(this, null, parameters, null);
    }

    public SqlContent Append(IEnumerable<IUpdateBinding> bindings)
    {
        if (bindings == null)
            throw new ArgumentNullException(nameof(bindings));

        return new SqlContent(this, null, null, bindings);
    }

    // params overloads, ToString, WriteTo, explicit ISqlContent members unchanged

    private string BuildText()
    {
        List<string> suffixes = null;
        int length = 0;
        SqlContent node = this;

        for (; node.parent != null && node.text == null; node = node.parent)
        {
            if (node.textSuffix != null)
            {
                (suffixes ??= []).Add(node.textSuffix);
                length += node.textSuffix.Length;
            }
        }

        string baseText = node.text;

        if (suffixes == null)
            return baseText;

        StringBuilder builder = new StringBuilder((baseText?.Length ?? 0) + length);

        builder.Append(baseText);

        for (int i = suffixes.Count - 1; i >= 0; i--)
            builder.Append(suffixes[i]);

        return builder.ToString();
    }

    private sealed class ChainEnumerable<T>(SqlContent content, Func<SqlContent, IEnumerable<T>> suffix, Func<SqlContent, IEnumerable<T>> root) : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            List<IEnumerable<T>> parts = [];
            SqlContent node = content;

            for (; root(node) == null && node.parent != null; node = node.parent)
            {
                if (suffix(node) is IEnumerable<T> part)
                    parts.Add(part);
            }

            if (root(node) is IEnumerable<T> baseItems)
                parts.Add(baseItems);

            for (int i = parts.Count - 1; i >= 0; i--)
            {
                foreach (T item in parts[i])
                    yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
```

**Diff**
```diff
+using System.Collections;
+using System.Text;
 ...
 public class SqlContent : ISqlContent
 {
-    public IEnumerable<IUpdateBinding> Bindings { get; internal set; } = [];
-    public IEnumerable<IParameter> Parameters { get; internal set; } = [];
-    public string Text { get; internal set; } = "";
+    private readonly SqlContent parent;
+    private readonly string textSuffix;
+    private readonly IEnumerable<IParameter> parameterSuffix;
+    private readonly IEnumerable<IUpdateBinding> bindingSuffix;
+
+    private string text;
+    private IEnumerable<IParameter> parameters;
+    private IEnumerable<IUpdateBinding> bindings;
+
+    private ChainEnumerable<IParameter> parameterChain;
+    private ChainEnumerable<IUpdateBinding> bindingChain;
+
+    public IEnumerable<IUpdateBinding> Bindings
+    {
+        get => this.bindings ?? (this.parent == null ? null : this.bindingChain ??= new ChainEnumerable<IUpdateBinding>(this, static n => n.bindingSuffix, static n => n.bindings));
+        internal set => this.bindings = value;
+    }
+
+    public IEnumerable<IParameter> Parameters
+    {
+        get => this.parameters ?? (this.parent == null ? null : this.parameterChain ??= new ChainEnumerable<IParameter>(this, static n => n.parameterSuffix, static n => n.parameters));
+        internal set => this.parameters = value;
+    }
+
+    public string Text
+    {
+        get => this.text ??= this.BuildText();
+        internal set => this.text = value;
+    }
 
     public static SqlContent Empty { get; } = new SqlContent();
 
     public SqlContent()
     {
-
+        this.text = "";
+        this.parameters = [];
+        this.bindings = [];
     }
 
+    private SqlContent(SqlContent parent, string text, IEnumerable<IParameter> parameters, IEnumerable<IUpdateBinding> bindings)
+    {
+        this.parent = parent;
+        this.textSuffix = text;
+        this.parameterSuffix = parameters;
+        this.bindingSuffix = bindings;
+    }
+
     public SqlContent Append(string text)
     {
         if (text == null)
             throw new ArgumentNullException(nameof(text));
 
-        return new SqlContent()
-        {
-            Text = this.Text + text,
-            Bindings = this.Bindings,
-            Parameters = this.Parameters,
-        };
+        return new SqlContent(this, text, null, null);
     }
 
     public SqlContent Append(IEnumerable<IParameter> parameters)
     {
         if (parameters == null)
             throw new ArgumentNullException(nameof(parameters));
 
-        return new SqlContent()
-        {
-            Text = this.Text,
-            Bindings = this.Bindings,
-            Parameters = this.Parameters.Concat(parameters),
-        };
+        return new SqlContent(this, null, parameters, null);
     }
 
     public SqlContent Append(IEnumerable<IUpdateBinding> bindings)
     {
         if (bindings == null)
             throw new ArgumentNullException(nameof(bindings));
 
-        return new SqlContent()
-        {
-            Text = this.Text,
-            Bindings = this.Bindings.Concat(bindings),
-            Parameters = this.Parameters,
-        };
+        return new SqlContent(this, null, null, bindings);
     }
 ...
     ISqlContent ISqlContent.Append(params IUpdateBinding[] bindings) => this.Append(bindings);
+
+    private string BuildText()
+    {
+        // ... as in After ...
+    }
+
+    private sealed class ChainEnumerable<T>(SqlContent content, Func<SqlContent, IEnumerable<T>> suffix, Func<SqlContent, IEnumerable<T>> root) : IEnumerable<T>
+    {
+        // ... as in After ...
+    }
 }
```

**Dropped from the original plan for this section:**
- *ProcPage.Write<T> per-schema `IProjectionMetadata` cache.* The schema comes from `Schemas.From<T>(value)`, which is `GetSchema(typeof(T))`, the static type rather than `value.GetType()`, and each domain has its own `ISchemaStore`. A correct key would therefore have to be the `ISchema` instance, never a `Type`. Even with that key, caching would skip the interface call `ISchema.Require<IProjectionMetadata>()`, which breaks the rule that interface members must still be called. The built-in `Schema` is internal to Relations, so Mvc cannot limit the cache to it. With section 2's lock-free `Lookup` fast path, `Require` is already a single dictionary read.
- *`ProcLookup` prefix `string.Concat`.* `key.Prefix + prefixes` already compiles to `string.Concat(string, string)`, so the rewrite would change nothing.
- *`ProjectionHelper.GetJsonMetadata`.* It needs no Mvc code change; it benefits from section 2's `Lookup` fast path automatically.

---

## 5. Bug fixes (change behavior, so each gets its own commit)

These are real bugs found during the sweep. Each fix changes behavior, so each one gets its own commit and a regression test in the matching test project.

### 5.1 `Equality.CombineAll(IEnumerable, IEnumerable)`: inverted equality check
**File:** [Equality.cs:48-64](src/Shared/Jerrycurl.Common/Diagnostics/Equality.cs#L48-L64) · **Runs:** never today (latent) · **Expected gain:** none (correctness) · **Risk:** none, no current callers

This overload returns `false` when two elements are *equal*, so any two non-empty sequences compare as unequal. No caller reaches it today: `Tuple`, `RelationHeader` and `CommandCacheKey` all resolve to the `IReadOnlyList` overload. The fix keeps it from causing trouble for future callers. **If you apply 1.2, this one is moot:** the whole file is deleted.

**Before**
```csharp
foreach (var (l, r) in left.ZipAll(right))
{
    if (l == null ^ r == null)
        return false;
    else if (l != null && l.Equals(r))
        return false;
}
```

**After**
```csharp
foreach (var (l, r) in left.ZipAll(right))
{
    if (l == null ^ r == null)
        return false;
    else if (l != null && !l.Equals(r))
        return false;
}
```

**Diff**
```diff
 foreach (var (l, r) in left.ZipAll(right))
 {
     if (l == null ^ r == null)
         return false;
-    else if (l != null && l.Equals(r))
+    else if (l != null && !l.Equals(r))
         return false;
 }
```

### 5.2 `Equality.AddRange`: lists of different lengths compare equal
**File:** [Equality.cs:27-44](src/Shared/Jerrycurl.Common/Diagnostics/Equality.cs#L27-L44) · **Runs:** per query-cache and buffer-cache hit · **Expected gain:** none (correctness) · **Risk:** low; makes cache keys stricter

`Zip` stops at the shorter list. A `QueryCacheKey` whose column header is a prefix of another key's header therefore compares as equal. The dictionary only calls `Equals` after the hashes match, so this only matters when the hashes also collide. When they do, the wrong compiled reader is used for the result set. The fix adds a count check before comparing elements. **1.2 includes this fix** through `ListEquals`. If you pick both, 5.2 is committed first on its own, and 1.2 then carries it forward.

**Before**
```csharp
if (left != null && right != null)
{
    foreach (var (l, r) in left.Zip(right))
    {
        // ...
```

**After**
```csharp
if (left != null && right != null)
{
    if (left.Count != right.Count)
    {
        this.equals = false;
        return;
    }

    foreach (var (l, r) in left.Zip(right))
    {
        // ...
```

**Diff**
```diff
 if (left != null && right != null)
 {
+    if (left.Count != right.Count)
+    {
+        this.equals = false;
+        return;
+    }
+
     foreach (var (l, r) in left.Zip(right))
     {
         // ...
```

### 5.3 `ProjectionAttribute.With`: inverted metadata condition
**File:** [ProjectionAttribute.cs:54-61](src/Mvc/Jerrycurl.Mvc/Projections/ProjectionAttribute.cs#L54-L61) · **Runs:** per `Append` (per column/parameter per render) · **Expected gain:** Medium (side effect of the fix) · **Risk:** medium; changes data resolution in the `IsEq` edge case

When `metadata` is null (every `Append` call), `newMetadata` becomes `this.Metadata`, so `metadata != newMetadata` is `null != this.Metadata`, which is true. `ProjectionData.Resolve` therefore runs on every `Append`, even though the metadata didn't change. When the data's relation doesn't match, `Resolve` can fall back to building a full `ProjectionReader`. `Projection.With` uses the intended comparison, `newMetadata != this.Metadata`. Run the Mvc tests carefully: `IsEq` (`l.With(data: r.Data).Par()`) currently relies on this re-resolution, even though its result is overwritten on the next line.

**Before**
```csharp
public IProjectionAttribute With(IProjectionMetadata metadata = null, IProjectionData data = null, ISqlContent content = null)
{
    IProjectionMetadata newMetadata = metadata ?? this.Metadata;
    IProjectionData newData = data ?? (metadata != newMetadata ? ProjectionData.Resolve(this.Data, newMetadata) : this.Data);
    ISqlContent newContent = content ?? this.Content;

    return new ProjectionAttribute(this, newMetadata, newData, newContent);
}
```

**After**
```csharp
public IProjectionAttribute With(IProjectionMetadata metadata = null, IProjectionData data = null, ISqlContent content = null)
{
    IProjectionMetadata newMetadata = metadata ?? this.Metadata;
    IProjectionData newData = data ?? (newMetadata != this.Metadata ? ProjectionData.Resolve(this.Data, newMetadata) : this.Data);
    ISqlContent newContent = content ?? this.Content;

    return new ProjectionAttribute(this, newMetadata, newData, newContent);
}
```

**Diff**
```diff
 public IProjectionAttribute With(IProjectionMetadata metadata = null, IProjectionData data = null, ISqlContent content = null)
 {
     IProjectionMetadata newMetadata = metadata ?? this.Metadata;
-    IProjectionData newData = data ?? (metadata != newMetadata ? ProjectionData.Resolve(this.Data, newMetadata) : this.Data);
+    IProjectionData newData = data ?? (newMetadata != this.Metadata ? ProjectionData.Resolve(this.Data, newMetadata) : this.Data);
     ISqlContent newContent = content ?? this.Content;
 
     return new ProjectionAttribute(this, newMetadata, newData, newContent);
 }
```

### 5.4 `IsoDialect.String`: `NullReferenceException` on null
**File:** [IsoDialect.cs:39-45](src/Mvc/Jerrycurl.Mvc/IsoDialect.cs#L39-L45) · **Runs:** per string literal · **Expected gain:** none (correctness) · **Risk:** low; null now returns `"NULL"` as the code intended

`value.Contains('\'')` runs before the `value != null` check, so `String(null)` throws `NullReferenceException` instead of returning `"NULL"`.

**Before**
```csharp
public virtual string String(string value)
{
    if (value.Contains('\''))
        throw new InvalidOperationException("String literal cannot contain the ' character.");

    return value != null ? $"{this.StringPrefix}'{value}'" : "NULL";
}
```

**After**
```csharp
public virtual string String(string value)
{
    if (value == null)
        return "NULL";

    if (value.Contains('\''))
        throw new InvalidOperationException("String literal cannot contain the ' character.");

    return $"{this.StringPrefix}'{value}'";
}
```

**Diff**
```diff
 public virtual string String(string value)
 {
+    if (value == null)
+        return "NULL";
+
     if (value.Contains('\''))
         throw new InvalidOperationException("String literal cannot contain the ' character.");
 
-    return value != null ? $"{this.StringPrefix}'{value}'" : "NULL";
+    return $"{this.StringPrefix}'{value}'";
 }
```

### 5.5 `MetadataExtensions.HasFlag(ReferenceMetadataFlags)`: wrong null check
**File:** [MetadataExtensions.cs:30-31](src/Mvc/Jerrycurl.Mvc.Abstractions/Metadata/MetadataExtensions.cs#L30-L31) · **Runs:** per `Key()`/`NonKey()` header filter · **Expected gain:** none (correctness) · **Risk:** low; returns `false` instead of throwing

Both overloads check `metadata.Relation != null` but then dereference `metadata.Reference`, which can be null. This throws `NullReferenceException` for any projection metadata that has a relation but no reference metadata.

**Before**
```csharp
public static bool HasFlag(this IProjectionMetadata metadata, ReferenceMetadataFlags flag) => (metadata.Relation != null && metadata.Reference.HasFlag(flag));
public static bool HasAnyFlag(this IProjectionMetadata metadata, ReferenceMetadataFlags flag) => (metadata.Relation != null && metadata.Reference.HasAnyFlag(flag));
```

**After**
```csharp
public static bool HasFlag(this IProjectionMetadata metadata, ReferenceMetadataFlags flag) => (metadata.Reference != null && metadata.Reference.HasFlag(flag));
public static bool HasAnyFlag(this IProjectionMetadata metadata, ReferenceMetadataFlags flag) => (metadata.Reference != null && metadata.Reference.HasAnyFlag(flag));
```

**Diff**
```diff
-public static bool HasFlag(this IProjectionMetadata metadata, ReferenceMetadataFlags flag) => (metadata.Relation != null && metadata.Reference.HasFlag(flag));
-public static bool HasAnyFlag(this IProjectionMetadata metadata, ReferenceMetadataFlags flag) => (metadata.Relation != null && metadata.Reference.HasAnyFlag(flag));
+public static bool HasFlag(this IProjectionMetadata metadata, ReferenceMetadataFlags flag) => (metadata.Reference != null && metadata.Reference.HasFlag(flag));
+public static bool HasAnyFlag(this IProjectionMetadata metadata, ReferenceMetadataFlags flag) => (metadata.Reference != null && metadata.Reference.HasAnyFlag(flag));
```

---

## 6. Native .NET replacements (net6–net11)

These entries swap hand-written helpers and old idioms in `src/` for BCL APIs. None of them overlaps sections 1–5. Each one keeps behavior the same, or states the exact difference.

### 6.1 Closure-free `DictionaryExtensions.GetOrAdd` (value and `new()` overloads)
**File:** [Shared/Jerrycurl.Common/Collections/DictionaryExtensions.cs:L19-L34](src/Shared/Jerrycurl.Common/Collections/DictionaryExtensions.cs#L19-L34) · **Native API:** `CollectionsMarshal.GetValueRefOrAddDefault` (.NET 6) · **Runs:** per query (CommandBuffer, per parameter); cold for BufferCache (query compile) · **Expected gain:** Low · **Risk:** Common: Dictionary overload goes under `#if NET`, and the IDictionary path stays for netstandard2.0

`GetOrAdd(key, value)` wraps the value in `() => value`, which allocates a closure and a delegate on every call, hit or miss. `GetOrAdd(key)` runs `new TValue()` *before* the lookup, so every hit still allocates a throw-away object. For example, [BufferCache.cs:L56](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Caching/BufferCache.cs#L56) allocates a new `Dictionary<MetadataIdentity,int>` on every `GetJoinIndex`.

The fix has two parts:
- Under `#if NET`, add `Dictionary<TKey,TValue>` overloads. Overload resolution prefers them over the `IDictionary` ones because the identity conversion beats the interface conversion. The value overload uses `GetValueRefOrAddDefault`, so a hit or a miss costs one hash lookup.
- Rewrite the `IDictionary` bodies without closures. Both targets get this part.

The `new()` overload keeps `TryGetValue` + `Add`, so a constructor that throws can't leave a `default` slot behind. Its `Dictionary` twin exists only so the calls bind to concrete, non-interface methods. A null key throws `ArgumentNullException` either way.

Call sites that bind to the new overloads:
- [BufferCache.cs:L56](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Caching/BufferCache.cs#L56), [L58](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Caching/BufferCache.cs#L58), [L65](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Caching/BufferCache.cs#L65), [L78](src/Mvc/Jerrycurl.Cqs/Queries/Internal/Caching/BufferCache.cs#L78)
- `CommandBuffer.cs` L180/L214/L225/L238/L239. Section 3 rewrites these sites directly; until then they also get the fix.

The value argument (`innerMap.Count`, `this.parentMap.Count + 1`) is evaluated before the call, so the stored index is the same as today.

**Before**
```csharp
public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, TValue value) => d.GetOrAdd(key, () => value);
public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, Func<TValue> valueFactory)
{
    if (d.TryGetValue(key, out TValue value2))
        return value2;

    TValue value;

    d.Add(key, value = valueFactory());

    return value;
}

public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key)
    where TValue : new()
    => d.GetOrAdd(key, new TValue());
```

**After**
```csharp
public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, TValue value)
{
    if (d.TryGetValue(key, out TValue existing))
        return existing;

    d.Add(key, value);

    return value;
}
public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, Func<TValue> valueFactory)
{
    if (d.TryGetValue(key, out TValue value2))
        return value2;

    TValue value;

    d.Add(key, value = valueFactory());

    return value;
}

public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key)
    where TValue : new()
{
    if (!d.TryGetValue(key, out TValue value))
        d.Add(key, value = new TValue());

    return value;
}

#if NET
public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key, TValue value)
{
    ref TValue slot = ref CollectionsMarshal.GetValueRefOrAddDefault(d, key, out bool exists);

    if (!exists)
        slot = value;

    return slot;
}

public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key)
    where TValue : new()
{
    if (!d.TryGetValue(key, out TValue value))
        d.Add(key, value = new TValue());

    return value;
}
#endif
```

**Diff**
```diff
-public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, TValue value) => d.GetOrAdd(key, () => value);
+public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, TValue value)
+{
+    if (d.TryGetValue(key, out TValue existing))
+        return existing;
+
+    d.Add(key, value);
+
+    return value;
+}
 public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, Func<TValue> valueFactory)
 {
     ...
 }
 
 public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key)
     where TValue : new()
-    => d.GetOrAdd(key, new TValue());
+{
+    if (!d.TryGetValue(key, out TValue value))
+        d.Add(key, value = new TValue());
+
+    return value;
+}
+
+#if NET
+public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key, TValue value)
+{
+    ref TValue slot = ref CollectionsMarshal.GetValueRefOrAddDefault(d, key, out bool exists);
+
+    if (!exists)
+        slot = value;
+
+    return slot;
+}
+
+public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key)
+    where TValue : new()
+{
+    if (!d.TryGetValue(key, out TValue value))
+        d.Add(key, value = new TValue());
+
+    return value;
+}
+#endif
```
(add `using System.Runtime.InteropServices;` under the same `#if NET`)

### 6.2 Retire the shadowed `GetValueOrDefault` / `TryAdd` / `TryRemove` polyfills
**File:** [Shared/Jerrycurl.Common/Collections/DictionaryExtensions.cs:L8-L54](src/Shared/Jerrycurl.Common/Collections/DictionaryExtensions.cs#L8-L54) · **Native API:** `CollectionExtensions.GetValueOrDefault` (.NET Core 2.0), `Dictionary.TryAdd` (.NET Core 2.0), `IDictionary.Remove` (bool) · **Runs:** cold (metadata lookup) · **Expected gain:** Low · **Risk:** Common: no Build project calls them; guard with `#if NETSTANDARD2_0` or delete

On net11, all three helpers are dead or shadowed:
- **`TryAdd`:** never runs. Instance methods beat extension methods, so every call site uses the instance method: `ConcurrentDictionary.TryAdd` at [Schema.cs:L53](src/Mvc/Jerrycurl.Relations/Metadata/Schema.cs#L53) and `Dictionary.TryAdd` at `CommandBuffer.cs` L186/L230.
- **`TryRemove(IDictionary)`:** has no callers. The only `TryRemove` is `ConcurrentDictionary.TryRemove(key, out _)`.
- **`GetValueOrDefault`:** wherever `System.Collections.Generic` is imported, the BCL `CollectionExtensions.GetValueOrDefault` wins the tie-break, because the custom version has to fill an optional parameter. `Jerrycurl.Cqs.dll` references `CollectionExtensions`, which confirms this for CommandBuffer.cs:L148. The same applies to the Tools call sites (`Dictionary<string,string>`).

The one live binding is [Schema.cs:L60](src/Mvc/Jerrycurl.Relations/Metadata/Schema.cs#L60). That file has no `using System.Collections.Generic`, so it falls back to the custom `IDictionary` helper. `Jerrycurl.Relations.dll` has no `CollectionExtensions` reference. That call goes through interface dispatch over `ConcurrentDictionary`. Calling `TryGetValue` on the concrete type is direct, and the cast/null result stays the same.

**Before**
```csharp
internal TMetadata GetCachedMetadata<TMetadata>(string name)
    where TMetadata : IMetadata
{
    return (TMetadata)this.entries.GetValueOrDefault(this.CreateKey<TMetadata>(name));
}
```

**After**
```csharp
internal TMetadata GetCachedMetadata<TMetadata>(string name)
    where TMetadata : IMetadata
{
    return (TMetadata)(this.entries.TryGetValue(this.CreateKey<TMetadata>(name), out object value) ? value : null);
}
```

**Diff**
```diff
 internal TMetadata GetCachedMetadata<TMetadata>(string name)
     where TMetadata : IMetadata
 {
-    return (TMetadata)this.entries.GetValueOrDefault(this.CreateKey<TMetadata>(name));
+    return (TMetadata)(this.entries.TryGetValue(this.CreateKey<TMetadata>(name), out object value) ? value : null);
 }
```
```diff
 // DictionaryExtensions.cs
+#if NETSTANDARD2_0
 public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, TValue defaultValue = default)
 { ... }
+#endif
 ...
-public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, TValue value)
-{ ... }
-
-public static bool TryRemove<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key)
-{ ... }
```

### 6.3 `EnumerableExtensions.IndexOf` via `Enumerable.Index`; delete unused helpers
**File:** [Shared/Jerrycurl.Common/Collections/EnumerableExtensions.cs:L27-L79](src/Shared/Jerrycurl.Common/Collections/EnumerableExtensions.cs#L27-L79) · **Native API:** `Enumerable.Index<T>` (.NET 9), `Enumerable.ToHashSet` (.NET Core 2.0) · **Runs:** cold · **Expected gain:** Low · **Risk:** Common: `#if NET9_0_OR_GREATER`, with the old body kept in `#else`

`IndexOf(predicate)` builds a `Select` over `((T, int)?)` nullable tuples, then runs `FirstOrDefault` with a second delegate. Its only caller is [ProjectionMetadataBuilder.cs:L86](src/Mvc/Jerrycurl.Mvc/Metadata/ProjectionMetadataBuilder.cs#L86). On .NET 9+ a `foreach` over `source.Index()` returns the same first-match index, or -1, with no tuple boxing and no extra delegate.

`ToSet` (both overloads) duplicates `Enumerable.ToHashSet` / `new HashSet<T>(source, comparer)`. Like `Second`, `SecondOrDefault`, `ZipAll` and `AsLazy`, it has no callers anywhere in `src/`, including Build, so all of them can go.

**Before**
```csharp
public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    => source.Select((it, i) => ((T Item, int Index)?)(it, i)).FirstOrDefault(t => predicate(t.Value.Item))?.Index ?? -1;
```

**After**
```csharp
public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
{
#if NET9_0_OR_GREATER
    foreach ((int index, T item) in source.Index())
    {
        if (predicate(item))
            return index;
    }

    return -1;
#else
    return source.Select((it, i) => ((T Item, int Index)?)(it, i)).FirstOrDefault(t => predicate(t.Value.Item))?.Index ?? -1;
#endif
}
```

**Diff**
```diff
-public static T Second<T>(this IEnumerable<T> source) => source.Skip(1).First();
-public static T SecondOrDefault<T>(this IEnumerable<T> source) => source.Skip(1).FirstOrDefault();
 ...
-public static HashSet<T> ToSet<T>(this IEnumerable<T> source) => new HashSet<T>(source);
-public static HashSet<T> ToSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer) => new HashSet<T>(source, comparer);
 ...
-public static IEnumerable<(TFirst First, TSecond Second)> ZipAll<TFirst, TSecond>(...) { ... }
 ...
 public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
-    => source.Select((it, i) => ((T Item, int Index)?)(it, i)).FirstOrDefault(t => predicate(t.Value.Item))?.Index ?? -1;
+{
+#if NET9_0_OR_GREATER
+    foreach ((int index, T item) in source.Index())
+    {
+        if (predicate(item))
+            return index;
+    }
+
+    return -1;
+#else
+    return source.Select((it, i) => ((T Item, int Index)?)(it, i)).FirstOrDefault(t => predicate(t.Value.Item))?.Index ?? -1;
+#endif
+}
 ...
-public static IEnumerable<T> AsLazy<T>(this IEnumerable<T> source) { ... }
```

### 6.4 `FieldArray.EnsureIndex` via `CollectionsMarshal.SetCount`
**File:** [Mvc/Jerrycurl.Relations/Internal/Queues/FieldArray.cs:L26-L30](src/Mvc/Jerrycurl.Relations/Internal/Queues/FieldArray.cs#L26-L30) · **Native API:** `CollectionsMarshal.SetCount<T>(List<T>, int)` (.NET 8) · **Runs:** per row (cached relation queues) · **Expected gain:** Low · **Risk:** none; `IField` is a reference type, so the new slots are null

`RelationQueue` creates a new `FieldArray` for every cached item ([RelationQueue.cs:L75](src/Mvc/Jerrycurl.Relations/Internal/Queues/RelationQueue.cs#L75)). Each time it grows, it allocates a temporary `IField[]` only to pass it to `AddRange`.

`SetCount` grows the list with the same `Grow(count)` capacity policy, so it ends up with the same capacity and no temporary array. The new slots are null because `List<T>` keeps unused reference slots cleared.

This is the same pattern as `ElasticArray` and `SqlBuffer.ReserveBuffer`, which sections 3 and 4 already cover. This file is not covered there.

**Before**
```csharp
private void EnsureIndex(int index)
{
    if (index >= this.innerList.Count)
        this.innerList.AddRange(new IField[index - this.innerList.Count + 1]);
}
```

**After**
```csharp
private void EnsureIndex(int index)
{
    if (index >= this.innerList.Count)
        CollectionsMarshal.SetCount(this.innerList, index + 1);
}
```

**Diff**
```diff
+using System.Runtime.InteropServices;
 ...
 private void EnsureIndex(int index)
 {
     if (index >= this.innerList.Count)
-        this.innerList.AddRange(new IField[index - this.innerList.Count + 1]);
+        CollectionsMarshal.SetCount(this.innerList, index + 1);
 }
```

### 6.5 Guard clauses → `ArgumentNullException.ThrowIfNull`
**File:** [Mvc/Jerrycurl.Relations/Field.cs:L26-L30](src/Mvc/Jerrycurl.Relations/Field.cs#L26-L30) (representative) · **Native API:** `ArgumentNullException.ThrowIfNull(object, [CallerArgumentExpression] string)` (.NET 6) · **Runs:** per row (`Field` ctor), per query (sessions/engines), one-time elsewhere · **Expected gain:** Low · **Risk:** none outside Common; leave Shared (14 sites) and Build as they are

There are 102 `if (x == null) throw new ArgumentNullException(nameof(x));` blocks:

| Project | Sites |
|---|---|
| Cqs | 63 |
| Mvc | 23 |
| Relations | 9 |
| Mvc.Abstractions | 3 |
| Tools | 3 |
| EntityFrameworkCore | 1 |

Examples: `Field` and `Missing` ctors, [AsyncSession.cs:L25-L26](src/Mvc/Jerrycurl.Cqs/Sessions/AsyncSession.cs#L25-L26), `QueryEngine`/`CommandEngine` entry points.

`ThrowIfNull` moves the throw into a non-inlined helper. Methods get smaller and become easier to inline, which matters most for the per-row `Field` ctor. `ParamName` and the message are identical, because `CallerArgumentExpression` produces the same text as `nameof(x)` for a simple identifier.

There is one exception: [Schema.cs:L48-L49](src/Mvc/Jerrycurl.Relations/Metadata/Schema.cs#L48-L49) uses `nameof(metadata.Relation)`, which gives `"Relation"`. It has to pass that name explicitly to keep `ParamName` the same.

Leave the ~100 `x ?? throw new ArgumentNullException(...)` assignment forms alone; they are already single expressions. `ObjectDisposedException.ThrowIf` is **not** a drop-in for the session checks (`SyncSession.cs:121`, `AsyncSession.cs:123`), because they pass a message as `objectName`.

**Before**
```csharp
if (name == null)
    throw new ArgumentNullException(nameof(name));

if (metadata == null)
    throw new ArgumentNullException(nameof(metadata));
```

**After**
```csharp
ArgumentNullException.ThrowIfNull(name);
ArgumentNullException.ThrowIfNull(metadata);
```

**Diff**
```diff
-if (name == null)
-    throw new ArgumentNullException(nameof(name));
-
-if (metadata == null)
-    throw new ArgumentNullException(nameof(metadata));
+ArgumentNullException.ThrowIfNull(name);
+ArgumentNullException.ThrowIfNull(metadata);
```
```diff
 // Schema.cs:48 — keep the existing ParamName
-if (metadata.Relation == null)
-    throw new ArgumentNullException(nameof(metadata.Relation));
+ArgumentNullException.ThrowIfNull(metadata.Relation, nameof(metadata.Relation));
```

### 6.6 `AsyncSession.DisposeAsync`: `Close()` → `CloseAsync()`
**File:** [Mvc/Jerrycurl.Cqs/Sessions/AsyncSession.cs:L204-L213](src/Mvc/Jerrycurl.Cqs/Sessions/AsyncSession.cs#L204-L213) · **Native API:** `DbConnection.CloseAsync()` (.NET Core 3.0) · **Runs:** per query (one async session per Execute/Enumerate) · **Expected gain:** Low · **Risk:** depends on the provider; the base class falls back to sync `Close()`

The async disposal path closes the connection synchronously. That blocks the caller's thread for any network work the provider does on close; Npgsql and MySqlConnector, for example, implement real async close/reset. `this.connection` is already a `DbConnection`, because `VerifyConnection` enforces it. Exceptions still surface through the same `catch`, so filter behavior doesn't change. Providers that don't override `CloseAsync` run the same `Close()`.

The same idiom appears at [Tools/Jerrycurl.Tools/Orm/OrmTool.cs:L53](src/Tools/Jerrycurl.Tools/Orm/OrmTool.cs#L53): `using (DbDataReader …)` inside an async iterator. It can become `await using` (`DbDataReader.DisposeAsync`, .NET Core 3.0).

**Before**
```csharp
try
{
    this.connection?.Close();
}
catch (Exception ex)
{
    await this.ApplyFilters(h => h.OnExceptionAsync, exception: ex, swallowExceptions: true).ConfigureAwait(false);

    throw;
}
```

**After**
```csharp
try
{
    if (this.connection != null)
        await this.connection.CloseAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    await this.ApplyFilters(h => h.OnExceptionAsync, exception: ex, swallowExceptions: true).ConfigureAwait(false);

    throw;
}
```

**Diff**
```diff
 try
 {
-    this.connection?.Close();
+    if (this.connection != null)
+        await this.connection.CloseAsync().ConfigureAwait(false);
 }
 catch (Exception ex)
```

### 6.7 `ToolRunner.RunAsync`: `Task.Run(WaitForExit)` + TCS/`Task.Delay` → `Process.WaitForExitAsync`
**File:** [Tools/Jerrycurl.Tools/ToolRunner.cs:L74-L144](src/Tools/Jerrycurl.Tools/ToolRunner.cs#L74-L144) · **Native API:** `Process.WaitForExitAsync(CancellationToken)` (.NET 5), `CancellationTokenSource(int)` · **Runs:** cold (one Node.js transform per `jerry orm` run) · **Expected gain:** Low · **Risk:** low; the timeout and exit-code semantics are unchanged

The current code has several hand-built parts:
- it blocks a thread-pool thread in `process.WaitForExit(timeout)` for up to 30 s;
- two `TaskCompletionSource<bool>` instances detect stdout/stderr EOF, a `null` `Data` event;
- `Task.WhenAny` races the result against a `Task.Delay` timer.

On .NET 5+, `WaitForExitAsync` waits for the process to exit *and* for the redirected async streams to reach EOF. The null-data callback has already run by the time EOF completes, so the TCSs become redundant.

A `CancellationTokenSource(options.Timeout)` replaces both the timer and `WaitForExit(timeout)`. `-1`/`Infinite` still means no timeout. A timeout still kills the process and throws `Exception("Timed out.")`, and a non-zero exit still throws `ToolException`.

`Task.WaitAsync(TimeSpan)` (.NET 6) would also work. It is not used because it leaves the inner wait registered until the process ends.

**Before**
```csharp
TaskCompletionSource<bool> outputCloseEvent = new TaskCompletionSource<bool>();

if (startInfo.RedirectStandardOutput)
{
    process.OutputDataReceived += (s, e) =>
    {
        if (e.Data == null)
            outputCloseEvent.SetResult(true);
        else
            StdOut(e);
    };
}
else
    outputCloseEvent.SetResult(true);
// ... same for errorCloseEvent ...

Task<bool> waitForExit = WaitForExitAsync(process, options.Timeout);
Task processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);

if (await Task.WhenAny(Task.Delay(options.Timeout), processTask) == processTask && await waitForExit)
{
    if (process.ExitCode != 0)
        throw new ToolException(process.ExitCode, stdOut: stdOutBuilder.ToString(), stdErr: stdErrBuilder.ToString());
}
else
{
    try
    {
        process.Kill();
    }
    catch { }

    throw new Exception("Timed out.");
}
...
static Task<bool> WaitForExitAsync(Process process, int timeout) => Task.Run(() => process.WaitForExit(timeout));
```

**After**
```csharp
if (startInfo.RedirectStandardOutput)
    process.OutputDataReceived += (s, e) => { if (e.Data != null) StdOut(e); };

if (startInfo.RedirectStandardError)
    process.ErrorDataReceived += (s, e) => { if (e.Data != null) StdErr(e); };

// ... Start / BeginOutputReadLine / BeginErrorReadLine unchanged ...

using CancellationTokenSource timeout = new CancellationTokenSource(options.Timeout);

try
{
    // .NET 5+: also waits for redirected stdout/stderr to reach EOF
    await process.WaitForExitAsync(timeout.Token);
}
catch (OperationCanceledException) when (timeout.IsCancellationRequested)
{
    try
    {
        process.Kill();
    }
    catch { }

    throw new Exception("Timed out.");
}

if (process.ExitCode != 0)
    throw new ToolException(process.ExitCode, stdOut: stdOutBuilder.ToString(), stdErr: stdErrBuilder.ToString());
```

**Diff**
```diff
-TaskCompletionSource<bool> outputCloseEvent = new TaskCompletionSource<bool>();
-
 if (startInfo.RedirectStandardOutput)
-{
-    process.OutputDataReceived += (s, e) =>
-    {
-        if (e.Data == null)
-            outputCloseEvent.SetResult(true);
-        else
-            StdOut(e);
-    };
-}
-else
-    outputCloseEvent.SetResult(true);
-
-TaskCompletionSource<bool> errorCloseEvent = new TaskCompletionSource<bool>();
+    process.OutputDataReceived += (s, e) => { if (e.Data != null) StdOut(e); };
 
 if (startInfo.RedirectStandardError)
-{
-    process.ErrorDataReceived += (s, e) =>
-    {
-        if (e.Data == null)
-            errorCloseEvent.SetResult(true);
-        else
-            StdErr(e);
-    };
-}
-else
-    errorCloseEvent.SetResult(true);
+    process.ErrorDataReceived += (s, e) => { if (e.Data != null) StdErr(e); };
 ...
-Task<bool> waitForExit = WaitForExitAsync(process, options.Timeout);
-Task processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);
+using CancellationTokenSource timeout = new CancellationTokenSource(options.Timeout);
 
-if (await Task.WhenAny(Task.Delay(options.Timeout), processTask) == processTask && await waitForExit)
+try
 {
-    if (process.ExitCode != 0)
-        throw new ToolException(process.ExitCode, stdOut: stdOutBuilder.ToString(), stdErr: stdErrBuilder.ToString());
+    await process.WaitForExitAsync(timeout.Token);
 }
-else
+catch (OperationCanceledException) when (timeout.IsCancellationRequested)
 {
     try
     {
         process.Kill();
     }
     catch { }
 
     throw new Exception("Timed out.");
 }
+
+if (process.ExitCode != 0)
+    throw new ToolException(process.ExitCode, stdOut: stdOutBuilder.ToString(), stdErr: stdErrBuilder.ToString());
 ...
-static Task<bool> WaitForExitAsync(Process process, int timeout) => Task.Run(() => process.WaitForExit(timeout));
```
(add `using System.Threading;`)

### 6.8 `ToolRunner`: hand-built argument string → `ProcessStartInfo.ArgumentList`
**File:** [Tools/Jerrycurl.Tools/ToolRunner.cs:L48-L57](src/Tools/Jerrycurl.Tools/ToolRunner.cs#L48-L57) · **Native API:** `ProcessStartInfo.ArgumentList` (.NET Core 2.1) · **Runs:** cold · **Expected gain:** Low · **Risk:** quoting differs only for arguments that are broken today

`Escape` wraps whitespace-containing arguments in `"..."` but does not escape embedded quotes or trailing backslashes. On Windows, `"C:\my dir\"` is parsed as `C:\my dir"`. `ArgumentList` applies the platform's correct quoting rules. For the current callers ([OrmTransformer.cs:L41](src/Tools/Jerrycurl.Tools/Orm/OrmTransformer.cs#L41): four file paths), the process sees the same arguments.

The public `ToolRunner.Escape` stays, because it is public API, but `RunAsync` no longer uses it.

**Before**
```csharp
ProcessStartInfo startInfo = new ProcessStartInfo()
{
    FileName = options.ToolName,
    Arguments = Escape(options.Arguments),
    WorkingDirectory = options.WorkingDirectory,
    ...
};
```

**After**
```csharp
ProcessStartInfo startInfo = new ProcessStartInfo()
{
    FileName = options.ToolName,
    WorkingDirectory = options.WorkingDirectory,
    ...
};

foreach (string argument in options.Arguments ?? [])
    startInfo.ArgumentList.Add(argument);
```

**Diff**
```diff
 ProcessStartInfo startInfo = new ProcessStartInfo()
 {
     FileName = options.ToolName,
-    Arguments = Escape(options.Arguments),
     WorkingDirectory = options.WorkingDirectory,
     ...
 };
+
+foreach (string argument in options.Arguments ?? [])
+    startInfo.ArgumentList.Add(argument);
```

### 6.9 `ContainsKey` + `Add`/indexer double lookups → `Dictionary.TryAdd`
**File:** [Mvc/Jerrycurl.Relations/RelationDataReader.cs:L37-L40](src/Mvc/Jerrycurl.Relations/RelationDataReader.cs#L37-L40) · **Native API:** `Dictionary<TKey,TValue>.TryAdd` (.NET Core 2.0) · **Runs:** one-time per reader; per row in the ORM tool · **Expected gain:** Low · **Risk:** none

Each site hashes the key twice when the key is new. `TryAdd` hashes once and returns the same "already present" signal. Other sites:
- [TupleModel.cs:L30-L31](src/Tools/Jerrycurl.Tools/Orm/Model/TupleModel.cs#L30-L31), per column per schema row: `if (!this.map.ContainsKey(key)) this.map.Add(key, dataReader.GetValue(i));` → `this.map.TryAdd(key, dataReader.GetValue(i));`. `GetValue(i)` now also runs for duplicate column names. It has no side effects for the providers involved.
- [SchemaBuilder.cs:L52-L53](src/Tools/Jerrycurl.Tools/Orm/Model/SchemaBuilder.cs#L52-L53): `if (overwrite) Flags[flag] = value; else Flags.TryAdd(flag, value);`

**Before**
```csharp
if (this.headerMap.ContainsKey(this.Header[i]))
    throw RelationException.DataHeaderCannotHaveDupes(this.InnerReader.Relation.Header, this.Header, i);

this.headerMap.Add(this.Header[i], i);
```

**After**
```csharp
if (!this.headerMap.TryAdd(this.Header[i], i))
    throw RelationException.DataHeaderCannotHaveDupes(this.InnerReader.Relation.Header, this.Header, i);
```

**Diff**
```diff
-if (this.headerMap.ContainsKey(this.Header[i]))
+if (!this.headerMap.TryAdd(this.Header[i], i))
     throw RelationException.DataHeaderCannotHaveDupes(this.InnerReader.Relation.Header, this.Header, i);
-
-this.headerMap.Add(this.Header[i], i);
```

### 6.10 Pre-computed `Lazy<T>` → `Lazy<T>(T value)` constructor
**File:** [Mvc/Jerrycurl.Mvc/Metadata/ProjectionMetadataBuilder.cs:L96](src/Mvc/Jerrycurl.Mvc/Metadata/ProjectionMetadataBuilder.cs#L96) · **Native API:** `Lazy<T>(T value)` (.NET Core 3.0) · **Runs:** cold (once per projection metadata) · **Expected gain:** Low · **Risk:** `IsValueCreated` is `true` immediately; nothing in `src/` reads it

`new Lazy<T>(() => metadata)` allocates a closure, a delegate and a lazy-init lock object, all for a value that already exists. The pre-initialized constructor stores the value directly.

[RelationMetadataBuilder.cs:L208](src/Mvc/Jerrycurl.Relations/Metadata/RelationMetadataBuilder.cs#L208) (`() => metadata.Owner`) is a candidate too. It only qualifies if `Owner` is never reassigned after this point; the assignments at L45 and L241 run when the metadata is created. Confirm that before changing it.

**Before**
```csharp
metadata.Input = new Lazy<IProjectionMetadata>(() => metadata);
```

**After**
```csharp
metadata.Input = new Lazy<IProjectionMetadata>(metadata);
```

**Diff**
```diff
-metadata.Input = new Lazy<IProjectionMetadata>(() => metadata);
+metadata.Input = new Lazy<IProjectionMetadata>(metadata);
```

### 6.11 `CSharp.Identifier`: LINQ char array → `string.Create`
**File:** [Shared/Jerrycurl.Common/CodeAnalysis/CSharp.cs:L16](src/Shared/Jerrycurl.Common/CodeAnalysis/CSharp.cs#L16) · **Native API:** `string.Create<TState>(int, TState, SpanAction<char,TState>)` (.NET Core 2.1) · **Runs:** cold (codegen: Razor/ORM, `Namespace` ctor per part) · **Expected gain:** Low · **Risk:** Common: `#if NET`, with the LINQ form kept for netstandard2.0

The current code runs a `Select` iterator, builds a `char[]` through the collection-expression spread, and then copies it into a `string`. `string.Create` writes the characters straight into the final string, with a static lambda and no captures. The output is identical, character for character.

**Before**
```csharp
string identifier = new string([.. s.Select(c => IsIdentifierPart(c) ? c : '_')]);
```

**After**
```csharp
#if NET
string identifier = string.Create(s.Length, s, static (span, source) =>
{
    for (int i = 0; i < source.Length; i++)
        span[i] = IsIdentifierPart(source[i]) ? source[i] : '_';
});
#else
string identifier = new string([.. s.Select(c => IsIdentifierPart(c) ? c : '_')]);
#endif
```

**Diff**
```diff
+#if NET
+string identifier = string.Create(s.Length, s, static (span, source) =>
+{
+    for (int i = 0; i < source.Length; i++)
+        span[i] = IsIdentifierPart(source[i]) ? source[i] : '_';
+});
+#else
 string identifier = new string([.. s.Select(c => IsIdentifierPart(c) ? c : '_')]);
+#endif
```

### 6.12 Static `Regex.*` calls in Tools → `[GeneratedRegex]`
**File:** [Tools/Jerrycurl.Tools/Orm/OrmDiff.cs:L178](src/Tools/Jerrycurl.Tools/Orm/OrmDiff.cs#L178) · **Native API:** `GeneratedRegexAttribute` (.NET 7) · **Runs:** cold (per line of the diffed file in `jerry orm diff`) · **Expected gain:** Low · **Risk:** the class must become `partial`, which does not break callers

`dotnet-jerry` calls `DiffText(..., ignoreSpace: true)` ([OrmCommandBuilder.cs:L175](src/Tools/dotnet-jerry/Commands/OrmCommandBuilder.cs#L175)), so every line of the generated file goes through `Regex.Replace(s, "\\s+", " ")`. Each call costs a static Regex-cache lookup and runs the interpreter. The source generator emits compiled matching code once, at build time, with the same pattern and options, so matching behavior is the same. It also removes the startup parse/cache cost.

Other call sites:
- [SqliteScaffoldCommand.cs:L179](src/Tools/Jerrycurl.Tools.Vendors/Sqlite/SqliteScaffoldCommand.cs#L179): `INTEGER\s+PRIMARY\s+KEY\s+AUTOINCREMENT`. It is case-sensitive today; keep `RegexOptions.None`.
- [OracleOrmTool.cs:L81](src/Tools/Jerrycurl.Tools.Vendors/Oracle/OracleOrmTool.cs#L81): `\(\d+\)`.

**Before**
```csharp
s = Regex.Replace(s, "\\s+", " ");            // TODO: optimization: faster blank removal.
```

**After**
```csharp
s = WhitespaceRegex().Replace(s, " ");
...
[GeneratedRegex("\\s+")]
private static partial Regex WhitespaceRegex();
```

**Diff**
```diff
-public class OrmDiff
+public partial class OrmDiff
 ...
-                s = Regex.Replace(s, "\\s+", " ");            // TODO: optimization: faster blank removal.
+                s = WhitespaceRegex().Replace(s, " ");
 ...
+    [GeneratedRegex("\\s+")]
+    private static partial Regex WhitespaceRegex();
```
