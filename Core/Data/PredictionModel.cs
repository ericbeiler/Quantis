namespace Visavi.Quantis.Data
{
    public class PredictionModel
    {
        private const int OneYear = 12;
        private const int TwoYears = 24;
        private const int ThreeYears = 36;
        private const int FiveYears = 60;

        public PredictionModel(PredictionModelSummary summary, BinaryData inferencingModel)
        {
            Id = summary.Id;
            Type = summary.Type;
            Index = summary.Index;
            TargetDuration = summary.TargetDuration;
            Timestamp = summary.Timestamp;
            Path = summary.Path;
            MeanAbsoluteError = summary.MeanAbsoluteError;
            RootMeanSquaredError = summary.RootMeanSquaredError;
            LossFunction = summary.LossFunction;
            RSquared = summary.RSquared;
            InferencingModel = inferencingModel;
        }

        public int? Id { get; set; }
        public string Type { get; set; }
        public string Index { get; set; }
        public int TargetDuration { get; set; }
        public DateTime Timestamp { get; set; }
        public string Path { get; set; }
        public double MeanAbsoluteError { get; set; }
        public double RootMeanSquaredError { get; set; }
        public double LossFunction { get; set; }
        public double RSquared { get; set; }
        public BinaryData InferencingModel { get; set; }

        public static bool IsValidDuration(int durationInMonths)
        {
            switch (durationInMonths)
            {
                case OneYear:
                case TwoYears:
                case ThreeYears:
                case FiveYears:
                    return true;

                default:
                    return false;
            }
        }

    }

    public static class PredictionModelExtensions
    {
        public static Stream GetModelStream(this PredictionModel model)
        {
            return model.InferencingModel.ToStream();
        }
    }
}
