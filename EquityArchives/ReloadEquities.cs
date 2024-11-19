using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using Visavi.Quantis;

namespace QuantisFunctions
{
    public class ReloadEquities
    {
        private readonly ILogger<ReloadEquities>? _logger;
        private readonly string? _storageConnectionString;

        public ReloadEquities(ILogger<ReloadEquities> logger, IConfiguration configuration)
        {
            _logger = logger;

            _storageConnectionString = configuration["QuantisStorageConnection"];
            if (string.IsNullOrEmpty(_storageConnectionString))
            {
                _logger?.LogError("QuantisStorageConnection value is missing.");
            }
        }

        [Function("ReloadEquities")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger?.LogInformation("Queuing Reload Equities Task.");
            try
            {
                var queueServiceClient = new QueueServiceClient(_storageConnectionString);
                var queueClient = queueServiceClient.GetQueueClient(EquityQueueService.EquityQueueName);
                await queueClient.CreateIfNotExistsAsync();
                var messageContent = JsonConvert.SerializeObject(new { Property1 = "Value1", Property2 = "Value2" });
                var receipt = await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(EquityQueueService.ReloadMessage)));
                return new OkObjectResult($"Reload Equities Queued: {receipt.Value.MessageId}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error queuing Reload Equities Task.");
                throw;
            }
        }
    }
}