using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Jerrycurl.Tools.Orm.Model
{
    public class TupleModel
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

        public static async Task<IList<TupleModel>> FromDbCommandAsync(DbCommand command)
        {
            List<TupleModel> tuples = new List<TupleModel>();

            using (DbDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                do
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                        tuples.Add(new TupleModel(reader));
                }
                while (await reader.NextResultAsync().ConfigureAwait(false));
            }

            return tuples;
        }
    }
}
