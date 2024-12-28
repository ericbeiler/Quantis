using Microsoft.ML.Data;

namespace Visavi.Quantis.Modeling
{
    public class RegressionModelQualityMetrics
    {
        public double? MeanAbsoluteError { get; }
        public double? MeanSquaredError { get; }
        public double RootMeanSquaredError { get; }
        public double LossFunction { get; }
        public double RSquared { get; }
        public CrossValidationMetrics CrossValidationMetrics { get; }

        internal RegressionModelQualityMetrics(RegressionMetrics regressionMetrics, CrossValidationMetrics crossValidationMetrics)
        {
            MeanAbsoluteError = regressionMetrics.MeanAbsoluteError;
            MeanSquaredError = regressionMetrics.MeanSquaredError;
            RootMeanSquaredError = regressionMetrics.RootMeanSquaredError;
            LossFunction = regressionMetrics.LossFunction;
            RSquared = regressionMetrics.RSquared;
            CrossValidationMetrics = crossValidationMetrics;
        }
    }
}
