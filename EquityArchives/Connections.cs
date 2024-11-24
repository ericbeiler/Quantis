using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Visavi.Quantis
{
    public class Connections
    {
        private readonly ILogger<Connections> _logger;
        private readonly string? _storageConnectionString;
        private readonly string? _dbConnectionString;

        public Connections(ILogger<Connections> logger, IConfiguration configuration)
        {
            _logger = logger;

            _storageConnectionString = configuration["QuantisStorageConnection"];
            if (string.IsNullOrEmpty(_storageConnectionString))
            {
                _logger.LogError("QuantisStorageConnection value is missing.");
            }

            _dbConnectionString = configuration["QuantisDbConnection"];
            if (string.IsNullOrEmpty(_dbConnectionString))
            {
                _logger.LogError("QuantisDbConnection value is missing.");
            }
        }

        public string? StorageConnectionString => _storageConnectionString;
        public string? DbConnectionString => _dbConnectionString;

        public ILogger Logger => _logger;

        public SqlConnection DbConnection => new SqlConnection(_dbConnectionString);

        public BlobServiceClient BlobConnection => new BlobServiceClient(_storageConnectionString);

        public QueueServiceClient QueueConnection => new QueueServiceClient(_storageConnectionString);
    }
}
