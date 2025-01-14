using System;

namespace Visavi.Quantis.Data
{
    public enum ModelState
    {
        Created,
        Training,
        Trained,
        Failed
    };

    internal class CompositeModelRecord
    {
        public int Id { get; set; }
        public string? Parameters { get; set; }
        public string? ModelQuality { get; set; }
        public double? QualityScore { get; set; }
        public ModelState? State { get; set; } = ModelState.Created;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedTimestamp { get; set; }
    }

    public static class CompositeModelExtensions
    {
        private const string defaultDateTimeFormat = "g";
        internal static string GetName(this CompositeModelRecord model)
        {
            return $"{model.CreatedTimestamp.ToString(defaultDateTimeFormat)}";
        }

        internal static string GetDescription(this CompositeModelRecord model)
        {
            return $"State: {model.State}, Quality: {model.QualityScore}";
        }

        internal static ModelSummary ToModelSummary(this CompositeModelRecord model)
        {
            return new ModelSummary(model.Id, model.CreatedTimestamp, model.GetName(), model.GetDescription(), ModelType.Composite, model.QualityScore);
        }
    }

}
