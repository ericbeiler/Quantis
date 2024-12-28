using Azure.Storage.Blobs;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Visavi.Quantis.Modeling;

namespace Visavi.Quantis.Data
{
    internal class PredictionModels : IPredictionModels
    {
        private const string equityModelsContainer = "equity-models";
        private const string retrieveCagrRegressionModels = "SELECT * FROM CagrRegressionModels";
        private const string retrieveCompositeModels = "SELECT * FROM CompositeModels";
        private const ModelState createdModelState = ModelState.Created;

        ILogger _logger;
        Connections _connections;

        internal PredictionModels(Connections connections, ILogger logger)
        {
            _logger = logger;
            _connections = connections;
        }

        public async Task<int> CreateCompositeModel(TrainModelMessage trainingParameters)
        {
            using var dbConnection = _connections.DbConnection;
            var jsonTrainingParameters = JsonSerializer.Serialize(trainingParameters);
            return await dbConnection.ExecuteScalarAsync<int>("INSERT INTO CompositeModels ([Parameters], [State]) Values (@jsonTrainingParameters, @createdModelState); SELECT SCOPE_IDENTITY()", new { jsonTrainingParameters, createdModelState });
        }

        public async Task<CompositeModel> GetCompositeModel(int compositeModelId)
        {
            using var dbConnection = _connections.DbConnection;
            var jsonTrainingParameters = await dbConnection.QueryFirstOrDefaultAsync<string>("SELECT [Parameters] FROM CompositeModels WHERE Id = @Id", new { Id = compositeModelId });
            if (jsonTrainingParameters == null)
            {
                throw new KeyNotFoundException($"Model with id {compositeModelId} not found.");
            }
            var trainingParameters = JsonSerializer.Deserialize<TrainingParameters>(jsonTrainingParameters);

            var predictionModelSummaries = await dbConnection.QueryAsync<CagrRegressionModelRecord>("SELECT * FROM CagrRegressionModels WHERE CompositeId = @CompositeId", new { CompositeId = compositeModelId });
            var pricePredictors = await Task.WhenAll(predictionModelSummaries.Select(async modelSummary => await getPricePredictor(modelSummary)));

            return new CompositeModel(trainingParameters, pricePredictors);
        }

        public async Task<IEnumerable<ModelSummary>> GetModelSummaryList(ModelType modelType = ModelType.Composite)
        {
            using var dbConnection = _connections.DbConnection;
            switch (modelType)
            {
                case ModelType.Composite:
                    var compositeModels = await dbConnection.QueryAsync<CompositeModelRecord>(retrieveCompositeModels);
                    return compositeModels.Select(model => model.ToModelSummary());

                case ModelType.CagrRegression:
                    var regressionModels = await dbConnection.QueryAsync<CagrRegressionModelRecord>(retrieveCagrRegressionModels);
                    return regressionModels.Select(model => model.ToModelSummary());

                default:
                    throw new ArgumentException($"ModelType {modelType} not supported.");
            }
        }

        public async Task<IPredictor> GetPricePredictor(int id)
        {
            using var dbConnection = _connections.DbConnection;
            var modelSummary = await dbConnection.QueryFirstOrDefaultAsync<CagrRegressionModelRecord>("SELECT * FROM CagrRegressionModels WHERE Id = @Id", new { Id = id });
            if (modelSummary == null)
            {
                throw new KeyNotFoundException($"Model with id {id} not found.");
            }

            return await getPricePredictor(modelSummary);
        }

        private async Task<IPredictor> getPricePredictor(CagrRegressionModelRecord modelSummary)
        {
            if (modelSummary == null)
            {
                throw new NullReferenceException($"modelSummary can not be null.");
            }
            var storageConnection = _connections.BlobConnection;
            var containerClient = storageConnection.GetBlobContainerClient(equityModelsContainer);
            var blobClient = containerClient.GetBlobClient(modelSummary.Path);
            var inferencingModel = (await blobClient.DownloadContentAsync()).Value.Content;
            return new PricePointPredictor(modelSummary, inferencingModel) as IPredictor;
        }

