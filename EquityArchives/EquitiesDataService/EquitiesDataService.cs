using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Queues.Models;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Visavi.Quantis.EquitiesDataService
{
    public class EquitiesDataService
    {
        private readonly ILogger<EquitiesDataService> _logger;
        private readonly Connections _connections;

        // HTTP Requests
        private static readonly string reloadDaysParmName = "reloadDays";

        // Queue Requests
        internal const string EquityQueueName = "quantis-equities";
        private static readonly List<string> _workingMessageList = new List<string>();  // TODO: Scalable solution needed

        private const string loadEquitiesMessage = "LoadEquities";
        private const string updateYearlyReturnsMessage = "UpdateYearlyReturns";
        private const string exportTrainingDataMessage = "ExportTrainingData";

        // Container Management
        private const string usDerivedSharepricesDailySource = "us-derived-shareprices-daily.csv";
        internal const string EquityArchivesContainerName = "equity-archives";
        internal const int maxRows = 200000;
        private static readonly List<string> _workingFileList = new List<string>();  // TODO: Scalable solution needed
        internal const string WorkingSetPrefix = "workingset";

        public EquitiesDataService(ILogger<EquitiesDataService> logger, Connections connections)
        {
            _logger = logger;
            _connections = connections;
        }

        [Function("LoadEquities")]
        public async Task<IActionResult> HttpLoadEquities([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger?.LogInformation("Queuing Reload Equities Task.");
            uint? reloadDays = string.IsNullOrWhiteSpace(req.Query[reloadDaysParmName]) ? null : Convert.ToUInt32(req.Query[reloadDaysParmName]);
            var receipt = await sendEquitiesQueueMessage(new EquitiesQueueMessage
            {
                Message = loadEquitiesMessage,
                ReloadDays = reloadDays
            });
            var resultText = $"Reload Equities Queued: {receipt.Value.MessageId}";
            _logger?.LogInformation(resultText);
            return new OkObjectResult(resultText);
        }

        [Function("ExportTrainingData")]
        public async Task<IActionResult> HttpExportTrainingData([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger?.LogInformation("Queuing Export of Training Data.");
            List<int> simFinIds = new List<int>();
            using var connection = _connections.DbConnection;
            {
                simFinIds = (await connection.QueryAsync<int>("SELECT DISTINCT SimFinId FROM EquityHistory")).ToList();
            }
            foreach (int simFinId in simFinIds)
            {
                var receipt = await sendEquitiesQueueMessage(new EquitiesQueueMessage
                {
                    Message = exportTrainingDataMessage,
                    SimFinId = simFinId
                });
            }
            var resultText = $"Export Training Data Queued: {simFinIds.Count()} Total Equities";
            _logger?.LogInformation(resultText);
            return new OkObjectResult(resultText);
        }

        [Function("UpdateYearlyReturns")]
        public async Task<IActionResult> HttpUpdateYearlyReturns([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            int messagesQueued = 0;
            _logger?.LogInformation("Queuing Update Yearly Returns Task.");
            for (int year = 2000; year <= DateTime.Now.Year; year++)
            {
                for (int month = 1; month <= 12; month++)
                {
                    var receipt = await sendEquitiesQueueMessage(new EquitiesQueueMessage
                    {
                        Message = updateYearlyReturnsMessage,
                        StartDate = new DateTime(year, month, 1),
                        EndDate = new DateTime(year, month, 1).AddMonths(1)
                    });
                    _logger?.LogDebug($"Update Yearly Returns Queued: {receipt.Value.MessageId}");
                }
                messagesQueued++;
            }
            var resultText = $"Update Yearly Returns Queued: {messagesQueued} Total Messages";
            _logger?.LogInformation(resultText);
            return new OkObjectResult(resultText);
        }

        private async Task parseEquitiesFile(uint? reloadDays)
        {
            var startTime = DateTime.Now;
            BlobServiceClient blobServiceClient = _connections.BlobConnection;
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(EquityArchivesContainerName);
            BlobClient blobClient = containerClient.GetBlobClient(usDerivedSharepricesDailySource);
            foreach (var blob in containerClient.GetBlobs())
            {
                if (blob.Name.StartsWith(WorkingSetPrefix))
                {
                    _logger?.LogInformation($"Deleting Blob: {blob.Name}, Size: {blob.Properties.ContentLength}");
                    containerClient.DeleteBlobIfExists(blob.Name);
                }
            }

            _logger?.LogInformation($" Blob Client: {blobClient.BlobContainerName}, URI: {blobClient.Uri}, Exists: {blobClient.Exists()}");
            var streamReader = new StreamReader(blobClient.OpenRead());
            string? columnHeaderLine = await streamReader.ReadLineAsync() + "\n";
            _logger?.LogInformation($" Header: {columnHeaderLine}");
            int workingCount = 1;

            // Determine the index of the Date column
            var columns = columnHeaderLine.Split(DerivedSharepriceFileLoader.FileDelimiter);
            int dateColumnIndex = Array.IndexOf(columns, "Date");
            _logger?.LogInformation($" Days to Reload: {reloadDays}, Date Column Index {dateColumnIndex}");

            var timeId = startTime.ToString("yyMMddHHmm");
            while (!streamReader.EndOfStream)
            {
                int lineCount = 0;
                StringBuilder text = new StringBuilder(columnHeaderLine, 5000000);
                while (!streamReader.EndOfStream && lineCount < maxRows)
                {
                    var appendLine = true; // default to appending line in working set
                    string? line = await streamReader.ReadLineAsync();
                    try
                    {
                        if (reloadDays != null && line?.Split(DerivedSharepriceFileLoader.FileDelimiter)[dateColumnIndex].Length > 0)
                        {
                            string dateString = line.Split(DerivedSharepriceFileLoader.FileDelimiter)[dateColumnIndex];
                            if (!string.IsNullOrWhiteSpace(dateString))
                            {
                                DateTime date = DateTime.Parse(line.Split(DerivedSharepriceFileLoader.FileDelimiter)[dateColumnIndex]);
                                appendLine = (DateTime.Now - date).TotalDays <= reloadDays;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning($"Error parsing date from line: {line}, Date Column Index: {dateColumnIndex}, {ex.Message}");
                    }
                    finally
                    {
                        if (appendLine)
                        {
                            text.AppendLine(line);
                            lineCount++;
                        }
                    }
                }

                string workingFilename = $"{WorkingSetPrefix}\\us-derived-shareprices-working-{timeId}-{workingCount}.csv";
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
            _logger?.LogInformation($" Completed reading file {usDerivedSharepricesDailySource}, Duration: {DateTime.Now - startTime}");
        }

        [Function("ProcessEquitiesQueue")]
        public async Task ProcessEquitiesQueue([QueueTrigger(EquityQueueName, Connection = "QuantisStorageConnection")] QueueMessage message)
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
                // Deserialize the message body to get the ReloadDays
                var queueMessage = JsonSerializer.Deserialize<EquitiesQueueMessage>(messageBody);
                switch (queueMessage?.Message)
                {
                    case loadEquitiesMessage:
                        await parseEquitiesFile(queueMessage?.ReloadDays);
                        break;

                    case updateYearlyReturnsMessage:
                        if (queueMessage?.StartDate.HasValue == true && queueMessage?.EndDate.HasValue == true)
                        {
                            await updateYearlyReturns(queueMessage.StartDate.Value, queueMessage.EndDate.Value);
                        }
                        break;

                    case exportTrainingDataMessage:

                        await new TrainingDataExport(_connections).ExportTrainingDataAsync(queueMessage.SimFinId.Value);
                        break;
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
                    recordCount = await new DerivedSharepriceFileLoader(_connections).LoadRecords(stream);

                    _logger?.LogInformation($"Processed {name} in {(int)Math.Ceiling((DateTime.Now - startTime).TotalMilliseconds)} ms, Records Processed: {recordCount}\n  Removing {name}");
                    BlobServiceClient blobServiceClient = _connections.BlobConnection;
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

        private async Task<Response<SendReceipt>> sendEquitiesQueueMessage(EquitiesQueueMessage message)
        {
            var queueServiceClient = _connections.QueueConnection;
            var queueClient = queueServiceClient.GetQueueClient(EquityQueueName);
            await queueClient.CreateIfNotExistsAsync();

            string jsonMessage = JsonSerializer.Serialize(message);
            return await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonMessage)));
        }

        private async Task<int> updateYearlyReturns(DateTime startDate, DateTime endDate)
        {
            _logger?.LogInformation($"Updating Yearly Returns for {startDate} - {endDate}");
            int rowsUpdated = await new YearlyReturns(_logger).UpdateYearlyReturns(
                new DateOnly(startDate.Year, startDate.Month, 1),
                new DateOnly(endDate.Year, endDate.Month, 1));

            return rowsUpdated;
        }
    }

    public class EquitiesQueueMessage
    {
        public string Message { get; set; }
        public uint? ReloadDays { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? SimFinId { get; set; }
    }
}
