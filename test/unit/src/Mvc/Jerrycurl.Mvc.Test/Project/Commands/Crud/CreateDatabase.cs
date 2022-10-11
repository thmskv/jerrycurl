using System;
using Jerrycurl.Mvc.Projections;

namespace Jerrycurl.Mvc.Test.Project.Queries.Crud
{
    public class CreateDatabase_cssql : ProcPage<dynamic, object>
    {
        public CreateDatabase_cssql(IProjection model, IProjection result)
            : base(model, result)
        {

        }

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
