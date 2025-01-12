using System;

namespace Visavi.Quantis.Data
{
    public class ModelSummary
    {
        internal ModelSummary(int id, string name, string description, ModelType type)
        {
            Id = id;
            Name = name;
            Description = description;
            Type = type;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ModelType Type { get; set; }
        public double QualityScore { get; set; }
    }
}
