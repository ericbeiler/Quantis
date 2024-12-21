using Microsoft.ML.Data;

namespace Visavi.Quantis.Modeling
{
    internal class PredictionModelOutput
    {
        [ColumnName("Score")]
        public float PredictedCagr { get; set; }
    }
}
