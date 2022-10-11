using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Queries;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Language
{
    public static class RelationExtensions
    {
        public static ISchema Describe<T>(this ISchemaStore store)
            => store.GetSchema(typeof(T));

        public static QueryBuffer AsBuffer(this ISchema schema, QueryType type)
            => new QueryBuffer(schema, type);

        public static Query AsQuery(this IRelation relation, string queryText)
        {
            return new Query()
            {
                QueryText = queryText,
                Parameters = relation.AsParameters()
            };
        }

        public static Command AsCommand(this IRelation relation, string commandText)
        {
            return new Command()
            {
                CommandText = commandText,
                Parameters = relation.AsParameters()
            };
        }

        public static Command AsCommand(this IRelation relation, Func<IList<IParameter>, string> textBuilder)
        {
            ParameterStore store = new ParameterStore();

            Command command = new Command()
            {
                Parameters = store,
            };

            using var reader = relation.GetReader();

            while (reader.Read())
            {
                IList<IParameter> parameters = store.Add(reader);

                command.CommandText += textBuilder(parameters) + Environment.NewLine;
            }

            return command;
        }

        public static DbDataReader As(this IRelation relation, IEnumerable<string> header)
            => relation.GetDataReader(header);

        public static DbDataReader As(this IRelation relation, params string[] header)
            => relation.GetDataReader(header);

        public static IList<IParameter> AsParameters(this IField source, params string[] header)
            => source.Select(header).AsParameters();

        public static IList<IParameter> AsParameters(this IField source, IEnumerable<string> header)
            => source.Select(header).AsParameters();

        public static IList<IParameter> AsParameters(this ITuple tuple)
            => new ParameterStore().Add(tuple);

        public static IList<IParameter> AsParameters(this IRelation relation)
            => new ParameterStore().Add(relation);

        public static IList<IDbDataParameter> AddParameters(this ITuple tuple, IDbCommand dbCommand)
        {
            List<IDbDataParameter> dbParameters = new List<IDbDataParameter>();

            foreach (IParameter parameter in tuple.AsParameters())
            {
                IDbDataParameter dbParameter = dbCommand.CreateParameter();

                parameter.Build(dbParameter);
                dbParameters.Add(dbParameter);

                dbCommand.Parameters.Add(dbParameter);
            }

            return dbParameters;
        }

        public static IList<IDbDataParameter> AddParameters(this IRelation relation, IDbCommand dbCommand)
            => relation.Body.SelectMany(t => t.AddParameters(dbCommand)).ToList();

        public static IParameter AsParameter(this IField field, string parameterName)
            => new Parameter(parameterName, field);

        public static IEnumerable<ColumnBinding> AsBindings(this IRelation targets)
        {
            if (targets == null)
                throw new ArgumentNullException(nameof(targets));

            using IRelationReader reader = targets.GetReader();

            List<ColumnBinding> bindings = new List<ColumnBinding>();

            while (reader.Read())
                bindings.AddRange(reader.AsBindings());

            return bindings;
        }
        public static IEnumerable<ColumnBinding> AsBindings(this ITuple targets)
        {
            if (targets == null)
                throw new ArgumentNullException(nameof(targets));

            return targets.Select(t => new ColumnBinding(t));
        }
    }
}
