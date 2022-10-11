using Jerrycurl.Mvc.Projections;

namespace Jerrycurl.Mvc.Test.Project.Queries.Crud
{
    public class Sql_cssql : ProcPage<string, object>
    {
        public Sql_cssql(IProjection model, IProjection result)
            : base(model, result)
        {

        }

        public override void Execute() => this.WriteLiteral(this.Model);
    }
}
