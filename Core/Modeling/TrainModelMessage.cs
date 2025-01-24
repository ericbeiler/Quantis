using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    public class TrainModelMessage
    {
        public string Message = "Train Model";

        public string? ModelName { get; }
        public string? ModelDescription { get; }
        public TrainingParameters TrainingParameters { get; }

        public TrainModelMessage(TrainingParameters trainingParameters, string? modelName = null, string? modelDescription = null)
        {
            TrainingParameters = trainingParameters;
            ModelName = modelName;
            ModelDescription = modelDescription;
        }
    }

    public static class TrainModelMessageExtensions
    {
        public static TrainModelMessage? ToTrainModelMessage(this QueueMessage message, ILogger? debugLogger = null)
        {
            return message?.Body.ToTrainModelMessage(debugLogger);
        }

        public static TrainModelMessage? ToTrainModelMessage(this BinaryData binaryData, ILogger? debugLogger = null)
        {
            if (binaryData == null)
            {
                return null;
            }

            // Deserialize the message body
            string messageBody = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(binaryData.ToString()));
            if (string.IsNullOrWhiteSpace(messageBody))
            {
                return null;
            }

            JsonSerializerOptions options = new JsonSerializerOptions() { RespectNullableAnnotations = true };
            debugLogger?.LogDebug($"Deserializing message to TrainModelMessage: {messageBody}");
            return JsonSerializer.Deserialize<TrainModelMessage>(messageBody, options);
        }
    }
}
