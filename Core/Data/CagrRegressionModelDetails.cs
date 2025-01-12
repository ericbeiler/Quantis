using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visavi.Quantis.Data
{
    public class CagrRegressionModelDetails
    {
        public CagrRegressionModelDetails(CagrRegressionModelRecord cagrRegressionModelRecord)
        {
            Id = cagrRegressionModelRecord.Id;
            Type = cagrRegressionModelRecord.Type;
            TargetDuration = cagrRegressionModelRecord.TargetDuration;
            Timestamp = cagrRegressionModelRecord.Timestamp;
            MeanAbsoluteError = cagrRegressionModelRecord.MeanAbsoluteError;
            RootMeanSquaredError = cagrRegressionModelRecord.RootMeanSquaredError;
            LossFunction = cagrRegressionModelRecord.LossFunction;
            RSquared = cagrRegressionModelRecord.RSquared;
            AveragePearsonCorrelation = cagrRegressionModelRecord.AveragePearsonCorrelation;
            MinimumPearsonCorrelation = cagrRegressionModelRecord.MinimumPearsonCorrelation;
            AverageSpearmanRankCorrelation = cagrRegressionModelRecord.AverageSpearmanRankCorrelation;
            MinimumSpearmanRankCorrelation = cagrRegressionModelRecord.MinimumSpearmanRankCorrelation;
            CrossValAverageMeanAbsoluteError = cagrRegressionModelRecord.CrossValAverageMeanAbsoluteError;
            CrossValMaximumMeanAbsoluteError = cagrRegressionModelRecord.CrossValMaximumMeanAbsoluteError;
            CrossValAverageRootMeanSquaredError = cagrRegressionModelRecord.CrossValAverageRootMeanSquaredError;
            CrossValMaximumRootMeanSquaredError = cagrRegressionModelRecord.CrossValMaximumRootMeanSquaredError;
            CrossValAverageRSquared = cagrRegressionModelRecord.CrossValAverageRSquared;
            CrossValMaximumRSquared = cagrRegressionModelRecord.CrossValMaximumRSquared;
        }

        public int? Id { get; set; }
        public string Type { get; set; }
        public int TargetDuration { get; set; }
        public DateTime Timestamp { get; set; }
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
}
