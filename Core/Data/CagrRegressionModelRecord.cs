using System;

namespace Visavi.Quantis.Data
{
    /// <summary>
    /// This is the data in the EquityModel table (Cagr Regression Models)
    /// </summary>
    public class CagrRegressionModelRecord
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
        public double AveragePearsonCorrelation { get; set; }
        public double MinimumPearsonCorrelation { get; set; }
        public double AverageSpearmanRankCorrelation { get; set; }
        public double MinimumSpearmanRankCorrelation { get; set; }
        public double CrossValAverageMeanAbsoluteError { get; set; }
        public double CrossValMaximumMeanAbsoluteError { get; set; }
        public double CrossValAverageRootMeanSquaredError { get; set; }
        public double CrossValMaximumRootMeanSquaredError { get; set; }
        public double CrossValAverageRSquared { get; set; }
        public double CrossValMaximumRSquared { get; set; }
    }

    public static class CagrRegressionModelExtensions
    {
        public static string GetName(this CagrRegressionModelRecord model)
        {
            return model.Index + "-" + model.TargetDuration + "-" + model.Timestamp.ToString();
        }

        public static string GetDescription(this CagrRegressionModelRecord model)
        {
            return $"Target Duration: {model.TargetDuration}\nMean Absolute Error: {model.MeanAbsoluteError}\n{model.RootMeanSquaredError}\n{model.LossFunction}\n{model.RSquared}";
        }

        public static ModelSummary ToModelSummary(this CagrRegressionModelRecord model)
        {
            return new ModelSummary(model?.Id ?? -1, model?.GetName() ?? string.Empty, model?.GetDescription() ?? string.Empty, ModelType.CagrRegression);
        }
    }
}
