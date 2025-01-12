using Dapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Visavi.Quantis.Data
{
    internal class Cache : SqlAccessor, ICacheService
    {
        private const int timeoutInSeconds = 120;
        private const string createCacheTableQuery = @"
                                CREATE TABLE [dbo].[Cache] (
                                    [Key]     NVARCHAR (50) NOT NULL,
                                    [Value]   JSON          NULL,
                                    [Created] DATETIME      CONSTRAINT [DEFAULT_Cache_Created] DEFAULT (getdate()) NULL,
                                    CONSTRAINT [PK_Cache] PRIMARY KEY CLUSTERED ([Key] ASC)
                                );";

        public Cache(Connections connections, ILogger logger) : base(connections, logger)
        {
            ExecuteQuery(createCacheTableQuery);
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
            _logger.LogInformation($"Setting cache value for key {key} to {jsonValue}");
            using var connection = _connections.DbConnection;
            {
                await connection.ExecuteAsync("INSERT INTO [Cache] ([Key], [Value]) VALUES (@key, @jsonValue)", new { key, jsonValue }, commandTimeout: timeoutInSeconds);
            }
            _logger.LogInformation($"Set cache value for key {key}");
        }
    }
}
