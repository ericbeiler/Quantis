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

        protected async Task<IEnumerable<dynamic>?> ExecuteQuery(string query, object? param = null, bool eatExceptions = true)
        {
            using var dbConnection = _connections.DbConnection;
            {
                try
                {
                    dbConnection.Open();
                    return await dbConnection.QueryAsync(query, param);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error running query:\n{ex.Message}\n{query}");
                    if (!eatExceptions)
                    {
                        throw;
                    }
                    return null;
                }
            }
        }

        protected async Task<bool> TableExists(string tableName)
        {
            var tables = await ExecuteQuery($"SELECT * FROM sys.objects  WHERE object_id = OBJECT_ID('{tableName}') AND type = 'U'");
            return tables != null && tables.Count() > 0;
        }
    }
}
