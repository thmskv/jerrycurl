using Jerrycurl.Mvc.Metadata.Annotations;
using Jerrycurl.Mvc.Projections;

namespace Jerrycurl.Mvc.Test.Project.Queries.Misc;

[Template(typeof(CycleB))]
public class CycleA(IProjection model, IProjection result) : ProcPage<object, object>(model, result);

[Template(typeof(CycleA))]
public class CycleB(IProjection model, IProjection result) : ProcPage<object, object>(model, result);
