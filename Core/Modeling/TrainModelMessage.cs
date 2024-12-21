using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    public class TrainModelMessage
    {
        public string Message = "Train Model";

        public TrainingParameters TrainingParameters { get; }

        public TrainModelMessage(TrainingParameters trainingParameters)
        {
            TrainingParameters = trainingParameters;
        }
    }
}
