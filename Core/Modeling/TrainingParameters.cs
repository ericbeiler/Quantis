using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    public class TrainingParameters
    {
        private const int OneYear = 12;
        private const int TwoYears = 24;
        private const int ThreeYears = 36;
        private const int FiveYears = 60;
        public static readonly int[] ValidDurations = { OneYear, TwoYears, ThreeYears, FiveYears };
        public static readonly int[] DefaultDurations = ValidDurations;

        private int[]? _targetDurationsInMonths = { };

        public int? CompositeModelId { get; set; }
        public int[]? TargetDurationsInMonths
        {
            get
            {
                return _targetDurationsInMonths;
            }
            set
            {
                if (value != null && value.Any(duration => !PricePointPredictor.IsValidDuration(duration)))
                {
                    throw new ArgumentException($"{value} is not a valid Target Duration. Try 12, 24, 36, or 60.");
                }
                _targetDurationsInMonths = value;
            }
        }

        public string? Index { get; set; }
        public int? DatasetSizeLimit { get; set; }
        public TrainingAlgorithm? Algorithm { get; set; }
        public TimeSpan? MaxTrainingTime { get; set; }

        public int? NumberOfTrees { get; set; }
        public int? NumberOfLeaves { get; set; }
        public int? MinimumExampleCountPerLeaf { get; set; }
    }
}
