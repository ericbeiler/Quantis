using System;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Visavi.Quantis
{
    public class EquityQueueService
    {
        internal const string EquityQueueName = "quantis-equities";
        internal const string ReloadMessage = "Reload";
        internal const int maxRows = 200000;

        private readonly ILogger<EquityQueueService>? _logger;
        private const string usDerivedSharepricesDailySource = "us-derived-shareprices-daily.csv"; // Replace with your actual file name
        private static readonly List<string> _workingMessageList = new List<string>();
        private readonly string? _dbConnectionString;
        private readonly string? _storageConnectionString;

        public EquityQueueService(ILogger<EquityQueueService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _dbConnectionString = configuration["QuantisDbConnection"];
            if (string.IsNullOrEmpty(_dbConnectionString))
            {
                _logger?.LogError("QuantisDbConnection value is missing.");
            }

            _storageConnectionString = configuration["QuantisStorageConnection"];
            if (string.IsNullOrEmpty(_storageConnectionString))
            {
                _logger?.LogError("QuantisStorageConnection value is missing.");
            }
        }

        [Function(nameof(EquityQueueService))]
        public async Task Run([QueueTrigger(EquityQueueName, Connection = "QuantisStorageConnection")] QueueMessage message)
        {
            var startTime = DateTime.Now;
            var messageBody = message.Body.ToString();
            _logger?.LogInformation($"Processing Queue Message: {message.MessageId}, {messageBody}");
            bool alreadyLoading = true;
            lock (_workingMessageList)
            {
                if (!_workingMessageList.Contains(messageBody))
                {
                    _workingMessageList.Add(messageBody);
                    alreadyLoading = false;
                }
            }

            if (alreadyLoading)
            {
                _logger?.LogWarning($"Skipping redundant processing of message {messageBody}.");
                return;
            }

            try
            {
                var timeId = startTime.ToString("yyMMddHHmm");

                BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConnectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(EquityContainerService.EquityArchivesContainerName);
                BlobClient blobClient = containerClient.GetBlobClient(usDerivedSharepricesDailySource);
                foreach (var blob in containerClient.GetBlobs())
                {
                    if (blob.Name.StartsWith(EquityContainerService.WorkingSetPrefix))
                    {
                        _logger?.LogInformation($"Deleting Blob: {blob.Name}, Size: {blob.Properties.ContentLength}");
                        containerClient.DeleteBlobIfExists(blob.Name);
                    }
                }

                _logger?.LogInformation($" Blob Client: {blobClient.BlobContainerName}, URI: {blobClient.Uri}, Exists: {blobClient.Exists()}");
                var streamReader = new StreamReader(blobClient.OpenRead());
                string? columnHeaderLine = (await streamReader.ReadLineAsync()) + "\n";
                int workingCount = 1;
                while (!streamReader.EndOfStream)
                {
                    int lineCount = 0;
                    StringBuilder text = new StringBuilder(columnHeaderLine, 5000000);
                    while (!streamReader.EndOfStream && lineCount < maxRows)
                    {
                        string? line = await streamReader.ReadLineAsync();
                        text.AppendLine(line);
                        lineCount++;
                    }

                    string workingFilename = $"{EquityContainerService.WorkingSetPrefix}\\us-derived-shareprices-working-{timeId}-{workingCount}.csv";
                    _logger?.LogInformation($"Writing working file {workingFilename}");
                    BlobClient workingSetWriterClient = containerClient.GetBlobClient(workingFilename);
                    using (var workingSetStream = workingSetWriterClient.OpenWrite(true))
                    using (var workingSetWriter = new StreamWriter(workingSetStream))
                    {
                        await workingSetWriter.WriteAsync(text);
                        workingSetStream.Flush();
                        workingSetStream.Close();
                        workingCount++;
                    }
                }
            }
            finally
            {
                lock (_workingMessageList)
                {
                    _workingMessageList.Remove(messageBody);
                }
            }
        }
    }
}
