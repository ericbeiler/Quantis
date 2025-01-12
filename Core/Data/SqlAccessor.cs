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

        protected void ExecuteQuery(string query, object? param = null, bool eatExceptions = true)
        {
            using var dbConnection = _connections.DbConnection;
            {
                try
                {
                    dbConnection.Open();
                    dbConnection.Execute(query, param);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error running query:\n{ex.Message}\n{query}");
                }
            }
        }
    }
}
