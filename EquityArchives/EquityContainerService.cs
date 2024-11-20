using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Visavi.Quantis
{
    public class EquityContainerService
    {
        private readonly ILogger<EquityContainerService>? _logger;
        private static readonly List<string> _workingFileList = new List<string>();
        private readonly string? _storageConnectionString;
        internal const string EquityArchivesContainerName = "equity-archives";
        internal const string WorkingSetPrefix = "workingset";

        public EquityContainerService(ILogger<EquityContainerService> logger, IConfiguration configuration)
        {
            _logger = logger;

            _storageConnectionString = configuration["QuantisStorageConnection"];
            if (string.IsNullOrEmpty(_storageConnectionString))
            {
                _logger?.LogError("QuantisStorageConnection value is missing.");
            }
        }

        [Function(nameof(EquityContainerService))]
        public async Task Run([BlobTrigger(EquityArchivesContainerName + "/{name}", Connection = "QuantisStorageConnection")] Stream stream, FunctionContext context)
        {
            // TODO: Implement the logic to process the blob.
            // var recordCount = await new DerivedSharepriceFileLoader(_logger).LoadRecords(stream);
            // _logger?.LogInformation($"C# Blob trigger function Processed blob\n Records Processed: {recordCount}");
        }

        [Function("ProcessWorkingEquityFile")]
        public async Task<int> ProcessWorkingEquityFile([BlobTrigger(EquityArchivesContainerName + "/" + WorkingSetPrefix + "/{name}", Connection = "QuantisStorageConnection")] Stream stream, string name, FunctionContext context)
        {
            DateTime startTime = DateTime.Now;
            bool alreadyLoading = true;
            lock (_workingFileList)
            {
                if (!_workingFileList.Contains(name))
                {
                    _workingFileList.Add(name);
                    alreadyLoading = false;
                }
            }

            if (alreadyLoading)
            {
                _logger?.LogWarning($"Skipping redundant processing working file {name}.");
                return 0;
            }

            try
            {
                int recordCount = 0;
                if (stream.Length > 0)
                {
                    _logger?.LogInformation($"Received working file {name}");
                    recordCount = await new DerivedSharepriceFileLoader(_logger).LoadRecords(stream);

                    _logger?.LogInformation($"Processed {name} in {(int)Math.Ceiling((DateTime.Now - startTime).TotalMilliseconds)} ms, Records Processed: {recordCount}\n  Removing {name}");
                    BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConnectionString);
                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(EquityArchivesContainerName);
                    await containerClient.DeleteBlobIfExistsAsync("/" + WorkingSetPrefix + "/" + name);
                }
                else
                {
                    _logger?.LogWarning($"Skipping processing of empty file {name}.");
                }

                return recordCount;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error occurred processing working file {name}");
                throw;
            }
            finally
            {
                lock (_workingFileList)
                {
                    _workingFileList.Remove(name);
                }
            }
        }
    }
}