        public async Task SaveModel(string modelName, RegressionModel regressionModel)
        {
            string blobModelName = $"{modelName}.zip";
            _logger?.LogInformation($"Writing blob model: {blobModelName}");

            BlobServiceClient blobServiceClient = new BlobServiceClient(_connections.StorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(equityModelsContainer);
            containerClient.CreateIfNotExists();

            BlobClient modelBlobClient = containerClient.GetBlobClient(blobModelName);
            using (var modelBlobStream = modelBlobClient.OpenWrite(true))
            {
                regressionModel.Save(modelBlobStream);
                modelBlobStream.Flush();
                modelBlobStream.Close();
            }

            _logger?.LogInformation("Saved Model, updating metadata");
            using var connection = _connections.DbConnection;
            await connection.ExecuteAsync("INSERT INTO CagrRegressionModels ([Type], [Index], [TargetDuration], [Timestamp], [Path], [CompositeId], [MeanAbsoluteError], [RootMeanSquaredError], [LossFunction], [RSquared]," +
                                            "[AveragePearsonCorrelation],[MinimumPearsonCorrelation],[AverageSpearmanRankCorrelation],[MinimumSpearmanRankCorrelation],[CrossValAverageMeanAbsoluteError],[CrossValMaximumMeanAbsoluteError]," +
                                            "[CrossValAverageRootMeanSquaredError],[CrossValMaximumRootMeanSquaredError],[CrossValAverageRSquared],[CrossValMaximumRSquared])" +
                                            "Values (@Type, @Index, @TargetDuration, @Timestamp, @Path, @CompositeId, @MeanAbsoluteError, @RootMeanSquaredError, @LossFunction, @RSquared," +
                                            "@AveragePearsonCorrelation, @MinimumPearsonCorrelation, @AverageSpearmanRankCorrelation, @MinimumSpearmanRankCorrelation, @CrossValAverageMeanAbsoluteError, @CrossValMaximumMeanAbsoluteError," +
                                            "@CrossValAverageRootMeanSquaredError, @CrossValMaximumRootMeanSquaredError, @CrossValAverageRSquared, @CrossValMaximumRSquared)",
                                    new
                                    {
                                        Type = ModelType.CagrRegression.ToString(),
                                        Index = regressionModel.IndexTicker,
                                        TargetDuration = regressionModel.TargetDurationInMonths,
                                        Timestamp = regressionModel.Timestamp,
                                        Path = blobModelName,
                                        CompositeId = regressionModel.CompositeId,
                                        MeanAbsoluteError = regressionModel?.Metrics?.MeanAbsoluteError,
                                        RootMeanSquaredError = regressionModel?.Metrics?.RootMeanSquaredError,
                                        LossFunction = regressionModel?.Metrics?.LossFunction,
                                        RSquared = regressionModel?.Metrics?.RSquared,
                                        AveragePearsonCorrelation = regressionModel?.Metrics?.CrossValidationMetrics?.AveragePearsonCorrelation,
                                        MinimumPearsonCorrelation = regressionModel?.Metrics?.CrossValidationMetrics?.MinimumPearsonCorrelation,
                                        AverageSpearmanRankCorrelation = regressionModel?.Metrics?.CrossValidationMetrics?.AverageSpearmanRankCorrelation,
                                        MinimumSpearmanRankCorrelation = regressionModel?.Metrics?.CrossValidationMetrics?.MinimumSpearmanRankCorrelation,
                                        CrossValAverageMeanAbsoluteError = regressionModel?.Metrics?.CrossValidationMetrics?.AverageMeanAbsoluteError,
                                        CrossValMaximumMeanAbsoluteError = regressionModel?.Metrics?.CrossValidationMetrics?.MaximumMeanAbsoluteError,
                                        CrossValAverageRootMeanSquaredError = regressionModel?.Metrics?.CrossValidationMetrics?.AverageRootMeanSquaredError,
                                        CrossValMaximumRootMeanSquaredError = regressionModel?.Metrics?.CrossValidationMetrics?.MaximumRootMeanSquaredError,
                                        CrossValAverageRSquared = regressionModel?.Metrics?.CrossValidationMetrics?.AverageRSquared,
                                        CrossValMaximumRSquared = regressionModel?.Metrics?.CrossValidationMetrics?.MaximumRSquared
                                    });
        }

        public async Task UpdateModelState(int modelId, ModelState modelState)
        {
            try
            {
                using var connection = _connections.DbConnection;
                await connection.ExecuteAsync("UPDATE CompositeModels SET State = @ModelState WHERE Id = @Id", new { ModelState = modelState, Id = modelId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating model state for model {modelId} to {modelState}");
                throw;
            }
        }

        public async Task UpdateQualityScore(int modelId, double qualityScore)
        {
            try
            {
                using var connection = _connections.DbConnection;
                await connection.ExecuteAsync("UPDATE CompositeModels SET QualityScore = @QualityScore WHERE Id = @Id", new { QualityScore = qualityScore, Id = modelId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating quality score for model {modelId} to {qualityScore}");
                throw;
            }
        }
    }
}
