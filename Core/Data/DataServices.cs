using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Visavi.Quantis.Data
{
    internal class DataServices : IDataServices
    {
        private readonly ILogger<DataServices> _logger;
        private readonly IConfiguration _configuration;
        private readonly Connections _connections;

        public DataServices(ILogger<DataServices> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _connections = new Connections(_logger, _configuration);
        }

        public IEquityArchives EquityArchives => new EquityArchives(_connections, _logger);

        public IPredictionModels PredictionModels => new PredictionModels(_connections, _logger);

        public string? DbConnectionString => _connections.DbConnectionString;

        public string? StorageConnectionString => _connections.StorageConnectionString;

        public BlobServiceClient BlobConnection => _connections.BlobConnection;

        public SqlConnection DbConnection => _connections.DbConnection;

        public QueueServiceClient QueueConnection => _connections.QueueConnection;
    }
}
