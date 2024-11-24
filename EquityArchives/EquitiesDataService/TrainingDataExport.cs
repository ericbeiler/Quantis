using Azure.Storage.Blobs;
using CsvHelper;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Visavi.Quantis.EquitiesDataService
{
    internal class TrainingDataExport
    {
        private readonly Connections _connections;
        private const string modelTrainingContainerName = "model-training";
        private const string equitiesPrefix = "equities";
        private ILogger _logger => _connections.Logger;

        public TrainingDataExport(Connections connections)
        {
            _connections = connections;
        }

        internal async Task ExportTrainingDataAsync(int simFinId)
        {
            await ExportTrainingDataAsync([simFinId]);
        }

        internal async Task ExportTrainingDataAsync(int[] simFinIds)
        {
            using var dbConnection = _connections.DbConnection;
            BlobServiceClient blobServiceClient = _connections.BlobConnection;
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(modelTrainingContainerName);
            foreach (int simFinId in simFinIds)
            {
                var equityData = await dbConnection.QueryAsync<DailyEquityRecord>("SELECT * FROM EquityHistory WHERE SimFinId = @simFinId ORDER BY [Date] DESC", new { simFinId });

                string equityFilename = $"{equitiesPrefix}\\equity-{simFinId}.csv";
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
