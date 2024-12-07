using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visavi.Quantis.Data
{
    public interface ITrainedPredictionModel
    {
        DateTime Timestamp { get; } 
        string TickerIndex { get; }
        int TargetDuration { get; }
        MLContext MLContext { get; }
        DataViewSchema TrainingSchema { get; }
        ITransformer Transformer { get; }
        RegressionMetrics Metrics { get; }
    }
}
