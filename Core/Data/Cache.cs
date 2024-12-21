using Dapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Visavi.Quantis.Data
{
    internal class Cache : ICacheService
    {
        private const int timeoutInSeconds = 120;
        private readonly Connections _connections;
        private readonly ILogger _logger;

        public Cache(Connections connections, ILogger logger)
        {
            _connections = connections;
        }

        public async Task<T?> Get<T>(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            key = key.ToLower();
            using var connection = _connections.DbConnection;
            {
                string? jsonValue = await connection.QueryFirstOrDefaultAsync<string>("SELECT [Value] FROM [Cache] WHERE [Key] = @key", new { key }, commandTimeout: timeoutInSeconds);
                return jsonValue != null ? JsonSerializer.Deserialize<T>(jsonValue) : default;
            }
        }

        public async Task Set(string key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            key = key.ToLower();
            string jsonValue = JsonSerializer.Serialize(value);
            using var connection = _connections.DbConnection;
            {
                await connection.ExecuteAsync("INSERT INTO [Cache] ([Key], [Value]) VALUES (@key, @jsonValue)", new { key, jsonValue }, commandTimeout: timeoutInSeconds);
            }
        }
    }
}
