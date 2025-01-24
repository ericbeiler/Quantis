using System;
using Visavi.Quantis.Modeling;

namespace Visavi.Quantis.Data
{
    public class CompositeModelDetails
    {
        public CompositeModelDetails(TrainingParameters trainingParameters, ModelSummary? modelSummary = null, IEnumerable<CagrRegressionModelDetails>? regressionModels = null)
        {
            TrainingParameters = trainingParameters;

            Id = modelSummary?.Id;
            Name = modelSummary?.Name ?? "";
            Description = modelSummary?.Description ?? "";
            Type = modelSummary?.Type;
            QualityScore = modelSummary?.QualityScore;

            RegressionModelDetails = regressionModels?.ToArray() ?? [] ;
        }

        public TrainingParameters TrainingParameters { get; set; }
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ModelType? Type { get; set; }

        public CagrRegressionModelDetails[] RegressionModelDetails { get; set; }
        public double? QualityScore { get; set; }

    }
}
