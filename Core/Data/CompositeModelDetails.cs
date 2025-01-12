using System;
using Visavi.Quantis.Modeling;

namespace Visavi.Quantis.Data
{
    public class CompositeModelDetails
    {
        public CompositeModelDetails(int id, string name, string description, ModelType type)
        {
            Id = id;
            Name = name;
            Description = description;
            Type = type;
        }

        public CompositeModelDetails(TrainingParameters trainingParameters, ModelSummary? modelSummary, IEnumerable<CagrRegressionModelDetails> regressionModels)
        {
            TrainingParameters = trainingParameters;

            Id = modelSummary?.Id;
            Name = modelSummary?.Name ?? "";
            Description = modelSummary?.Description ?? "";
            Type = modelSummary?.Type;
            QualityScore = modelSummary?.QualityScore;

            RegressionModelDetails = regressionModels.ToArray();
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
