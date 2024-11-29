using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Queues.Models;
using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Visavi.Quantis.Data
{
    public class DataService
    {
        internal const string ContainerFolderSeperator = "/";
        private readonly ILogger<DataService> _logger;
        private readonly Connections _connections;

        // Queue Requests
        internal const string EquityQueueName = "quantis-equities";
        private static readonly List<string> _workingMessageList = new List<string>();  // TODO: Scalable solution needed

        internal const string LoadEquitiesMessage = "LoadEquities";
        internal const string UpdateYearlyReturnsMessage = "UpdateYearlyReturns";
        internal const string ExportTrainingDataMessage = "ExportTrainingData";

        // Container Management
        private const string usDerivedSharepricesDailySource = "us-derived-shareprices-daily.csv";
        internal const string EquityArchivesContainerName = "equity-archives";
        internal const int maxRows = 200000;
        private static readonly List<string> _workingFileList = new List<string>();  // TODO: Scalable solution needed
        internal const string WorkingSetPrefix = "workingset";

        public DataService(ILogger<DataService> logger, Connections connections)
        {
            _logger = logger;
            _connections = connections;
        }

        private async Task<DateTime> getEquitiesLastUpdate()
        {
            using var connection = _connections.DbConnection;
            return (await connection.QueryAsync<DateTime>("Select Top 1 [Date] from EquityHistory order by [Date] desc")).FirstOrDefault();
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
                    case LoadEquitiesMessage:
                        await parseEquitiesFile(queueMessage?.ReloadDays);
                        break;

                    case UpdateYearlyReturnsMessage:
                        if (queueMessage?.StartDate.HasValue == true && queueMessage?.EndDate.HasValue == true)
                        {
                            await updateYearlyReturns(queueMessage.StartDate.Value, queueMessage.EndDate.Value);
                        }
                        break;

                    case ExportTrainingDataMessage:

                        await new TrainingDataExport(_connections).ExportTrainingDataAsync(queueMessage.SimFinId.Value,
                                                                                            queueMessage.EndDate.Value,
                                                                                            queueMessage.OutputPath);
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
}
