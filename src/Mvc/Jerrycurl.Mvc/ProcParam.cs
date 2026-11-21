using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Cqs.Queries;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Mvc.Sql;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Language;

namespace Jerrycurl.Mvc;

public union ProcParam<T>(T, IProjection<T>);