using System;
using System.Data;

namespace Jerrycurl.Cqs.Queries.Internal.Compilation
{
    internal class ListFactory
    {
        public Action<IQueryBuffer> Initialize { get; set; }
        public Action<IQueryBuffer, IDataReader> WriteAll { get; set; }
        public Action<IQueryBuffer, IDataReader> WriteOne { get; set; }
    }
}
