using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Text.Json;

namespace Visavi.Quantis.Modeling
{
    public class TrainModelsService : BackgroundService
    {
        private readonly ILogger<TrainModelsService> _logger;
        private readonly QueueClient _queueClient = new QueueClient(StorageConnectionString, "quantis-modeling");

        internal const string StorageConnectionString = "BlobEndpoint=https://quantis.blob.core.windows.net/;QueueEndpoint=https://quantis.queue.core.windows.net/;FileEndpoint=https://quantis.file.core.windows.net/;TableEndpoint=https://quantis.table.core.windows.net/;SharedAccessSignature=sv=2022-11-02&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2025-11-17T02:13:18Z&st=2024-11-16T18:13:18Z&spr=https&sig=iKRDTg8msgsX8pPrgbJo%2Fm2gZam8JxDrF%2B11PU2KsZU%3D";

        public TrainModelsService(ILogger<TrainModelsService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Model Training Service Started at: {time}", DateTime.Now);
            while (!cancellationToken.IsCancellationRequested)
            {
                QueueMessage? poppedMessage = null;
                try
                {
                    // Retrieving the training parameters
                    _queueClient.CreateIfNotExists();

                    _logger.LogInformation("Waiting for message...");
                    poppedMessage = _queueClient.ReceiveMessage(TimeSpan.FromMinutes(180), cancellationToken).Value;
                    var trainingParameters = decodeMessage(poppedMessage);
                    if (trainingParameters == null)
                    {
                        _logger.LogInformation("No model message is in the queue, delaying 1 minute.");
                        await Task.Delay(60000, cancellationToken);
                        continue;
                    }

                    _logger.LogInformation($"Processing message {poppedMessage?.MessageId}.");
                    var trainingJob = new ModelTrainingJob(trainingParameters, _logger, cancellationToken);
                    await trainingJob.ExecuteAsync();

                    _logger.LogInformation($"Deleting message {poppedMessage?.MessageId}.");
                    _queueClient.DeleteMessage(poppedMessage?.MessageId, poppedMessage?.PopReceipt, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not build model for message {poppedMessage?.MessageId}\n{ex.ToString()}.");
                    continue;
                }
            }
            _logger.LogInformation("Model Training Service Stopped at: {time}", DateTime.Now);
        }

        private TrainModelMessage? decodeMessage(QueueMessage message)
        {
            if (message == null)
            {
                return null;
            }

            // Deserialize the message body to get the ReloadDays
            string messageBody = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(message.Body?.ToString()));
            if (string.IsNullOrWhiteSpace(messageBody))
            {
                return null;
            }

            _logger.LogInformation($"Received message: {messageBody}");
            return JsonSerializer.Deserialize<TrainModelMessage>(messageBody);
        }
    }
}
