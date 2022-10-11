using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Relations;

namespace Jerrycurl.Cqs.Language
{
    public class ParameterStore : Collection<IParameter>
    {
        private readonly Dictionary<IField, IParameter> innerMap = new Dictionary<IField, IParameter>();

        public char? Prefix { get; }

        public ParameterStore(char? prefix = null)
        {
            this.Prefix = prefix;
        }

        public IParameter Add(IField field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            if (!this.innerMap.TryGetValue(field, out IParameter param))
            {
                string paramName = $"{this.Prefix}P{this.innerMap.Count}";

                this.innerMap.Add(field, param = new Parameter(paramName, field));
                this.Add(param);
            }

            return param;
        }

        public IList<IParameter> Add(ITuple tuple)
            => tuple?.Select(this.Add).ToList() ?? throw new ArgumentNullException(nameof(tuple));

        public IList<IParameter> Add(IRelation relation)
        {
            if (relation == null)
                throw new ArgumentNullException(nameof(relation));

            using IRelationReader reader = relation.GetReader();

            List<IParameter> parameters = new List<IParameter>();

            while (reader.Read())
                parameters.AddRange(this.Add(reader));

            return parameters;
        }
    }
}
