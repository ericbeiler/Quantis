using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Visavi.Quantis.Data
{
    internal class Connections
    {
        private const string eventsHubName = "events";

        private readonly ILogger _logger;
        private readonly string? _storageConnectionString;
        private readonly string? _dbConnectionString;
        private readonly string? _signalRConnectionString;

        public Connections(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;

            _storageConnectionString = configuration["QuantisStorageConnection"];
            if (string.IsNullOrEmpty(_storageConnectionString))
            {
                _logger.LogError("QuantisStorageConnection value is missing. Is the environment variable set?");
            }

            _dbConnectionString = configuration["QuantisDbConnection"];
            if (string.IsNullOrEmpty(_dbConnectionString))
            {
                _logger.LogError("QuantisDbConnection value is missing. Is the environment variable set?");
            }

            _signalRConnectionString = configuration["AzureSignalRConnectionString"];
            if (string.IsNullOrEmpty(_signalRConnectionString))
            {
                _logger.LogError("AzureSignalRConnectionString value is missing. Is the environment variable set?");
            }
        }

        public string? StorageConnectionString => _storageConnectionString;
        public string? DbConnectionString => _dbConnectionString;
        public string? SignalRConnectionString => _signalRConnectionString;

        public ILogger Logger => _logger;

        public SqlConnection DbConnection => new SqlConnection(_dbConnectionString);

        public BlobServiceClient BlobConnection => new BlobServiceClient(_storageConnectionString);

        public QueueServiceClient QueueConnection => new QueueServiceClient(_storageConnectionString);

        public async Task<ServiceHubContext> EventHub()
        {
            // Create a ServiceManager using the updated API
            var serviceManager = new ServiceManagerBuilder()
                .WithOptions(option =>
                {
                    option.ConnectionString = _signalRConnectionString;
                })
                .BuildServiceManager(); // Updated method

            // Create a HubContext to send messages to the hub
            return await serviceManager.CreateHubContextAsync(eventsHubName, new CancellationToken());
        }
    }
}
