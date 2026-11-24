# Jerrycurl.Mvc → Jerrycurl.Razor: migration plan

## Context

Jerrycurl copies ASP.NET Core MVC. An `Accessor` subclass (the controller) calls `Query<T>()`. The calling method's name and the accessor's type name are then turned into a page type by `ProcLocator`. It strips `Accessor` and `Async`, searches `Queries/<Prefix>`, `Commands/<Prefix>` and their `Shared` folders, and appends `_cssql`. Pages reach each other the same way: `@template "Paged"` and `Subquery("Foo")` both go through `ProcLocator.FindPage`.

That model no longer fits:
- The generator now emits strongly typed pages. `@input` / `@output` produce nested `Input` / `Output` types, and the classes are `public`.
- `ProcPage<TModel,TResult>.Query<TFinal>(Type pageType, …)` already runs a page with no accessor and no name lookup (`src/Mvc/Jerrycurl.Mvc/ProcPage.cs:76`).
- **Name lookup is already broken for generated pages.** Since d34614b the generator emits class `Foo`, not `Foo_cssql`. `ProcLocator.SanitizeTypeName` still appends `_cssql`, so `@template`, `Subquery` and accessor calls cannot find generated pages. Only the hand-written `*_cssql` test classes still resolve.

**Goal:** a `.cssql` file is its own runnable unit, like a Razor Page. `Queries/Foo.cssql` declares `@query`, and you call it as `MyApp.Queries.Foo.QueryAsync(new() { … })`. Accessors, the folder conventions and string-based page lookup all go away. Pages refer to each other by type. The product is renamed `Jerrycurl.Mvc` → `Jerrycurl.Razor` as a hard break with no shim, as part of the relaunch.

---

## Target design

### 1. `@query` / `@command`: what a page is

The directive has no arguments and works like Razor's `@page`. It is the only thing that makes a page callable from C#.

| Page declares | Generated static API | Notes |
|---|---|---|
| `@query` | `Query`, `QueryAsync`, `Aggregate`, `AggregateAsync`, `Enumerate`, `EnumerateAsync` | Result type = `@output` / `@result`, else `object` (generic overloads let the caller choose) |
| `@command` | `Execute`, `ExecuteAsync` | Results are written back into the model (`@out` / `@in` bindings), the same as today. `@output` / `@result` is a compile error. |
| neither | **nothing**: the page is a *partial* | It can only be used via `@template` or `Subquery<T>` / `Subcommand<T>`. This mirrors a `.cshtml` without `@page`. |
| both | compile error | |

**The rule in one line:** *"Add `@query` or `@command` to make a page callable from C#. Without one, it's a building block for other pages."*

Further rules:
- `@query` / `@command` in `_imports.cssql` is an error. A page's kind must be visible in the page itself, as with `@page`.
- Inferring the kind from `@output` was rejected because it is implicit. A command with no output and a query whose output is `object` would look the same.
- Diagnostics are reported through `SourceProductionContext.ReportDiagnostic`, mapped to the `.cssql` file and line:
  - `JC0001` both directives present
  - `JC0002` `@output` / `@result` on a `@command`
  - `JC0003` kind directive in `_imports`
  - `JC0004` bad `@template` / `@input` syntax
  - `JC0005` invalid `@visibility` value (anything other than `public` / `internal`)
  - Optional info diagnostic `JC0100`: "page has no `@query`/`@command` and is only usable as a partial"

### 1a. `@visibility`: page accessibility

Generated page classes are **`internal` by default**. The reasons:
- **Single-assembly apps:** internal behaves the same as public.
- **Libraries:** nothing leaks by accident. A public page commits its nested `Input` / `Output` DTOs, its static entry points and `Action<SqlOptions>` to the library's public API, and that should be a deliberate choice.
- **The case that needs `public`:** a separate data project whose pages are called from another assembly. This is the case that opts in.

Precedence for the emitted modifier, highest first:

