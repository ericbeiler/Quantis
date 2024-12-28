namespace Visavi.Quantis.Modeling
{
    public class CrossValidationMetrics
    {
        public double? AveragePearsonCorrelation { get; set; }
        public double? MinimumPearsonCorrelation { get; set; }
        public double? AverageSpearmanRankCorrelation { get; set; }
        public double? MinimumSpearmanRankCorrelation { get; set; }
        public double? AverageMeanAbsoluteError { get; set; }
        public double? MaximumMeanAbsoluteError { get; set; }
        public double? AverageRootMeanSquaredError { get; set; }
        public double? MaximumRootMeanSquaredError { get; set; }
        public double? AverageRSquared { get; set; }
        public double? MaximumRSquared { get; set; }

    }
}
