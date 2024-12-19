using Microsoft.ML;
using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    internal class ModelTrainingJob
    {
        private const float l1RegularizationValue = 2.0f;
        private const float l2RegularizationValue = 0.5f;

        private readonly ILogger _logger;
        private readonly DateTime _timestamp = DateTime.Now;
        private readonly CancellationToken _stoppingToken;
        private readonly TrainModelMessage _trainingParameters;
        private IDataView _modelingDataset;
        private IDataServices _dataServices;

        internal ModelTrainingJob(TrainModelMessage trainingParameters, IDataServices dataServices, ILogger logger, CancellationToken stoppingToken)
        {
            _logger = logger;
            _stoppingToken = stoppingToken;
            _trainingParameters = trainingParameters;
            _dataServices = dataServices;
        }

        public string IndexTicker => _trainingParameters.Index ?? "";

        public TrainingAlgorithm Algorithm => _trainingParameters.Algorithm ?? TrainingAlgorithm.FastTree;

        public TimeSpan? MaxTrainingTime => _trainingParameters.MaxTrainingTime;

        public DateTime Timestamp => _timestamp;

        internal async Task ExecuteAsync()
        {
            var targetDurations = _trainingParameters.TargetDurationsInMonths == null || _trainingParameters.TargetDurationsInMonths.Length == 0
                                        ? TrainModelMessage.DefaultDurations : _trainingParameters.TargetDurationsInMonths;

            foreach (int targetDuration in targetDurations)
            {
                try
                {
                    if (_stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Model Training Job was cancelled.");
                        return;
                    }
                    await generateModel(targetDuration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error training model for {targetDuration} months.");
                }
            }
        }

        private async Task generateModel(int targetDurationInMonths)
        {
            var totalSeconds = MaxTrainingTime.HasValue ? (uint?)Convert.ToUInt32(MaxTrainingTime?.TotalSeconds) : null;
            var regressionModel = new RegressionModel(_dataServices, _logger, IndexTicker, targetDurationInMonths,
                                                        _trainingParameters.Algorithm ?? TrainingAlgorithm.FastTree,
                                                        _trainingParameters.CompositeModelId, totalSeconds);
            _ = regressionModel.Train();
            _ = regressionModel.Evaluate();
            await regressionModel.Save();

            _logger.LogInformation($"Validating Model:");

        }
    }
}
