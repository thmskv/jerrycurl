using System.Collections.Generic;
using Jerrycurl.Razor.Test.Project.Commands.Crud;
using Jerrycurl.Razor.Test.Project.Queries.Crud;

namespace Jerrycurl.Razor.Test.Project.Accessors;

/// <summary>
/// The accessor pattern: a plain class with domain-shaped methods that wraps internal pages. No base class needed.
/// </summary>
public class CrudAccessor(IProcEngine engine = null)
{
    public IList<T> Get<T>() => Queries.Crud.Get.Query<T>(engine: engine);

    public void Create<T>(IEnumerable<T> model) => Commands.Crud.Create.Execute(model, engine: engine);
    public void CreateWithLiterals<T>(IEnumerable<T> model) => Commands.Crud.CreateWithLiterals.Execute(model, engine: engine);
    public void Update<T>(IEnumerable<T> model) => Commands.Crud.Update.Execute(model, engine: engine);
    public void Delete<T>(IEnumerable<T> model) => Commands.Crud.Delete.Execute(model, engine: engine);

    public void Sql(string sql) => Commands.Crud.Sql.Execute(sql, engine: engine);
}
