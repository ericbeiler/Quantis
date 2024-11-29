using Azure.Storage.Blobs;
using CsvHelper;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Visavi.Quantis.Data
{
    internal class TrainingDataExport
    {
        private readonly Connections _connections;
        private const string modelTrainingContainerName = "model-training";
        private static readonly string equitiesPrefix = "equities" + DataService.ContainerFolderSeperator;
        private ILogger _logger => _connections.Logger;


        private const string selectStatementForEquitiesTraining =
               @"
                SELECT *
                FROM EquityHistory
                WHERE SimFinId = @simFinId AND [Y1Cagr] IS NOT NULL AND [Date] < @endDate
                ORDER BY [Date] DESC";

        public TrainingDataExport(Connections connections)
        {
            _connections = connections;
        }

        internal async Task ExportTrainingDataAsync(int simFinId, DateTime? endDate = null, string? outputPath = null)
        {
            await ExportTrainingDataAsync([simFinId], endDate, outputPath);
        }

        internal async Task ExportTrainingDataAsync(int[] simFinIds, DateTime? endDate, string? outputPath)
        {
            endDate = endDate ?? new DateTime(5000, 1, 1);
            outputPath = outputPath ?? DataService.ContainerFolderSeperator;
            if (!outputPath.EndsWith(DataService.ContainerFolderSeperator))
            {
                outputPath += DataService.ContainerFolderSeperator;
            }


            using var dbConnection = _connections.DbConnection;
            BlobServiceClient blobServiceClient = _connections.BlobConnection;
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(modelTrainingContainerName);
            foreach (int simFinId in simFinIds)
            {
                var equityData = await dbConnection.QueryAsync<EquityModelingRecord>(selectStatementForEquitiesTraining, new { simFinId, endDate});

                string equityFilename = $"{equitiesPrefix}{outputPath}equity-{simFinId}.csv";
                _logger?.LogInformation($"Writing equity training data: {equityFilename}");

                BlobClient equityTrainingBlobClient = containerClient.GetBlobClient(equityFilename);
                using (var equityTrainingStream = equityTrainingBlobClient.OpenWrite(true))
                using (var equityTrainingWriter = new StreamWriter(equityTrainingStream))
                {
                    var csvWriter = new CsvWriter(equityTrainingWriter, CultureInfo.InvariantCulture);
                    await csvWriter.WriteRecordsAsync(equityData);

                    equityTrainingStream.Flush();
                    equityTrainingStream.Close();
                }
                _logger?.LogInformation($"Completed writing {equityFilename}");
            }
        }
    }
}
