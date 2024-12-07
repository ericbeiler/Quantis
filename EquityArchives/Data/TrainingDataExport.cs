using Azure.Storage.Blobs;
using CsvHelper;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Visavi.Quantis.Data
{
    internal class TrainingDataExport
    {
        private readonly IDataServices _dataServices;
        private const string modelTrainingContainerName = "model-training";
        private static readonly string equitiesPrefix = "equities" + DataService.ContainerFolderSeperator;


        private const string selectStatementForEquitiesTraining =
               @"
                SELECT *
                FROM EquityHistory
                WHERE SimFinId = @simFinId AND [Y1Cagr] IS NOT NULL AND [Date] < @endDate
                ORDER BY [Date] DESC";

        public TrainingDataExport(IDataServices dataServices)
        {
            _dataServices = dataServices;
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


            using var dbConnection = _dataServices.DbConnection;
            BlobServiceClient blobServiceClient = _dataServices.BlobConnection;
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(modelTrainingContainerName);
            foreach (int simFinId in simFinIds)
            {
                var equityData = await dbConnection.QueryAsync<DailyEquityRecord>(selectStatementForEquitiesTraining, new { simFinId, endDate});

                string equityFilename = $"{equitiesPrefix}{outputPath}equity-{simFinId}.csv";
                //_logger?.LogInformation($"Writing equity training data: {equityFilename}");

                BlobClient equityTrainingBlobClient = containerClient.GetBlobClient(equityFilename);
                using (var equityTrainingStream = equityTrainingBlobClient.OpenWrite(true))
                using (var equityTrainingWriter = new StreamWriter(equityTrainingStream))
                {
                    var csvWriter = new CsvWriter(equityTrainingWriter, CultureInfo.InvariantCulture);
                    await csvWriter.WriteRecordsAsync(equityData);

                    equityTrainingStream.Flush();
                    equityTrainingStream.Close();
                }
                //_logger?.LogInformation($"Completed writing {equityFilename}");
            }
        }
    }
}
