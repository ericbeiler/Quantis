using System;

namespace Visavi.Quantis.Data
{
    public enum ModelState
    {
        Deleted = -2,
        Failed = -1,
        Created = 0,
        Training = 1,
        Trained = 2
    };

    internal class CompositeModelRecord
    {
        public int Id { get; set; }
        public string? Parameters { get; set; }
        public string? ModelQuality { get; set; }
        public double? QualityScore { get; set; }
        public ModelState? State { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedTimestamp { get; set; }
    }

    public static class CompositeModelExtensions
    {
        private const string defaultDateTimeFormat = "g";
        internal static string GetDefaultName(this CompositeModelRecord model)
        {
            return $"{model.CreatedTimestamp.ToString(defaultDateTimeFormat)}";
        }

        internal static string GetDefaultDescription(this CompositeModelRecord model)
        {
            return $"State: {model.State}, Quality: {model.QualityScore}";
        }

        internal static ModelSummary ToModelSummary(this CompositeModelRecord model)
        {
            return new ModelSummary(model.Id, model.CreatedTimestamp, model.Name ?? model.GetDefaultName(), model.Description ?? model.GetDefaultDescription(), ModelType.Composite, model.QualityScore);
        }
    }

}
