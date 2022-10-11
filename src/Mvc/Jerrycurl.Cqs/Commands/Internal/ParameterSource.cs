using System;
using System.Data;
using Jerrycurl.Cqs.Sessions;

namespace Jerrycurl.Cqs.Commands.Internal
{
    internal class ParameterSource : IFieldSource
    {
        public IDbDataParameter AdoParameter { get; set; }
        public IParameter Parameter { get; set; }
        public bool HasSource => (this.Parameter != null);
        public bool HasTarget { get; set; }
        public bool HasChanged => this.HasTarget;
        public string Name { get; set; }

        public object Value
        {
            get
            {
                if (this.AdoParameter == null)
                    return DBNull.Value;

                return this.AdoParameter.Value;
            }
        }
    }
}
