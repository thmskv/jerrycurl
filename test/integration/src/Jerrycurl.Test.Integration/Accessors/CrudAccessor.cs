using System;
using System.Collections.Generic;
using Jerrycurl.Test.Integration.Commands.Shared;
using Jerrycurl.Test.Integration.Queries.Shared;
using Jerrycurl.Test.Integration.Views;

namespace Jerrycurl.Test.Integration.Accessors
{
    /// <summary>
    /// The accessor pattern: a plain public class with domain-shaped methods wrapping the (internal) pages.
    /// </summary>
    public class CrudAccessor
    {
        public IList<T> Get<T>() => Queries.Shared.Get.Query<T>();

#if VENDOR_ORACLE
        public void Create<T>(T model) => Create_Oracle.Execute(model);
        public void Update<T>(T model) => Update_Oracle.Execute(model);
        public void Delete<T>(T model) => Delete_Oracle.Execute(model);
        public void Clear() => Clear_Oracle.Execute();
        public IList<DatabaseView> GetDatabaseView() => GetDatabaseView_Oracle.Query();
#else
        public void Create<T>(T model) => Commands.Shared.Create.Execute(model);
        public void Update<T>(T model) => Commands.Shared.Update.Execute(model);
        public void Delete<T>(T model) => Commands.Shared.Delete.Execute(model);
        public void Clear() => Commands.Shared.Clear.Execute();
        public IList<DatabaseView> GetDatabaseView() => Queries.Shared.GetDatabaseView.Query();
#endif

        public void VerifyImport()
        {
            var result = Queries.Shared.VerifyImport.Query<int>();

            if (result.Count == 0)
                throw new InvalidOperationException();
        }
    }
}
