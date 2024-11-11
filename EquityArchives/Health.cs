using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections;

namespace EquityArchives
{
    public class Health
    {
        private readonly ILogger<Health>? _logger;
        private readonly string? _dbConnectionString;
        private readonly string? _storageConnectionString;

        public Health(ILogger<Health> logger, IConfiguration configuration)
        {
            _logger = logger;

            _dbConnectionString = configuration["QuantisDbConnection"];
            if (string.IsNullOrEmpty(_dbConnectionString))
            {
                _logger?.LogError("QuantisDbConnection value is missing.");
            }

            _storageConnectionString = configuration["QuantisStorageConnection"];
            if (string.IsNullOrEmpty(_storageConnectionString))
            {
                _logger?.LogError("QuantisStorageConnection value is missing.");
            }
        }

        [Function("Health")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger?.LogInformation("HTTP Request for Health.");

            string output = "Quantis Health \n\nDB Statistics\n";
            if (!string.IsNullOrEmpty(_dbConnectionString))
            {
                using var dbConnection = new SqlConnection(_dbConnectionString);
                {
                    await dbConnection.OpenAsync();
                    var statistics = dbConnection.RetrieveStatistics();

                    output += $"  Server Version: {dbConnection.ServerVersion}\n";
                    output += $"  Process ID: {dbConnection.ServerProcessId}\n";
                    output += $"  State: {dbConnection.State}\n";
                    foreach (DictionaryEntry de in statistics)
                    {
                        output += $"  {de.Key} = {de.Value}\n";
                    }
                }
            }
            else
            {
                output += "  No DB connection string provided.\n";
            }

            if (!string.IsNullOrEmpty(_storageConnectionString))
            {
                output += "\nStorage Statistics\n";
                var blobServiceClient = new BlobServiceClient(_storageConnectionString);
                var blobContainers = blobServiceClient.GetBlobContainersAsync();

                await foreach (var blobContainer in blobContainers)
                {
                    output += $"  Container Name: {blobContainer.Name}\n";
                }
            }
            else
            {
                output += "  No Storage connection string provided.\n";
            }

            return new OkObjectResult(output);
        }
    }
}
