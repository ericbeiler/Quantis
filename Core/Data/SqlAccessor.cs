using Dapper;
using Microsoft.Extensions.Logging;

namespace Visavi.Quantis.Data
{
    internal class SqlAccessor
    {
        protected ILogger _logger;
        protected Connections _connections;

        protected SqlAccessor(Connections connections, ILogger logger)
        {
            _logger = logger;
            _connections = connections;
        }

        protected int ExecuteQuery(string query, object? param = null, bool eatExceptions = true)
        {
            using var dbConnection = _connections.DbConnection;
            {
                try
                {
                    dbConnection.Open();
                    return dbConnection.Execute(query, param);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error running query:\n{ex.Message}\n{query}");
                    return -1;
                }
            }
        }

        protected bool TableExists(string tableName)
        {
            return ExecuteQuery($"\"SELECT * FROM sys.objects  WHERE object_id = OBJECT_ID('{tableName}') AND type = 'U'\"") > 0;
        }
    }
}
