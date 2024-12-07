using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    public class TrainModelMessage
    {
        private const int OneYear = 12;
        private const int TwoYears = 24;
        private const int ThreeYears = 36;
        private const int FiveYears = 60;

        private int _targetDurationInMonths = ThreeYears;

        public string Message = "Train Model";
        public int TargetDurationInMonths
        {
            get
            {
                return _targetDurationInMonths;
            }
            set
            {
                if (PredictionModel.IsValidDuration(value))
                {
                    _targetDurationInMonths = value;
                }
                else
                {
                    throw new ArgumentException($"{value} is not a valid Target Duration. Try 12, 24, 36, or 60.");
                }
            }
        }

        public string? Index { get; set; }
        public int? DatasetSizeLimit { get; set; }
    }
}