| Source | Example | Scope |
|---|---|---|
| Page directive | `@visibility public` | that page |
| Nearest `_imports.cssql` | `@visibility public` | the folder and its subfolders. Inherits like any other import, unlike `@query` / `@command` |
| MSBuild property | `<JerrycurlPageVisibility>public</JerrycurlPageVisibility>` | the whole project |
| Default | `internal` | |

- The directive name is deliberately not `@access`, to avoid confusion with the removed accessors.
- The same rule applies to partials (pages with no kind). A shared template library used from other assemblies is a valid reason for `public`.
- Nested `Input` / `Output` stay `public` nested types, and their effective accessibility follows the page.
- Tests in another assembly use `InternalsVisibleTo`, as for any other internal type.

### 2. Generated code shape

For `Queries/Blogs/GetAll.cssql` containing `@query`, `@input int Id` and `@output int Id, string Title`, with default visibility:

```csharp
namespace MyApp.Queries.Blogs;

internal partial class GetAll : global::Jerrycurl.Razor.ProcPage<GetAll.Input, GetAll.Output>
{
    public GetAll(IProjection model, IProjection result) : base(model, result) { }
    public override void Render() { /* WriteLiteral / Write … */ }
    public override void Throw(Exception ex) { … }

    public class Input  { public int Id { get; set; } }
    public class Output { public int Id { get; set; } public string Title { get; set; } }

    // @query
    public static IList<Output> Query(Input model, Action<SqlOptions> configure = null)
        => QueryCore<Output>(typeof(GetAll), model, configure);
    public static IList<T> Query<T>(Input model, Action<SqlOptions> configure = null) where T : Output
        => QueryCore<T>(typeof(GetAll), model, configure);
    public static Task<IList<Output>> QueryAsync(Input model, Action<SqlOptions> configure = null, CancellationToken cancellationToken = default) => …;
    // Aggregate / AggregateAsync / Enumerate / EnumerateAsync: same pattern
}
```

- **Thin generated wrappers.** All logic lives in `protected static` helpers on `ProcPage<TModel,TResult>`: `QueryCore`, `QueryCoreAsync`, `AggregateCore[Async]`, `EnumerateCore[Async]`, `ExecuteCore[Async]`. They are ported from `Accessor.cs` and the existing `ProcPage.Query`. The current public `ProcPage.Query(Type, …)` becomes `QueryCoreAsync` and is `protected`. It cannot be `private`, because generated subclasses must call it.
- **Rename instance `ISqlPage.Execute()` → `Render()`.** This avoids a clash with the generated static `Execute(model = default)`: C# would bind `GetAll.Execute()` to the parameterless instance method (error CS0120). `IProcRenderer Render` → rename the property to `Renderer`.
- **Model parameter:** `@input` gives `Input model` (default `null`). `@model T` gives `T model`. Neither gives `object model = null`.
- **`partial` class**, so users can add helpers or wrappers next to the `.cssql` file.
- **Explicit modifier.** The generator always emits `internal` or `public` (§1a). A user's partial part may omit the modifier, but it can't contradict the generated one. Visibility is changed only through `@visibility` or the MSBuild property, so there is one way to do it.
- **Runtime context:** helpers use `ProcEngine.Default` (new, settable static; default `new ProcEngine(ProcEngine.Services)`). This replaces `AccessorContext.Engine`, which DI tests used. Options still go through `Action<SqlOptions> configure`, seeded from `IDomainOptions.Sql`.

### 3. Typed page references (replaces `IProcLocator`)

