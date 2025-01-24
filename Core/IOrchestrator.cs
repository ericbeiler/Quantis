using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Data.SqlClient;
using Visavi.Quantis.Data;
using Visavi.Quantis.Events;

namespace Visavi.Quantis
{
    public interface IOrchestrator
    {
        // Connection Strings
        string? DbConnectionString { get; }
        string? StorageConnectionString { get; }

        // Raw Connections
        BlobServiceClient BlobConnection { get; }
        SqlConnection DbConnection { get; }
        QueueServiceClient QueueConnection { get; }

        // Repositories
        ICacheService Cache { get; }
        IEquityArchives EquityArchives { get; }
        IPredictionModels PredictionModels { get; }

        // Events
        IEventService EventService { get; }

        // Queues
        string ModelingQueue { get; }
    }
}
