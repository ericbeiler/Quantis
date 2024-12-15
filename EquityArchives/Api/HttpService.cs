using Azure;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Visavi.Quantis.Data;
using Visavi.Quantis.Modeling;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Visavi.Quantis.Api
{
    public class HttpService
    {
        private readonly ILogger<HttpService> _logger;
        private readonly ModelService _modelService;
        private readonly IDataServices _dataServices;

        // HTTP Requests
        private static readonly string reloadDaysParmName = "reloadDays";
        private const string quantisModelingQueue = "quantis-modeling";

        public HttpService(ILogger<HttpService> logger, ModelService modelService, IDataServices dataServices)
        {
            _logger = logger;
            _modelService = modelService;
            _dataServices = dataServices;
        }

        [Function("EquityModel")]
        public async Task<IActionResult> HttpEquityModel([HttpTrigger(AuthorizationLevel.Anonymous, ["post", "get"], Route = "EquityModel/{id:int?}")] HttpRequest req, int? id)
        {
            switch (req.Method.ToLower())
            {
                case "post":
                    req.Query.TryGetValue("equityIndex", out var equityIndex);
                    req.Query.TryGetValue("targetDuration", out var targetDuration);
                    req.Query.TryGetValue("datasetSizeLimit", out var _datasetSizeLimit);
                    string? datasetSizeLimit = _datasetSizeLimit.FirstOrDefault();
                    return await httpPostEquityModel(equityIndex, Convert.ToInt32(targetDuration), datasetSizeLimit != null ? Convert.ToInt32(datasetSizeLimit) : null);

                case "get":
                    return await (id == null ? httpGetModelSummaryList() : httpGetModel(id.Value));

                default:
                    return new BadRequestResult();
            }
        }

        private async Task<IActionResult> httpPostEquityModel(string? equityIndex, int targetDurationInMonths, int? datasetSizeLimit = null)
        {
            if (!PredictionModel.IsValidDuration(targetDurationInMonths))
            {
                return new BadRequestObjectResult($"{targetDurationInMonths} is not a valid Target Duration. Try 12, 24, 36 or 60 instead.");
            }

            _logger?.LogInformation("Queuing Training of Model.");

            var receipt = await sendModelingMessage(new TrainModelMessage(){
                                                        TargetDurationInMonths = targetDurationInMonths,
                                                        Index = equityIndex,
                                                        DatasetSizeLimit = datasetSizeLimit});

            var resultText = $"Training of Model Queued, Index: {equityIndex}, Target Duration (Months): {targetDurationInMonths}";
            _logger?.LogInformation(resultText);
            return new OkObjectResult(resultText);
        }

        [Function("Predictions")]
        public async Task<IActionResult> Predictions([HttpTrigger(AuthorizationLevel.Anonymous, ["get"], Route = "Predictions/{modelId:int?}")] HttpRequest req, int? modelId, string ticker, DateTime? predictionDay = null)
        {
            _logger?.LogInformation($"Prediction: modelId = {modelId}, ticker={ticker}");
            if (modelId == null)
            {
                return new BadRequestObjectResult("A modelId must be specified to predict values");
            }

            if (ticker == null)
            {
                return new BadRequestObjectResult("A valid ticker (e.g. ?ticker=SPY ) must be specified to predict values");
            }

            string[] tickers = ticker.Split(',');
            var predictions = await _modelService.PredictAsync(modelId.Value, tickers);
            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };
            var predictionsJson = JsonSerializer.Serialize(predictions, options);

            _logger?.LogInformation(predictionsJson);
            return new OkObjectResult(predictionsJson);
        }

        private async Task<IActionResult> httpGetModelSummaryList()
        {
            _logger?.LogInformation("Getting a list of all trained models.");

            var models = await _dataServices.PredictionModels.GetModelSummaryListAsync();
            var resultText = JsonSerializer.Serialize(models);

            _logger?.LogInformation(resultText);
            return new OkObjectResult(resultText);
        }

        private async Task<IActionResult> httpGetModel(int id)
        {
            /*
            _logger?.LogInformation($"Getting inference model for id {id}");

            var model = await _modelService.GetModelAsync(id);
            var resultText = JsonSerializer.Serialize(model);

            _logger?.LogInformation(resultText);
            return new OkObjectResult(resultText);
            */
            throw new NotImplementedException();
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


        [Function("ExportTrainingData")]
        public async Task<IActionResult> HttpExportTrainingData([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger?.LogInformation("Queuing Export of Training Data.");

            req.Query.TryGetValue("equityIndex", out var equityIndex);
            req.Query.TryGetValue("minimumAge", out var minimumAge);
            List<int> simFinIds = await _dataServices.EquityArchives.GetEquityIds(equityIndex);
            string outputPath = string.Empty;
            DateTime? endDate = null;
            if (!string.IsNullOrWhiteSpace(equityIndex + minimumAge))
            {
                outputPath += $"{equityIndex}-{minimumAge}" + DataService.ContainerFolderSeperator;

                if (!string.IsNullOrWhiteSpace(minimumAge))
                {
                    var lastUpdate = await _dataServices.EquityArchives.GetLastUpdateAsync();
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
            var queueServiceClient = _dataServices.QueueConnection;
            var queueClient = queueServiceClient.GetQueueClient(DataService.EquityQueueName);
            await queueClient.CreateIfNotExistsAsync();

            string jsonMessage = JsonSerializer.Serialize(message);
            return await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonMessage)));
        }

        private async Task<Response<SendReceipt>> sendModelingMessage(TrainModelMessage message)
        {
            var queueServiceClient = _dataServices.QueueConnection;
            var queueClient = queueServiceClient.GetQueueClient(quantisModelingQueue);
            await queueClient.CreateIfNotExistsAsync();

            string jsonMessage = JsonSerializer.Serialize(message);
            return await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonMessage)));
        }
    }
}
