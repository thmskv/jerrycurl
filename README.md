![Jerrycurl](gfx/icon.png)

[![NuGet](https://img.shields.io/nuget/v/Jerrycurl)](https://nuget.org/packages/Jerrycurl)
[![Build status](https://github.com/thmskv/jerrycurl/actions/workflows/build.yml/badge.svg)](https://github.com/thmskv/jerrycurl/actions)
[![Test status](https://img.shields.io/appveyor/tests/rhodosaur/jerrycurl/dev)](https://ci.appveyor.com/project/rhodosaur/jerrycurl/branch/master/tests)
[![Gitter chat](https://badges.gitter.im/gitterHQ/gitter.png)](https://gitter.im/jerrycurl-mvc/community)

## Jerrycurl - High-performance ORM and Razor SQL for .NET
**Jerrycurl** is an object-relational mapper and Razor SQL implementation that lets you write data access for .NET as typed SQL pages, using tools and features inspired by ASP.NET Razor Pages.

### Installation
Jerrycurl is available on NuGet and can be installed into any [SDK-style](https://docs.microsoft.com/en-us/nuget/resources/check-project-format) C# project. Its main package compiles `.cssql` files into your project and runs them with the built-in Razor SQL engine. Install support for [one of our supported databases](https://nuget.org/packages?q=Jerrycurl.Vendors) as well.

```shell
> dotnet add package Jerrycurl
> dotnet add package Jerrycurl.Vendors.SqlServer
```

#### Tooling
You can generate a basic object model from your database schema by installing and invoking our [CLI](https://www.nuget.org/packages/dotnet-jerry/).
```shell
> dotnet tool install --global dotnet-jerry
> jerry scaffold -v sqlserver -c "DATABASE=blogdb;..." -ns BlogDb.Database
Connecting to database 'blogdb'...
Generating...
Generated 7 tables and 21 columns in Database.cs.
```
To learn more about the CLI, type in `jerry help`.

### Getting started
A Jerrycurl project has three parts: **models** describing your tables, **pages** (`.cssql` files) containing your SQL, and a **domain** that configures the database connection.

#### Models
Models are the classes generated with the CLI above, in the familiar class-per-table style.

```csharp
// Database.cs
[Table("dbo", "Blog")]
class Blog
{
    [Id, Key("PK_Blog")]
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime CreatedOn { get; set; }
}
//...
```

Combine them into *views* with plain composition: unary properties map one-to-one and lists map one-to-many.

```csharp
// Views/BlogView.cs
class BlogView : Blog
{
    public BlogAuthor Author { get; set; }
    public IList<BlogPost> Posts { get; set; }
}
```

#### Pages
Pages are where you write SQL, with Razor syntax, in `.cssql` files. Like a Razor Page with `@page`, a page becomes callable from C# by declaring what it is:

> Add `@query` or `@command` to make a page callable from C#. Without one, it's a building block for other pages.

`@model` (or `@input`) declares the page's input and `@result` (or `@output`) its output:

```sql
-- Queries/Blogs/GetAll.cssql
@query
@result BlogView
@input DateTime FromDate
@{
    var p = R.Open(m => m.Posts);
}

SELECT      @R.Map(),
            @R.Map(m => m.Author)
FROM        @R.Tbl()
INNER JOIN  @R.Tbl(m => m.Author) ON @R.Col(m => m.Author.BlogId) = @R.Col(m => m.Id)
WHERE       @R.Col(m => m.CreatedOn) >= @M.Par(m => m.FromDate)

SELECT      @p.Map()
FROM        @p.Tbl()
INNER JOIN  @R.Tbl() ON @R.Col(m => m.Id) = @p.Col(m => m.BlogId)
WHERE       @R.Col(m => m.CreatedOn) >= @M.Par(m => m.FromDate)
```
```sql
-- Commands/Blogs/AddBlogs.cssql
@command
@model Blog

@foreach (var v in M.Vals())
{
    INSERT INTO @v.TblName() ( @v.In().ColNames() )
    OUTPUT      @v.Out().Cols("INSERTED").As().Props()
    VALUES                   ( @v.In().Pars() )
}
```

Each file compiles into a class named after the file, in your root namespace plus its folder path, with static methods for running it:

```csharp
IList<BlogView> blogs = await GetAll.QueryAsync(new() { FromDate = DateTime.Today.AddDays(-7) });

AddBlogs.Execute(newBlogs); // generated ids are written back into newBlogs
```

`Queries/` and `Commands/` are only a recommended layout. Pages refer to each other by type (`@template Shared.Paged`, `@R.Subquery<Shared.Filter>(...)`), so renames are refactor-safe and typos are compile errors.

#### Visibility and the accessor pattern
Pages are `internal` by default. Call them directly inside your assembly. To expose data access from a library, write a public class that wraps them, using public `@result` models:

```csharp
public class BlogAccessor(IProcEngine engine = null)
{
    public async Task<IList<BlogView>> GetRecentAsync(CancellationToken ct = default)
        => await GetAll.QueryAsync(new() { FromDate = DateTime.Today.AddDays(-7) }, engine: engine, cancellationToken: ct);
}
```

Alternatively, make pages public with `@visibility public` on a page or in an `_imports.cssql`, or for a whole project with `<JerrycurlPageVisibility>public</JerrycurlPageVisibility>`.

#### Domain
A domain configures the database connection for every page in its namespace and below.
```csharp
// BlogsDomain.cs
class BlogsDomain : IDomain
{
    public void Configure(DomainOptions options)
    {
        options.UseSqlServer("SERVER=.;DATABASE=blogdb;TRUSTED_CONNECTION=true");
    }
}
```

To learn more about Jerrycurl and how to get started, visit [our official site](https://jerrycurl.net).

## Building from source
Jerrycurl can be built on [any OS supported by .NET Core](https://docs.microsoft.com/en-us/dotnet/core/install/dependencies) and included in this repository is a [PowerShell script](build.ps1) that performs all build-related tasks.

### Prerequisites
* .NET SDK 10.0
* PowerShell 7+
* Visual Studio 2026 or VS Code with C# Dev Kit (optional)
* Docker (to live test databases) (optional)

A [dev container](.devcontainer/devcontainer.json) with the .NET 10 SDK, PowerShell, C# Dev Kit and Claude Code is included for VS Code and GitHub Codespaces.

### Clone, Build and Test
Clone the repository and run our build script from PowerShell.
```powershell
PS> git clone https://github.com/rhodosaur/jerrycurl
PS> cd jerrycurl
PS> .\build.ps1 [-NoTest] [-NoPack]
```

This runs the `Restore`, `Clean`, `Build`, `[Test]` and `[Pack]` targets on `jerrycurl.slnx` and places NuGet packages in `/artifacts/packages`. Each target can also be run manually, and in Visual Studio if preferred.

By default, the `Test` target skips any unit test that requires live running database server. To help you to include these, you can run our [`docker compose` script](test/tools/boot-dbs.ps1) to boot up instances of our supported databases.

```powershell
PS> .\test\tools\boot-dbs.ps1 up sqlserver,mysql,postgres,oracle
```

Please allow ~60 seconds for the databases to be ready after which you can re-run `build.ps1`; it will then automatically target the included databases instances. When done, you can tear everything down again.

```powershell
PS> .\test\tools\boot-dbs.ps1 down sqlserver,mysql,postgres,oracle
```

> If you already have an empty database running that can be used for testing, you can manually specify its connection string in the environment variable `JERRY_SQLSERVER_CONN`, `JERRY_MYSQL_CONN`, `JERRY_POSTGRES_CONN` or `JERRY_ORACLE_CONN`.

> Pulling the Oracle Database image requires that you are logged into Docker and have accepted their [terms of service](https://hub.docker.com/_/oracle-database-enterprise-edition).
