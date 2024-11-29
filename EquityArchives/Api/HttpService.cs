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
using Visavi.Quantis.Data;
using Visavi.Quantis.Modeling;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Visavi.Quantis.Api
{
    public class HttpService
    {
        private readonly ILogger<DataService> _logger;
        private readonly Connections _connections;

        // HTTP Requests
        private static readonly string reloadDaysParmName = "reloadDays";

        public HttpService(ILogger<DataService> logger, Connections connections)
        {
            _logger = logger;
            _connections = connections;
        }

        [Function("BuildModel")]
        public async Task<IActionResult> HttpBuildEquityModel([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger?.LogInformation("Queuing Training of Model.");

            req.Query.TryGetValue("equityIndex", out var equityIndex);
            req.Query.TryGetValue("targetDuration", out var targetDuration);
            var receipt = await sendModelingMessage(new TrainModelMessage
            {
                Index = equityIndex,
                TargetDuration = Convert.ToInt32(targetDuration)
            });
            var resultText = $"Training of Model Queued, Index: {equityIndex}, Target Duration (Years): {targetDuration}";
            _logger?.LogInformation(resultText);
            return new OkObjectResult(resultText);
        }

        [Function("LoadEquities")]
        public async Task<IActionResult> HttpLoadEquities([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger?.LogInformation("Queuing Reload Equities Task.");
            uint? reloadDays = string.IsNullOrWhiteSpace(req.Query[reloadDaysParmName]) ? null : Convert.ToUInt32(req.Query[reloadDaysParmName]);
            var receipt = await sendEquitiesQueueMessage(new EquitiesQueueMessage
            {
                Message = DataService.LoadEquitiesMessage,
                ReloadDays = reloadDays
            });
            var resultText = $"Reload Equities Queued: {receipt.Value.MessageId}";
            _logger?.LogInformation(resultText);
            return new OkObjectResult(resultText);
        }

        private async Task<List<int>> getEquityIds(string? equityIndex = null)
        {
            List<int> simFinIds = new List<int>();
            using var connection = _connections.DbConnection;
            {
                if (string.IsNullOrWhiteSpace(equityIndex))
                {
                    simFinIds = (await connection.QueryAsync<int>("SELECT DISTINCT SimFinId FROM EquityHistory")).ToList();
                }
                else
                {
                    simFinIds = (await connection.QueryAsync<int>("SELECT DISTINCT SimFinId FROM IndexEquities WHERE IndexTicker = @equityIndex", new { equityIndex })).ToList();
                }
            }
            return simFinIds;
        }

        private async Task<DateTime> getEquitiesLastUpdate()
        {
            using var connection = _connections.DbConnection;
            return (await connection.QueryAsync<DateTime>("Select Top 1 [Date] from EquityHistory order by [Date] desc")).FirstOrDefault();
        }

        [Function("ExportTrainingData")]
        public async Task<IActionResult> HttpExportTrainingData([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger?.LogInformation("Queuing Export of Training Data.");

            req.Query.TryGetValue("equityIndex", out var equityIndex);
            req.Query.TryGetValue("minimumAge", out var minimumAge);
            List<int> simFinIds = await getEquityIds(equityIndex);
            string outputPath = string.Empty;
            DateTime? endDate = null;
            if (!string.IsNullOrWhiteSpace(equityIndex + minimumAge))
            {
                outputPath += $"{equityIndex}-{minimumAge}" + DataService.ContainerFolderSeperator;

                if (!string.IsNullOrWhiteSpace(minimumAge))
                {
                    var lastUpdate = await getEquitiesLastUpdate();
                    endDate = lastUpdate.AddYears(-Convert.ToInt32(minimumAge)) - TimeSpan.FromDays(7);
                }
            }
            foreach (int simFinId in simFinIds)
            {
                var receipt = await sendEquitiesQueueMessage(new EquitiesQueueMessage
                {
                    Message = DataService.ExportTrainingDataMessage,
                    SimFinId = simFinId,
                    EndDate = endDate,
                    OutputPath = outputPath
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
                        Message = DataService.UpdateYearlyReturnsMessage,
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

        private async Task<Response<SendReceipt>> sendEquitiesQueueMessage(EquitiesQueueMessage message)
        {
            var queueServiceClient = _connections.QueueConnection;
            var queueClient = queueServiceClient.GetQueueClient(DataService.EquityQueueName);
            await queueClient.CreateIfNotExistsAsync();

            string jsonMessage = JsonSerializer.Serialize(message);
            return await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonMessage)));
        }

        private async Task<Response<SendReceipt>> sendModelingMessage(TrainModelMessage message)
        {
            var queueServiceClient = _connections.QueueConnection;
            var queueClient = queueServiceClient.GetQueueClient(ModelingService.ModelingQueueName);
            await queueClient.CreateIfNotExistsAsync();

            string jsonMessage = JsonSerializer.Serialize(message);
            return await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonMessage)));
        }
    }
}
