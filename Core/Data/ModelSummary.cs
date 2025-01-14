using System;

namespace Visavi.Quantis.Data
{
    public class ModelSummary
    {
        internal ModelSummary(int id, DateTime created, string name, string description, ModelType type, double? qualityScore = null)
        {
            Id = id;
            Created = created;
            Name = name;
            Description = description;
            Type = type;
            QualityScore = qualityScore;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ModelType Type { get; set; }
        public double? QualityScore { get; set; }
        public DateTime Created { get; set; }
    }
}
