using Azure;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;
using System.Net;
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
        private readonly IPredictionService _predictionService;
        private readonly IDataServices _dataServices;

        // HTTP Requests
        private static readonly string reloadDaysParmName = "reloadDays";
        private const string quantisModelingQueue = "quantis-modeling";

        public HttpService(ILogger<HttpService> logger, IPredictionService predictionService, IDataServices dataServices)
        {
            _logger = logger;
            _predictionService = predictionService;
            _dataServices = dataServices;
        }

        /// <summary>
        /// Collates the training parameters from the request query and body. The request favors parameters included
        /// in the query over the body for legacy purposes.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="BadHttpRequestException"></exception>
        private async Task<TrainingParameters> collateRequestTrainingParameters(HttpRequest request)
        {
            request.Query.TryGetValue("equityIndex", out var equityIndex);
            request.Query.TryGetValue("targetDurations", out var targetDurations);
            request.Query.TryGetValue("datasetSizeLimit", out var _datasetSizeLimit);
            request.Query.TryGetValue("algorithm", out var _algorithm);
            request.Query.TryGetValue("numberOfTrees", out var _numberOfTrees);
            request.Query.TryGetValue("numberOfLeaves", out var _numberOfLeaves);
            request.Query.TryGetValue("granularity", out var _granularity);
            request.Query.TryGetValue("minimumExampleCountPerLeaf", out var _minimumExampleCountPerLeaf);
            request.Query.TryGetValue("name", out var _name);
            request.Query.TryGetValue("description", out var _description);

            string? datasetSizeLimit = _datasetSizeLimit.FirstOrDefault();
            string? algorithm = _algorithm.FirstOrDefault();
            string? numberOfTrees = _numberOfTrees.FirstOrDefault();
            string? numberOfLeaves = _numberOfLeaves.FirstOrDefault();
            string? minimumExampleCountPerLeaf = _minimumExampleCountPerLeaf.FirstOrDefault();
            string? granularity = _granularity.FirstOrDefault();
            string? name = _name;
            string? description = _description;
            int[]? targetDurationsInMonths = targetDurations.FirstOrDefault()?.Split(',')?.Select(stringVal => Convert.ToInt32(stringVal))?.ToArray();

            TrainingAlgorithm? trainingAlgorithm = null;
            if (!string.IsNullOrEmpty(algorithm))
            {
                try
                {
                    trainingAlgorithm = Enum.Parse<TrainingAlgorithm>(algorithm, true);
                }
                catch (Exception ex)
                {
                    throw new BadHttpRequestException($"{algorithm} is not a valid algorithm. Try FastTree or Auto instead.", ex);
                };
            }

            TrainingGranularity? trainingGrainularity = null;
            if (!string.IsNullOrEmpty(granularity))
            {
                try
                {
                    trainingGrainularity = Enum.Parse<TrainingGranularity>(granularity, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"{algorithm} is not a valid grainularity. Try Daily or Monthly instead.");
                    throw new BadHttpRequestException($"{algorithm} is not a valid grainularity. Try Daily or Monthly instead.", ex);
                };
            }

            if (targetDurationsInMonths != null && targetDurationsInMonths.Any(duration => !PricePointPredictor.IsValidDuration(duration)))
            {
                throw new BadHttpRequestException($"{targetDurationsInMonths} is not a valid Target Duration. Try 12, 24, 36 or 60 instead.");
            }

            // Read the request body
            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            var collatedParameters = JsonSerializer.Deserialize<TrainingParameters>(requestBody) ?? new TrainingParameters();
            if (!string.IsNullOrWhiteSpace(datasetSizeLimit))
            {
                collatedParameters.DatasetSizeLimit = Convert.ToInt32(datasetSizeLimit);
            }
            if (!string.IsNullOrEmpty(equityIndex))
            {
                collatedParameters.Index = equityIndex;
            }
            if (targetDurationsInMonths != null)
            {
                collatedParameters.TargetDurationsInMonths = targetDurationsInMonths;
            }
            if (trainingAlgorithm != null)
            {
                collatedParameters.Algorithm = trainingAlgorithm;
            }
            if (numberOfTrees != null)
            {
                collatedParameters.NumberOfTrees = Convert.ToInt32(numberOfTrees);
            }
            if (numberOfLeaves != null)
            {
                collatedParameters.NumberOfLeaves = Convert.ToInt32(numberOfLeaves);
            }
            if (minimumExampleCountPerLeaf != null)
            {
                collatedParameters.MinimumExampleCountPerLeaf = Convert.ToInt32(minimumExampleCountPerLeaf);
            }
            if (trainingGrainularity != null)
            {
                collatedParameters.Granularity = trainingGrainularity;
            }
            return collatedParameters;
        }


        /// <summary>
        /// Queues a model for training.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Function("PostModel")]
        public async Task<IActionResult> PostEquityModel([HttpTrigger(AuthorizationLevel.Anonymous, ["post"], Route = "Model")] HttpRequest req)
        {
            _logger?.LogInformation("Queuing Training of Model.");
            try
            {
                req.Query.TryGetValue("name", out var _name);
                var name = _name;

                req.Query.TryGetValue("description", out var _description);
                var description = _description;

                var trainingParameters = await collateRequestTrainingParameters(req);

                var receipt = await sendModelingMessage(new TrainModelMessage(trainingParameters, name, description));
                var resultText = $"Training of Model {name} Queued:\n{trainingParameters}";
                _logger?.LogInformation(resultText);
                return new OkObjectResult(resultText);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error in sending the model training message. {ex.Message}");
                return new BadRequestObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// Gets a summary of all models or detailed information on a specific model.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Function("GetModel")]
        public async Task<IActionResult> GetEquityModel([HttpTrigger(AuthorizationLevel.Anonymous, ["get"], Route = "Model/{id:int?}")] HttpRequest req, int? id)
        {
            return await (id == null ? httpGetModelSummaryList() : httpGetModel(ModelType.Composite, id.Value));
        }

        /// <summary>
        /// Gets a summary of all models or detailed information on a specific model.
        /// </summary>
        /// <returns></returns>
        [Function("GetFeatureList")]
        public IActionResult GetFeatureList([HttpTrigger(AuthorizationLevel.Anonymous, ["get"], Route = "FeatureList")] HttpRequest req)
        {
            var featureList = _dataServices.PredictionModels.GetFeatureList();
            var resultText = JsonSerializer.Serialize(featureList);
            return new OkObjectResult(resultText);
        }

        public class UpdateModelRequest
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
        }

        /// <summary>
        /// Updates the name and description of a model.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Function("PatchModel")]
        public async Task<IActionResult> UpdateEquityModel([HttpTrigger(AuthorizationLevel.Anonymous, ["patch"], Route = "Model/{id:int}")] HttpRequest req, int id)
        {
            // Read the request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Deserialize the JSON into an object
            var data = JsonSerializer.Deserialize<UpdateModelRequest>(requestBody);

            // Extract values
            string? name = data?.Name;
            string? description = data?.Description;

            // Update model if values are provided
            if (!string.IsNullOrEmpty(name))
            {
                await _dataServices.PredictionModels.UpdateModelName(id, name);
            }

            if (!string.IsNullOrEmpty(description))
            {
                await _dataServices.PredictionModels.UpdateModelDescription(id, description);
            }

            return new OkResult();
        }

        /// <summary>
        /// Delete a model.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Function("DeleteModel")]
        public async Task<IActionResult> DeleteEquityModel([HttpTrigger(AuthorizationLevel.Anonymous, ["delete"], Route = "Model/{id:int}")] HttpRequest req, int id)
        {
            await _dataServices.PredictionModels.UpdateModelState(id, ModelState.Deleted);
            return new OkResult();
        }

        public class QueantisEventConnectionInfo
        {
            public string Url { get; set; }

            public string AccessToken { get; set; }
        }


        [Function("negotiate")]
        public async Task<HttpResponseData> Negotiate(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            [SignalRConnectionInfoInput(HubName = "events")] QueantisEventConnectionInfo connectionInfo)
        {
            _logger.LogInformation($"SignalR Connection URL = '{connectionInfo.Url}'");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            // Serialize the connection info to JSON
            var responseBody = new
            {
                url = connectionInfo.Url,
                accessToken = connectionInfo.AccessToken
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(responseBody));

            return response;
        }

        [Function("Predictions")]
        public async Task<IActionResult> Predictions([HttpTrigger(AuthorizationLevel.Anonymous, ["get"], Route = "Predictions/{modelId:int?}")] HttpRequest req, int? modelId, string ticker, DateTime? predictionDay = null)
        {
            _logger?.LogInformation($"Prediction: modelId = {modelId}, ticker={ticker}");
            if (modelId == null || modelId <= 0)
            {
                return new BadRequestObjectResult("A modelId must be specified and greater than 0 to predict values");
            }

            if (ticker == null)
            {
                return new BadRequestObjectResult("A valid ticker (e.g. ?ticker=SPY ) must be specified to predict values");
            }

            string[] tickers = ticker.Split(',');
            var predictions = await _predictionService.PredictPriceTrend(modelId.Value, tickers);
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

            var models = await _dataServices.PredictionModels.GetModelSummaryList();
            var resultText = JsonSerializer.Serialize(models);

            _logger?.LogInformation(resultText);
            return new OkObjectResult(resultText);
        }

        private async Task<IActionResult> httpGetModel(ModelType type, int id)
        {
            _logger?.LogInformation($"Getting inference model for id {id}");

            switch (type)
            {
                case ModelType.Composite:
                    var compositeModel = await _dataServices.PredictionModels.GetCompositeModelDetails(id);
                    var resultText = JsonSerializer.Serialize(compositeModel);
                    _logger?.LogInformation($"Returning object: {resultText}");
                    return new OkObjectResult(resultText);

                default:
                    return new BadRequestObjectResult($"Model Type {type} is not supported.");
            }
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

        private async Task<int> sendModelingMessage(TrainModelMessage message)
        {
            message.TrainingParameters.CompositeModelId = await _dataServices.PredictionModels.CreateCompositeModel(message);

            var queueServiceClient = _dataServices.QueueConnection;
            var queueClient = queueServiceClient.GetQueueClient(quantisModelingQueue);
            await queueClient.CreateIfNotExistsAsync();

            JsonSerializerOptions options = new JsonSerializerOptions() { RespectNullableAnnotations = true };
            string jsonMessage = JsonSerializer.Serialize(message, options);
            _logger?.LogInformation($"Sending Modeling Message: {jsonMessage}");
            var sendReceipt =  await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonMessage)));
            return message?.TrainingParameters.CompositeModelId ?? 0;
        }
    }
}
