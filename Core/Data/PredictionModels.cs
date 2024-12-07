using Azure.Storage.Blobs;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Visavi.Quantis.Data
{
    internal class PredictionModels : IPredictionModels
    {
        private const string equityModelsContainer = "equity-models";
        private const string selectTrainedModels = "SELECT * FROM EquityModels";

        ILogger _logger;
        Connections _connections;

        internal PredictionModels(Connections connections, ILogger logger)
        {
            _logger = logger;
            _connections = connections;
        }

        public async Task<IEnumerable<PredictionModelSummary>> GetModelSummaryListAsync()
        {
            using var dbConnection = _connections.DbConnection;
            return await dbConnection.QueryAsync<PredictionModelSummary>(selectTrainedModels);
        }

        public async Task<PredictionModel> GetPredictionModelAsync(int id)
        {
            using var dbConnection = _connections.DbConnection;
            var modelSummary = await dbConnection.QueryFirstOrDefaultAsync<PredictionModelSummary>("SELECT * FROM EquityModels WHERE Id = @Id", new { Id = id });
            if (modelSummary == null)
            {
                throw new KeyNotFoundException($"Model with id {id} not found.");
            }

            var storageConnection = _connections.BlobConnection;
            var containerClient = storageConnection.GetBlobContainerClient(equityModelsContainer);
            var blobClient = containerClient.GetBlobClient(modelSummary.Path);
            var inferencingModel = (await blobClient.DownloadContentAsync()).Value.Content;
            return new PredictionModel(modelSummary, inferencingModel);
        }

        public async Task SaveModel(string modelName, ITrainedPredictionModel trainedModel)
        {
            string blobModelName = $"{modelName}.zip";
            _logger?.LogInformation($"Writing blob model: {blobModelName}");

            BlobServiceClient blobServiceClient = new BlobServiceClient(_connections.StorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(equityModelsContainer);
            containerClient.CreateIfNotExists();

            BlobClient modelBlobClient = containerClient.GetBlobClient(blobModelName);
            using (var modelBlobStream = modelBlobClient.OpenWrite(true))
            {
                trainedModel.MLContext.Model.Save(trainedModel.Transformer, trainedModel.TrainingSchema, modelBlobStream);
                modelBlobStream.Flush();
                modelBlobStream.Close();
            }

            _logger.LogInformation("Saved Model, updating metadata");
            using var connection = _connections.DbConnection;
            await connection.ExecuteAsync("INSERT INTO EquityModels ([Type], [Index], [TargetDuration], [Timestamp], [Path], [MeanAbsoluteError], [RootMeanSquaredError], [LossFunction], [RSquared])" +
                                    "Values (@Type, @Index, @TargetDuration, @Timestamp, @Path, @MeanAbsoluteError, @RootMeanSquaredError, @LossFunction, @RSquared)",
                                    new
                                    {
                                        Type = "Regression",
                                        Index = trainedModel.TickerIndex,
                                        trainedModel.TargetDuration,
                                        trainedModel.Timestamp,
                                        Path = blobModelName,
                                        trainedModel.Metrics.MeanAbsoluteError,
                                        trainedModel.Metrics.RootMeanSquaredError,
                                        trainedModel.Metrics.LossFunction,
                                        trainedModel.Metrics.RSquared
                                    });
        }
    }
}
