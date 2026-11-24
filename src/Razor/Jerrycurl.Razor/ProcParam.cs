using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Cqs.Queries;
using Jerrycurl.Razor.Metadata;
using Jerrycurl.Razor.Projections;
using Jerrycurl.Razor.Sql;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Language;

namespace Jerrycurl.Razor;

public union ProcParam<T>(T, IProjection<T>);