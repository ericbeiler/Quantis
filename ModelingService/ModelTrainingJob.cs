using Microsoft.ML;
using System.Text.Json;

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
        private IOrchestrator _dataServices;
        private IPredictionService _predictionService;

        internal ModelTrainingJob(TrainingParameters trainingParameters, IOrchestrator dataServices, IPredictionService predictionService, ILogger logger)
        {
            _logger = logger;
            _stoppingToken = new CancellationToken();
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
            _logger.LogInformation($"Executing Training Job, {_trainingParameters}");
            if (_trainingParameters == null)
            {
                throw new NullReferenceException("Training Parameters are required to train a model.");
            }

            if (_dataServices == null)
            {
                throw new NullReferenceException("Data Services are required to train a model.");
            }

            if (_trainingParameters.CompositeModelId != null)
            {
                await _dataServices.PredictionModels.UpdateModelState(_trainingParameters.CompositeModelId.Value, ModelState.Training);
            }

            var targetDurations = _trainingParameters.TargetDurationsInMonths == null || _trainingParameters.TargetDurationsInMonths.Length == 0
                                        ? TrainingParameters.DefaultDurations : _trainingParameters.TargetDurationsInMonths;

            bool fullyTrained = true;
            List<RegressionModelQualityMetrics?> cagrModelMetrics = new List<RegressionModelQualityMetrics?>();
            foreach (int targetDuration in targetDurations)
            {
                try
                {
                    if (_stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Model Training Job was cancelled.");
                        return;
                    }
                    var individualModelMetrics = await generateModel(targetDuration);
                    cagrModelMetrics.Add(individualModelMetrics);
                }
                catch (Exception ex)
                {
                    fullyTrained = false;
                    _logger.LogError(ex, $"Error training model for {targetDuration} months.");
                }
            }

            var averageRSquared = cagrModelMetrics.Average(m => m?.RSquared ?? 0);
            var averagePearsonCorrelation = cagrModelMetrics.Average(m => m?.CrossValidationMetrics?.AveragePearsonCorrelation ?? 0);
            var averageSpearmanRankCorrelation = cagrModelMetrics.Average(m => m?.CrossValidationMetrics?.AverageSpearmanRankCorrelation ?? 0);
            var averageMinPearsonCorrelation= cagrModelMetrics.Average(m => m?.CrossValidationMetrics?.MinimumPearsonCorrelation ?? 0);
            var averageMinSpearmanRankCorrelation = cagrModelMetrics.Average(m => m?.CrossValidationMetrics?.MinimumSpearmanRankCorrelation ?? 0);
            int qualityScore = Convert.ToInt32(100 * Math.Pow(averageRSquared * averagePearsonCorrelation * averageSpearmanRankCorrelation * averageMinPearsonCorrelation * averageMinSpearmanRankCorrelation, 0.2));

            try
            {
                _logger.LogInformation($"Caching Quality Score and Trinaing State for model {_trainingParameters.CompositeModelId}. Score: {qualityScore}");
                if (_trainingParameters.CompositeModelId != null)
                {
                    await _dataServices.PredictionModels.UpdateQualityScore(_trainingParameters.CompositeModelId.Value, qualityScore);
                    await _dataServices.PredictionModels.UpdateModelState(_trainingParameters.CompositeModelId.Value, fullyTrained ? ModelState.Publishing : ModelState.Failed);
                }

                await cacheTickerProjections();
                if (_trainingParameters.CompositeModelId != null && fullyTrained)
                {
                    await _dataServices.PredictionModels.UpdateModelState(_trainingParameters.CompositeModelId.Value, ModelState.Ready);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unable to cache model predictions.");
            }
            _logger.LogInformation($"Model Training Job completed for Composite {_trainingParameters.CompositeModelId}.");
        }

        private async Task<RegressionModelQualityMetrics?> generateModel(int targetDurationInMonths)
        {
            RegressionModelQualityMetrics? modelQualityMetrics = null;
            try
            {
                var totalSeconds = MaxTrainingTime.HasValue ? (uint?)Convert.ToUInt32(MaxTrainingTime?.TotalSeconds) : null;
                var regressionModel = new RegressionModel(_dataServices, _logger, IndexTicker, targetDurationInMonths,
                                                            _trainingParameters.Algorithm ?? TrainingAlgorithm.FastTree,
                                                            _trainingParameters.CompositeModelId, totalSeconds,
                                                            _trainingParameters.DatasetSizeLimit, _trainingParameters.NumberOfTrees,
                                                            _trainingParameters.NumberOfLeaves, _trainingParameters.MinimumExampleCountPerLeaf,
                                                            _trainingParameters.Granularity, _trainingParameters.Features);
                _ = regressionModel.Train();
                modelQualityMetrics = regressionModel.Evaluate();
                await regressionModel.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unable to generate a model for composite {_trainingParameters.CompositeModelId}");
            }
            return modelQualityMetrics;
        }

        async Task cacheTickerProjections()
        {
            try
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
                else
                {
                    _logger.LogWarning("Composite Model Id is not set. Unable to cache ticker projections.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to cache ticker projections.");
            }
        }
    }
}
