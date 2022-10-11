using System.Collections.Generic;

namespace Jerrycurl.Mvc.Test.Project.Accessors
{
    public class CrudAccessor : Accessor
    {
        public IList<T> Get<T>() => this.Query<T>();

        public void Create<T>(IEnumerable<T> model) => this.Execute(model);
        public void CreateWithLiterals<T>(IEnumerable<T> model) => this.Execute(model);
        public void Update<T>(IEnumerable<T> model) => this.Execute(model);
        public void Delete<T>(IEnumerable<T> model) => this.Execute(model);

        public void Sql(string sql) => this.Execute(model: sql);
    }
}
