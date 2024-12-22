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
            return await dbConnection.ExecuteScalarAsync<int>("INSERT INTO CompositeModels ([Parameters]) Values (@jsonTrainingParameters); SELECT SCOPE_IDENTITY()", new { jsonTrainingParameters });
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

            var predictionModelSummaries = await dbConnection.QueryAsync<CagrRegressionModel>("SELECT * FROM CagrRegressionModels WHERE CompositeId = @CompositeId", new { CompositeId = compositeModelId });
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
                    var regressionModels = await dbConnection.QueryAsync<CagrRegressionModel>(retrieveCagrRegressionModels);
                    return regressionModels.Select(model => model.ToModelSummary());

                default:
                    throw new ArgumentException($"ModelType {modelType} not supported.");
            }
        }

        public async Task<IPredictor> GetPricePredictor(int id)
        {
            using var dbConnection = _connections.DbConnection;
            var modelSummary = await dbConnection.QueryFirstOrDefaultAsync<CagrRegressionModel>("SELECT * FROM CagrRegressionModels WHERE Id = @Id", new { Id = id });
            if (modelSummary == null)
            {
                throw new KeyNotFoundException($"Model with id {id} not found.");
            }

            return await getPricePredictor(modelSummary);
        }

        private async Task<IPredictor> getPricePredictor(CagrRegressionModel modelSummary)
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
            await connection.ExecuteAsync("INSERT INTO CagrRegressionModels ([Type], [Index], [TargetDuration], [Timestamp], [Path], [CompositeId], [MeanAbsoluteError], [RootMeanSquaredError], [LossFunction], [RSquared])" +
                                    "Values (@Type, @Index, @TargetDuration, @Timestamp, @Path, @CompositeId, @MeanAbsoluteError, @RootMeanSquaredError, @LossFunction, @RSquared)",
                                    new
                                    {
                                        Type = "Regression",
                                        Index = regressionModel.IndexTicker,
                                        TargetDuration = regressionModel.TargetDurationInMonths,
                                        Timestamp = regressionModel.Timestamp,
                                        Path = blobModelName,
                                        CompositeId = regressionModel.CompositeId,
                                        MeanAbsoluteError = regressionModel?.Metrics?.MeanAbsoluteError,
                                        RootMeanSquaredError = regressionModel?.Metrics?.RootMeanSquaredError,
                                        LossFunction = regressionModel?.Metrics?.LossFunction,
                                        RSquared = regressionModel?.Metrics?.RSquared
                                    });
        }
    }
}
