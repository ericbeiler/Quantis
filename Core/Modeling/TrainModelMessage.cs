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
}
