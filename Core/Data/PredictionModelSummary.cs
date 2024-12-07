using System;

namespace Visavi.Quantis.Data
{
    public class PredictionModelSummary
    {
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
    }
}
