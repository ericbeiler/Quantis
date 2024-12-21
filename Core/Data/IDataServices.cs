using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Data.SqlClient;

namespace Visavi.Quantis.Data
{
    public interface IDataServices
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
    }
}
