using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public CompositeModelDetails(TrainingParameters trainingParameters, ModelSummary modelSummary, IEnumerable<CagrRegressionModelRecord> regressionModels)
        {
            TrainingParameters = trainingParameters;
            Id = modelSummary.Id;
            Name = modelSummary.Name;
            Description = modelSummary.Description;
            Type = modelSummary.Type;
            QualityScore = modelSummary.QualityScore;
        }

        TrainingParameters TrainingParameters { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ModelType Type { get; set; }
        public double? QualityScore { get; set; }

    }
}