- `@template Shared.Paged` (a C# type name, resolved by normal C# rules against the page's namespace and `@import`s) emits `[Template(typeof(Shared.Paged))]`. `TemplateAttribute.ProcName` (string) becomes `Type PageType`.
- `Subquery<TPage>(…)` / `Subcommand<TPage>(…)` replace the string overloads in `Sql/PartialExtensions.cs`. `IProcRenderer.Partial(string, …)` becomes `Partial(Type pageType, …)`.
- Typos become C# compile errors, and renames are refactor-safe.
- **Domain discovery stays at runtime.** `GetDomainType` (namespace walk for `IDomain`) moves from `ProcLocator` into `ProcEngine` and is cached per page type. A later option is a `@domain` directive or a generator-emitted attribute, which is out of scope here.

### 4. Removed

| Removed | Notes |
|---|---|
| `Accessor.cs`, `AccessorContext.cs` | Wrapping pages remains a documented pattern with no base class (§5) |
| `ProcLocator.cs`, `IProcLocator`, `PageLocatorKey`, `DomainDescriptor` | |
| `ProcCache.PageDescriptors` / `DomainDescriptors`, `IProcContext.Locator` | |
| `PageDescriptor.Locator` / `.OriginType` | `PageDescriptor` becomes `{ PageType, DomainType }`, built by `ProcEngine.Describe(Type)` |
| Folder conventions (`Queries` / `Commands` / `Shared` / `Accessors`), the `Async` strip and the `_cssql` suffix | |
| `PageNotFoundException` | kept only if still thrown for "type is not a page"; otherwise use `ProcExecutionException` |

`Queries/` and `Commands/` stay as a recommended *layout* in the docs only. The framework ignores folder names.

### 5. Wrapping pages: the accessor *pattern* (no base class)

Pages run on their own, so a framework `Accessor` base class would be an empty wrapper, and none is shipped. Wrapping pages is still the recommended shape for **libraries**: a plain public class that calls internal pages. It:
- gives domain-shaped signatures (`GetBlogAsync(int id)` instead of `GetAll.QueryAsync(new() { Id = id })`)
- hides the generated `Input` / `Output` types
- composes several pages, e.g. a command followed by a query, or a unit of work
- can hold a per-instance `ProcEngine` (see the per-call engine overload under Risks → DI)

Internal-by-default enforces this. A public wrapper can't return an internal page's nested `Output`, so the author either maps the result or declares `@result MyLib.Models.Blog`, a public model type. That is the right boundary for a library.

```csharp
public class BlogAccessor(ProcEngine engine = null)
{
    public async Task<Blog> GetBlogAsync(int id, CancellationToken ct = default)
        => (await Queries.Blogs.GetById.QueryAsync(new() { Id = id }, engine: engine, cancellationToken: ct)).FirstOrDefault();
}
```

**The rule for the docs:** *"Pages are `internal` by default. Call them directly inside your assembly. To expose data access from a library, write a public class that wraps them using public `@result` models, or set `@visibility public` and expose the pages themselves."*

---

## Phases

Each phase is one commit and leaves the solution building with tests green. Branch: `feat/razor-pages` off `feat/relaunch-pages`.

### Phase 0: Fix the generator bugs this depends on
In `src/Build/Jerrycurl.CodeAnalysis.Razor/Generation/RazorGenerator.cs`:
- **`WriteType` defaults.** `@input int Id = 5` currently emits `public int Id = 5 { get; set; } 5`. Emit `public int Id { get; set; } = 5;` instead, and trim the args.
- **TResult check.** It tests `pageData.Input` and should test `pageData.Output` (line ~79).
- **Arg splitting.** Splitting on `,` breaks on generics (`Dictionary<int, string> X`). Split on top-level commas only, tracking `<>` / `()` / `[]` depth.
- **Optional cleanup.** Delete the stale skeletons `.nusrc/build/RazorSkeleton.cs.txt` and `JerrycurlSkeletonPath`, plus the unused targets properties. Make `src/Tools/Jerrycurl.Tools/Resources/razor_skeleton.txt` share the template string in `RazorSourceGenerator.cs`, so the CLI and the generator can't drift.

### Phase 1: Runtime entry points (additive; accessors still work)
- Add the `*Core[Async]` helpers to `ProcPage.cs`, ported from `Accessor.cs` (sharing one private `Prepare(...)` that runs the proc and builds options and the engine) and from the existing `ProcPage.Query`.
- Add `ProcEngine.Default` and `ProcEngine.Describe(Type)`. Move domain discovery here.
- Rename `ISqlPage.Execute` → `Render` and `ProcPage.Render` → `Renderer`. Update `ProcEngine` (`page.Execute()` call sites) and the generator template.

### Phase 2: Generator: `@query` / `@command` and static API
- **Lexer/parser.** `RazorStartTag.cs` (directive list), `RazorType` (`Query`, `Command`), `CSharpBlock.cs` (no-arg directive), `RazorParser.cs` / `RazorPageData.cs` (a `Kind` property).
- **`RazorImportConvention.cs`.** Don't inherit the kind; flag it in `_imports`. *Do* inherit `@visibility`: the nearest `_imports` wins, and the page overrides it.
- **`@visibility` directive.** Add it to the lexer and parser (`RazorType.Visibility`, `RazorPageData.Visibility`). Report `JC0005` for an invalid value.
- **MSBuild property.** Add `<CompilerVisibleProperty Include="JerrycurlPageVisibility" />` to `src/Build/Jerrycurl.CodeAnalysis.Razor.Compiler/.nusrc/build/Jerrycurl.CodeAnalysis.Razor.Compiler.props`. Read it in `RazorSourceGenerator` through `config.GlobalOptions.TryGetValue("build_property.JerrycurlPageVisibility", …)`, next to the existing `ProjectDir` / `RootNamespace` reads. Default: `internal`.
- **`RazorGenerator.cs`.** New `$entrypoints$` slot in the template (`RazorSourceGenerator.cs:86-119`), emitted per kind as in §2. Add `partial`. Replace the hard-coded `public` class modifier with a `$visibility$` slot, resolved per §1a.
- **Diagnostics.** Carry parse errors and kind conflicts out of `RazorGenerator` (`RazorPageData.Diagnostics` list), and report them in `RazorSourceGenerator` with a `Location` from the `.cssql` path and line span.

### Phase 3: Typed `@template` / partials, delete the locator and accessors
- **`TemplateAttribute`** gets a `Type`. The generator emits `typeof(...)` (`RazorGenerator.cs:94`). `ProcEngine.GetTemplateDescriptor` reads the type.
- **Partials.** `PartialExtensions` gets `Subquery<TPage>` / `Subcommand<TPage>`, and `ProcRenderer.Partial(Type, …)`.
- **Deletions.** Delete everything in §4. Fix `ProcContext`, `ProcCacheKey` and `InternalsVisibleTo` users.
- **Tests (largest chunk):**
  - **`Runner`.** `test/unit/src/Shared/Jerrycurl.Test.Common/Project/Accessors/Runner.cs` (≈175 call sites across vendor, EFCore, Mvc and Cqs tests) stops deriving from `Accessor`. It becomes a static helper that keeps the same `Runner.Query/Command/Aggregate/Sql` surface, so call sites don't change. Inside, it calls the hand-written `Query` / `Command` pages' static methods. The hand-written pages in `Project/Queries/Query.cs` and `Project/Commands/Command.cs` drop the `_cssql` suffix and get static wrappers written by hand to match the generator output. `Runner.Sql` uses `ProcEngine.Describe(typeof(Query))`.
  - **Delete** `LocatorTests.cs`, `LocatorAccessor` and the `Project/{Queries,Commands}/Locator/**` and `…Shared/Locator*` fixtures.
  - **Rewrite** `CrudTests`, `TemplateTests`, `EngineTests`, `ServiceTests`, `BatchingTests` and the DI/NoDomain accessors against typed pages and `ProcEngine.Default`.
  - **Integration project** (`test/integration/src/Jerrycurl.Test.Integration`, CI-disabled): add `@query` / `@command` to the `.cssql` pages and replace `CrudAccessor` with direct calls. Low priority.

### Phase 4: Rename Jerrycurl.Mvc → Jerrycurl.Razor
This is a mechanical rename, with no logic changes in the same commit.
- **Folders and projects.** `src/Mvc/Jerrycurl.Mvc[.Abstractions]` → `src/Razor/Jerrycurl.Razor[.Abstractions]`. Move Cqs and Relations from `src/Mvc/` to `src/Core/` (they have no Mvc dependency). Test projects: `test/unit/src/Mvc/Jerrycurl.Mvc.Test` → `test/unit/src/Razor/Jerrycurl.Razor.Test`, and move the Cqs and Relations tests to match.
- **`jerrycurl.slnx`** and the 11 csproj `ProjectReference`s (5 vendors, 3 extensions, Test.Common, the two Mvc projects). Set `RootNamespace` in the Abstractions csproj. Update `InternalsVisibleTo("Jerrycurl.Razor")` in `Abstractions/AssemblyInfo.cs`.
- **Namespaces.** `Jerrycurl.Mvc*` → `Jerrycurl.Razor*` in about 208 files: 93 namespace declarations in the two projects, the `DomainExtensions.cs` files in the vendors and extensions, the vendor `Jerrycurl.Mvc.Sql.{Vendor}` → `Jerrycurl.Razor.Sql.{Vendor}`, and the tests. Use `git mv` plus `sed`, then build.
- **Hard-coded strings** that the compiler won't catch:
  - `src/Shared/Jerrycurl.Common/Facts/RazorFacts.cs` (default page imports)
  - `RazorSourceGenerator.cs` template
  - `RazorGenerator.cs` (`Annotations.Template` / `Inject`, `IProjection<`)
  - `src/Tools/Jerrycurl.Tools/Orm/OrmTool.cs:69`
  - `razor_skeleton.txt`
  - `.cssql` `@import Jerrycurl.Mvc.Sql.*` in the integration tests

  Afterwards, `grep -rn "Jerrycurl\.Mvc"` must return nothing outside git history.
- **Packaging.** `eng/nuspec/Jerrycurl.Mvc.nuspec` → `Jerrycurl.Razor.nuspec` (new id, description, glob `Jerrycurl.Razor.*`), and the dependency id in the 10 nuspecs that depend on it. Drop the `mvc` tag in `Directory.Build.props` `PackageTags`, and update the `Jerrycurl.nuspec` description.

### Phase 5: Docs and skills
- **`README.md`:** remove the "MVC" tagline, "MVC setup", "Accessor (controller) layer" and "Usage". Replace them with a `@query` / `@command` page plus a `Foo.QueryAsync(...)` call, the one-line rule from §1, and the visibility rule from §5.
- **`src/NuGet/Jerrycurl/.nusrc/skills/`:**
  - Replace `jerrycurl-mvc` with a `jerrycurl-pages` skill: page kinds, partials, templates, calling pages, `@visibility` and the wrapping pattern (§5), and DI via `ProcEngine.Default` or a per-call engine.
  - Update the `jerrycurl-queries`, `jerrycurl-commands`, `jerrycurl-razor-sql` (default usings, typed `@template`, `Subquery<T>`, no `_cssql`) and `projection-api.md` / `jerrycurl-models` namespaces.
- **`PERF_PLAN.md`:** strike or adjust the entries that target `Accessor` / `ProcLocator`.

---

## Risks and open points
- **Static `Execute` vs instance render.** Handled by renaming to `Render()`. This is a breaking change for any hand-written page subclass, which is acceptable in the relaunch.
- **Inheritance with `partial`.** If a user also writes `partial class Foo : Something`, the compiler reports a base-class conflict. Document this.
- **DI.** Static entry points mean DI is ambient (`ProcEngine.Default` / `ProcEngine.Services`), not per accessor instance. The wrapping pattern (§5) relies on a per-call engine, so add an optional `ProcEngine engine = null` parameter to every generated entry point (falling back to `ProcEngine.Default`). It is cheap to add in Phase 2, and it is the only thing an old accessor instance provided that statics don't.
- **Generator reach.** It stays text-only (no `CompilationProvider`). Typed refs are left to the C# compiler to validate, which keeps incremental caching cheap.

## Verification
- `dotnet build jerrycurl.slnx -c Release` (includes the netstandard2.0 `src/Build` projects) after every phase.
- `dotnet test` for the Razor (ex-Mvc), Cqs, Relations, Vendors.Sqlite and EFCore unit tests. Vendors.Sqlite runs on a local file with no server.
- **New generator end-to-end tests.** In `Jerrycurl.Razor.Test`, reference the generator as an analyzer and add real `.cssql` fixtures:
  - `@query` with `@input` / `@output` and defaults
  - `@command`
  - a partial with no kind, used through `Subquery<T>`
  - a typed `@template`
  - visibility: the default is `internal`; `@visibility public` in a page, in `_imports` (inherited by a subfolder and overridden by a page), and through the MSBuild property

  Call the generated statics against Sqlite. Add a small Roslyn `CSharpGeneratorDriver` test for each diagnostic `JC0001`–`JC0005`. Assert the emitted modifier in the visibility cases.
- **Final grep.** No `Jerrycurl.Mvc`, `Accessor`, `_cssql` or `FindPage` remains in `src/`, `test/`, `eng/`, the README or the skills.
- **Packaging.** `dotnet pack`, then check that the produced nupkg ids are `Jerrycurl.Razor` and that vendor packages depend on it.

---

## Implementation notes

Where the implementation differs from or adds to the plan above:

- **`IProcEngine`, not `ProcEngine`.** The optional per-call engine parameter and `ProcEngine.Default` are typed as `IProcEngine`. `IProcEngine.Describe(Type)` was added for this. `ProcEngine.Default` creates a fresh `ProcEngine` per access unless it was set explicitly, so a later `ProcEngine.Services` assignment still takes effect (all caches are static).
- **Unconstrained `<T>` overloads.** The generated `Query<T>` etc. and the `*Core` helpers have no `where T : TResult` constraint. That constraint can't be emitted for sealed or struct results (`@result string`, `@result int`) by a text-only generator. Incompatible types fail at runtime, as before.
- **Commands accept lists.** A `@command` with a typed model also gets `Execute<TList>(TList model) where TList : IEnumerable<TModel>`, so the documented `@model Blog` + `Execute(blogs)` pattern still compiles. It is generic so `Execute(new() { ... })` stays unambiguous. Queries don't get list overloads.
- **`JC0006`.** A page named after one of its own entry points (`Query.cssql` with `@query`) would hit CS0542 inside generated code, so the generator reports `JC0006` instead and emits no entry points.
- **`RenderBody()`.** Renaming `Render` → `Renderer` would have turned `@Render.Body()` into `@Renderer.Body()`, so `ProcPage.RenderBody()` was added (as in ASP.NET Razor). The docs use it.
- **Template cycles.** A page that inherits itself as `@template` (via `_imports.cssql`) skips it. A real cycle throws `ProcExecutionException`.
- **Generator tests.** They live in a separate `test/unit/src/Build/Jerrycurl.CodeAnalysis.Razor.Test` project (generator unit tests, Roslyn-compile checks and `CSharpGeneratorDriver` tests). The end-to-end `.cssql` fixtures run inside `Jerrycurl.Razor.Test`.
- **Test `Runner`.** Its hand-written pages are `RunnerQuery` / `RunnerCommand`, not `Query` / `Command`, because a class can't have a static method named after itself.
- **Dropped accessor features.** `One<T>` (use `.SingleOrDefault()`) and the `QueryReader` overloads of `Enumerate` have no generated equivalent yet.
