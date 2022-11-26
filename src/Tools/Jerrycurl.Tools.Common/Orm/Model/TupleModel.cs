using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Jerrycurl.Tools.Orm.Model
{
    public class TupleModel : IEnumerable<KeyValuePair<string, object>>
    {
        private readonly Dictionary<string, object> map;

        public TupleModel(IDataReader dataReader)
        {
            if (dataReader == null)
                throw new ArgumentNullException(nameof(dataReader));

            this.map = new Dictionary<string, object>();

            this.InitializeData(dataReader);
        }

        private void InitializeData(IDataReader dataReader)
        {
            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                string key = dataReader.GetName(i);

                if (!this.map.ContainsKey(key))
                    this.map.Add(key, dataReader.GetValue(i));
            }
        }

        public object this[string name]
        {
            get
            {
                if (this.map.TryGetValue(name, out object value))
                    return value == DBNull.Value ? null : value;

                return null;
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => this.map.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
