using Microsoft.ML;
using System.Text.Json;
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
        private readonly TrainingParameters _trainingParameters;
        private IDataServices _dataServices;
        private IPredictionService _predictionService;

        internal ModelTrainingJob(TrainingParameters trainingParameters, IDataServices dataServices, IPredictionService predictionService, 
                                    ILogger logger, CancellationToken stoppingToken)
        {
            _logger = logger;
            _stoppingToken = stoppingToken;
            _trainingParameters = trainingParameters;
            _dataServices = dataServices;
            _predictionService = predictionService;
        }

        public string IndexTicker => _trainingParameters.Index ?? "";

        public TrainingAlgorithm Algorithm => _trainingParameters.Algorithm ?? TrainingAlgorithm.FastTree;

        public TimeSpan? MaxTrainingTime => _trainingParameters.MaxTrainingTime;

        public DateTime Timestamp => _timestamp;

        internal async Task ExecuteAsync()
        {
            if (_trainingParameters == null)
            {
                throw new NullReferenceException("Training Parameters are required to train a model.");
            }

            var targetDurations = _trainingParameters.TargetDurationsInMonths == null || _trainingParameters.TargetDurationsInMonths.Length == 0
                                        ? TrainingParameters.DefaultDurations : _trainingParameters.TargetDurationsInMonths;

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

            try
            {
                await cacheTickerProjections();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unable to cache model predictions.");
            }
            _logger.LogInformation($"Model Training Job completed for Composite {_trainingParameters.CompositeModelId}.");
        }

        private async Task generateModel(int targetDurationInMonths)
        {
            try
            {
                var totalSeconds = MaxTrainingTime.HasValue ? (uint?)Convert.ToUInt32(MaxTrainingTime?.TotalSeconds) : null;
                var regressionModel = new RegressionModel(_dataServices, _logger, IndexTicker, targetDurationInMonths,
                                                            _trainingParameters.Algorithm ?? TrainingAlgorithm.FastTree,
                                                            _trainingParameters.CompositeModelId, totalSeconds,
                                                            _trainingParameters.DatasetSizeLimit);
                _ = regressionModel.Train();
                _ = regressionModel.Evaluate();
                await regressionModel.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unable to generate a model for composite {_trainingParameters.CompositeModelId}");
            }
        }

        async Task cacheTickerProjections()
        {
            int compositeId = _trainingParameters.CompositeModelId ?? 0;
            if (compositeId != 0)
            {
                var indeces = await _dataServices.EquityArchives.GetIndeces();
                foreach (var index in indeces)
                {
                    var predictions = await _predictionService.PredictPriceTrend(compositeId, [index.Ticker]);
                    _logger.LogInformation($"Caching predictions for {index.Ticker}");
                    await _dataServices.Cache.Set(_predictionService.GetCacheKey(compositeId, index.Ticker), predictions);
                }
            }
        }
    }
}
