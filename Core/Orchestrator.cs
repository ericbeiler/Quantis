using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Visavi.Quantis.Data;
using Visavi.Quantis.Events;

namespace Visavi.Quantis
{
    internal class Orchestrator : IOrchestrator
    {
        private readonly ILogger<Orchestrator> _logger;
        private readonly IConfiguration _configuration;
        private readonly Connections _connections;

        public Orchestrator(ILogger<Orchestrator> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _connections = new Connections(_logger, _configuration);
        }

        public ICacheService Cache => new Cache(_connections, _logger);
        public IEquityArchives EquityArchives => new EquityArchives(_connections, _logger);

        public IPredictionModels PredictionModels => new PredictionModels(_connections, EventService, _logger);
        public IEventService EventService => new EventService(_connections, _logger);

        public string? DbConnectionString => _connections.DbConnectionString;

        public string? StorageConnectionString => _connections.StorageConnectionString;

        public BlobServiceClient BlobConnection => _connections.BlobConnection;

        public SqlConnection DbConnection => _connections.DbConnection;

        public QueueServiceClient QueueConnection => _connections.QueueConnection;
        public string ModelingQueue => Connections.QuantisModelingQueue;
    }
}
