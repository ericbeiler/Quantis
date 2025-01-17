using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    public class TrainModelsService : BackgroundService
    {
        private readonly ILogger<TrainModelsService> _logger;
        private readonly IDataServices _dataServices;
        private readonly IPredictionService _predictionService;
        private readonly QueueClient _queueClient = new QueueClient(StorageConnectionString, "quantis-modeling");

        internal const string StorageConnectionString = "BlobEndpoint=https://quantis.blob.core.windows.net/;QueueEndpoint=https://quantis.queue.core.windows.net/;FileEndpoint=https://quantis.file.core.windows.net/;TableEndpoint=https://quantis.table.core.windows.net/;SharedAccessSignature=sv=2022-11-02&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2025-11-17T02:13:18Z&st=2024-11-16T18:13:18Z&spr=https&sig=iKRDTg8msgsX8pPrgbJo%2Fm2gZam8JxDrF%2B11PU2KsZU%3D";

        public TrainModelsService(IDataServices dataServices, IPredictionService predictionService, ILogger<TrainModelsService> logger)
        {
            _dataServices = dataServices;
            _predictionService = predictionService;
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
                    if (poppedMessage != null)
                    {
                        var queueMessage = decodeMessage(poppedMessage);

                        // Check the message is a valid queue message
                        if (queueMessage == null)
                        {
                            _logger.LogWarning($"{poppedMessage?.MessageId} is not in a valid format. Deleting...");
                            await deleteQueueMessage(poppedMessage, cancellationToken);
                            continue;
                        }
                        _logger.LogInformation($"Processing message {poppedMessage?.MessageId}.");

                        // Check the message is a valid training message
                        if (queueMessage?.TrainingParameters == null)
                        {
                            _logger.LogError($"No training parameters found in message {poppedMessage?.MessageId}. Cancelling Message.");
                            await deleteQueueMessage(poppedMessage, cancellationToken);
                            continue;
                        }

                        // Run Training
                        _logger.LogError($"Training commenced for message {poppedMessage?.MessageId}, {queueMessage.TrainingParameters}");
                        var trainingJob = new ModelTrainingJob(queueMessage.TrainingParameters, _dataServices, _predictionService, _logger);
                        await trainingJob.ExecuteAsync();

                        _logger.LogInformation($"Deleting message {poppedMessage?.MessageId}.");
                        await deleteQueueMessage(poppedMessage, cancellationToken);
                    }
                    else
                    {
                        _logger.LogInformation("No model message is in the queue, delaying 1 minute.");
                        await Task.Delay(60000, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not build model for message {poppedMessage?.MessageId}\n{ex.ToString()}.");
                    await deleteQueueMessage(poppedMessage, cancellationToken);
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
            string messageBody = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(message.Body.ToString()));
            if (string.IsNullOrWhiteSpace(messageBody))
            {
                return null;
            }

            JsonSerializerOptions options = new JsonSerializerOptions() { RespectNullableAnnotations = true };
            _logger.LogInformation($"Received message: {messageBody}");
            return JsonSerializer.Deserialize<TrainModelMessage>(messageBody, options);
        }

        private async Task<Response?> deleteQueueMessage(QueueMessage? message, CancellationToken cancellationToken)
        {
            try
            {
                if (message != null)
                {
                    _logger.LogInformation($"Deleting message {message?.MessageId}");
                    return await _queueClient.DeleteMessageAsync(message?.MessageId, message?.PopReceipt, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Could not delete message {message?.MessageId}");
            }
            return null;
        }
    }
}
