using Microsoft.Extensions.Logging;
using Microsoft.ML.Data;
using Microsoft.ML.AutoML;
using System.Text;
using static Microsoft.ML.DataOperationsCatalog;
using Microsoft.ML;
using Microsoft.ML.Runtime;
using Visavi.Quantis.Data;
using static Microsoft.ML.TrainCatalogBase;
using Tensorflow;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Visavi.Quantis.Modeling
{
    public class RegressionModel
    {
        private class PredictionResult
        {
            public float Score { get; set; }
        }

        private const int defaultCrossValidationFolds = 4;
        private const int defaultNumberOfTrees = 100;
        private const int defaultNumberOfLeaves = 50;
        private const int defaultMinimumExampleCountPerLeaf = 30;

        private const double defaultTestSampling = 0.2;
        private const string datetimeTagFormat = "yyMMddHHmm";

        private const int defaultMaxTrainingTimeInSeconds = 300;

        private static readonly string[] numericInputColumnNames = [
                    nameof(PredictionModelInput.MarketCap), nameof(PredictionModelInput.PriceToEarningsQuarterly), nameof(PredictionModelInput.PriceToEarningsTTM)
                    , nameof(PredictionModelInput.PriceToSalesQuarterly), nameof(PredictionModelInput.PriceToSalesTTM), nameof(PredictionModelInput.PriceToBookValue)
                    , nameof(PredictionModelInput.PriceToFreeCashFlowQuarterly), nameof(PredictionModelInput.PriceToFreeCashFlowTTM), nameof(PredictionModelInput.EnterpriseValue)
                    , nameof(PredictionModelInput.EnterpriseValueToEBITDA), nameof(PredictionModelInput.EnterpriseValueToSales), nameof(PredictionModelInput.EnterpriseValueToFreeCashFlow)
                    , nameof(PredictionModelInput.BookToMarketValue), nameof(PredictionModelInput.OperatingIncomeToEnterpriseValue), nameof(PredictionModelInput.AltmanZScore)
                    , nameof(PredictionModelInput.DividendYield), nameof(PredictionModelInput.PriceToEarningsAdjusted)];

        private static readonly string[] DefaultInputs = numericInputColumnNames; // Include all data as inputs by default

        private readonly ILogger _logger;
        private readonly DateTime _timestamp = DateTime.Now;
        private MLContext _mlContext = new MLContext();
        private readonly CancellationToken _stoppingToken;
        private IDataView _modelingDataset;
        private IDataServices _dataServices;
        private uint _maxTrainingTimeInSeconds;
        private int? _datasetSizeLimit;
        private readonly int _numberOfTrees;
        private readonly int _numberOfLeaves;
        private readonly int _minimumExampleCountPerLeaf;
        private readonly TrainingGranularity _grainularity;
        private IReadOnlyList<TrainCatalogBase.CrossValidationResult<RegressionMetrics>> _crossValidationResults;

        public RegressionModel(IDataServices dataServices, ILogger logger, string indexTicker, int tagetDurationInMonths, TrainingAlgorithm algorithm, int? compositeId = null,
                                uint? maxTrainingTimeInSeconds = null, int? datasetSizeLimit = null, int? numberOfTrees = null, int? numberOfLeaves = null,
                                int? minimumExampleCountPerLeaf = null, TrainingGranularity? grainularity = null, string[]? featureList = null)
        {
            Timestamp = DateTime.Now;
            _dataServices = dataServices;
            _logger = logger;
            IndexTicker = indexTicker;
            TargetDurationInMonths = tagetDurationInMonths;
            TrainingAlgorithm = algorithm;
            _maxTrainingTimeInSeconds = maxTrainingTimeInSeconds ?? defaultMaxTrainingTimeInSeconds;
            CompositeId = compositeId;
            _datasetSizeLimit = datasetSizeLimit;
            _numberOfTrees = numberOfTrees ?? defaultNumberOfTrees;
            _numberOfLeaves = numberOfLeaves ?? defaultNumberOfLeaves;
            _minimumExampleCountPerLeaf = minimumExampleCountPerLeaf ?? defaultMinimumExampleCountPerLeaf;
            _grainularity = grainularity ?? TrainingGranularity.Daily;
            Features = featureList.IsNullOrEmpty() ? DefaultInputs : featureList ?? DefaultInputs;

            _mlContext.Log += (_, e) => logMlMessage(e.Kind, e.Message);
        }

        public int? CompositeId { get; }
        public string[] Features { get; }
        public DateTime Timestamp { get; }
        public string IndexTicker { get; }
        public int TargetDurationInMonths { get; }
        public TrainingAlgorithm TrainingAlgorithm { get; }
        public DataViewSchema? TrainingSchema => _modelingDataset?.Schema;
        public ITransformer? Predictor { get; private set; }
        public RegressionModelQualityMetrics? Metrics { get; private set; }

        private TrainTestData _trainAndTestData;

        private IDataView buildDataLoader()
        {
            //Create ML Context with seed for repeteable/deterministic results
            var loader = _mlContext.Data.CreateDatabaseLoader<PredictionModelInput>();
            return loader.Load(_dataServices.EquityArchives.GetTrainingDataQuerySource(IndexTicker, TargetDurationInMonths, _grainularity, _datasetSizeLimit));
        }

        private IEstimator<ITransformer> buildDataPipeline()
        {
            var dataProcessPipeline = _mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(PredictionModelInput.Cagr))
                                        .Append(_mlContext.Transforms.Conversion.ConvertType([
                                                new InputOutputColumnPair(nameof(PredictionModelInput.MarketCap)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.PriceToEarningsQuarterly)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.PriceToEarningsTTM)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.PriceToSalesQuarterly)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.PriceToSalesTTM)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.PriceToBookValue)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.PriceToFreeCashFlowQuarterly)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.PriceToFreeCashFlowTTM)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.EnterpriseValue)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.EnterpriseValueToEBITDA)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.EnterpriseValueToSales)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.EnterpriseValueToFreeCashFlow)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.BookToMarketValue)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.OperatingIncomeToEnterpriseValue)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.AltmanZScore)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.DividendYield)),
                                                    new InputOutputColumnPair(nameof(PredictionModelInput.PriceToEarningsAdjusted))
                                        ], DataKind.Single))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.MarketCap)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.PriceToEarningsQuarterly)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.PriceToEarningsTTM)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.PriceToSalesQuarterly)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.PriceToSalesTTM)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.PriceToBookValue)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.PriceToFreeCashFlowQuarterly)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.PriceToFreeCashFlowTTM)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.EnterpriseValue)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.EnterpriseValueToEBITDA)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.EnterpriseValueToSales)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.EnterpriseValueToFreeCashFlow)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.BookToMarketValue)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.OperatingIncomeToEnterpriseValue)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.AltmanZScore)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.DividendYield)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(PredictionModelInput.PriceToEarningsAdjusted)))
                                        .Append(_mlContext.Transforms.Concatenate("Features", Features));

            return dataProcessPipeline;
        }

        private ITransformer buildFastTreeModel()
        {
            var dataProcessPipeline = buildDataPipeline();
            var trainer = _mlContext.Regression.Trainers.FastTree(numberOfTrees: _numberOfTrees, numberOfLeaves: _numberOfLeaves, minimumExampleCountPerLeaf: _minimumExampleCountPerLeaf);

            var trainingPipeline = dataProcessPipeline.Append(trainer);

            // Log start of cross-validation
            _logger.LogInformation($"Performing Cross-Validation, Index: {IndexTicker}, Target Duration: {TargetDurationInMonths}");

            // Perform cross-validation
            _crossValidationResults = _mlContext.Regression.CrossValidate(
                            data: _trainAndTestData.TrainSet,
                            estimator: trainingPipeline,
                            numberOfFolds: defaultCrossValidationFolds
                        );

            // Log cross-validation metrics
            foreach (var result in _crossValidationResults)
            {
                _logger.LogInformation($"Fold: {result.Fold}, R^2: {result.Metrics.RSquared}, RMSE: {result.Metrics.RootMeanSquaredError}");
            }

            // Training the model
            _logger.LogInformation($"Training Model, Index: {IndexTicker}, Target Duration: {TargetDurationInMonths}");

            return trainingPipeline.Fit(_trainAndTestData.TrainSet);
        }

        private ITransformer? buildAutoMLModel(uint? maxTrainingTimeInSeconds)
        {
            try
            {
                _logger.LogInformation($"Training AutoML Model, Index: {IndexTicker}, Target Duration: {TargetDurationInMonths}");

                SweepablePipeline sweepablePipeline = _mlContext.Auto().Featurizer(_modelingDataset, numericColumns: numericInputColumnNames)
                                                        .Append(buildDataPipeline());

                AutoMLExperiment experiment = _mlContext.Auto().CreateExperiment();
                experiment.SetPipeline(sweepablePipeline)
                          .SetRegressionMetric(RegressionMetric.RSquared)
                          .SetTrainingTimeInSeconds(maxTrainingTimeInSeconds ?? defaultMaxTrainingTimeInSeconds)
                          .SetDataset(_trainAndTestData);

                var trialResult = experiment.Run();
                if (trialResult != null)
                {
                    _logger.LogInformation($"AutoML Model Training Completed, Loss: {trialResult?.Loss}, Metric: {trialResult?.Metric}");

                    // Validate the Test Set Data
                    IDataView predictions = trialResult.Model.Transform(_trainAndTestData.TestSet);
                    _logger.LogInformation($"AutoML Model Output Schema: {predictions.Schema}");
                }

                return trialResult?.Model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to train the Auto ML Model.");
                throw;
            }
        }

        private float calculatePearsonCorrelation(float[] x, float[] y)
        {
            var meanX = x.Average();
            var meanY = y.Average();

            var numerator = x.Zip(y, (xi, yi) => (xi - meanX) * (yi - meanY)).Sum();
            var denominator = Math.Sqrt(x.Sum(xi => Math.Pow(xi - meanX, 2)) * y.Sum(yi => Math.Pow(yi - meanY, 2)));

            return (float)(numerator / denominator);
        }

        public RegressionModelQualityMetrics Evaluate()
        {
            DateTime evaluationStart = DateTime.Now;
            if (Predictor == null)
            {
                throw new InvalidOperationException("Model has not been trained yet.");
            }

            // Testing the model
            _logger.LogInformation($"Testing Model:");
            IDataView predictions = Predictor.Transform(_trainAndTestData.TestSet);
            var modelMetrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: "Label", scoreColumnName: "Score");
            var crossValidationMetrics = runCrossValidationAnalysis();
            Metrics = new RegressionModelQualityMetrics(modelMetrics, crossValidationMetrics);

            int testingMinutes = Convert.ToInt32(Math.Ceiling((DateTime.Now - evaluationStart).TotalMinutes));
            _logger.LogInformation($"Completed Testing in {testingMinutes} minute(s):\n\tRoot Mean Squared Error: {Metrics.RootMeanSquaredError}\n\tAbsolute Error: {Metrics.MeanAbsoluteError}\n\tR Squared: {Metrics.RSquared}");
            return Metrics;
        }
        private float calculateSpearmanRankCorrelation(float[] x, float[] y)
        {
            _logger.LogInformation("Calculating Spearman Rank Correlation:");
            // Get ranks for both prediction arrays
            var rankX = getRanksForSpearmanCorrelation(x);
            var rankY = getRanksForSpearmanCorrelation(y);

            // Calculate the Spearman Rank Correlation
            long n = rankX.Length;
            float sumOfSquaredRankDifferences = 0;

            for (int i = 0; i < n; i++)
            {
                float difference = rankX[i] - rankY[i];
                sumOfSquaredRankDifferences += difference * difference;
            }

            long spearmanDenominator = n * (n * n - 1);
            var spearmanRankCorrelation = 1 - ((6 * sumOfSquaredRankDifferences) / spearmanDenominator);
            if (spearmanRankCorrelation < -1)
            {
                _logger.LogWarning("Spearman Rank Correlation is less than -1.");
                _logger.LogInformation($"X Count: {x.Length}, Y Count: {y.Length}, RankX Count: {rankX.Length}, RankY Count: {rankY.Length}");
                _logger.LogInformation($"Squared Rank Differences = {sumOfSquaredRankDifferences}, Denominator {spearmanDenominator}");
            }
            if (spearmanRankCorrelation > 1)
            {
                _logger.LogWarning("Spearman Rank Correlation is greater than 1.");
                _logger.LogInformation($"X Count: {x.Length}, Y Count: {y.Length}, RankX Count: {rankX.Length}, RankY Count: {rankY.Length}");
                _logger.LogInformation($"Squared Rank Differences = {sumOfSquaredRankDifferences}, Denominator {spearmanDenominator}");
            }

            return spearmanRankCorrelation;
        }

        private float[] getRanksForSpearmanCorrelation(float[] values)
        {
            // Rank the values with ties handled
            var indexedValues = values.Select((value, index) => new { Value = value, Index = index })
                                      .OrderBy(x => x.Value)
                                      .ToArray();

            float[] ranks = new float[values.Length];
            int i = 0;

            while (i < indexedValues.Length)
            {
                int start = i;

                // Find all tied values
                while (i + 1 < indexedValues.Length && indexedValues[i + 1].Value == indexedValues[i].Value)
                {
                    i++;
                }

                // Assign the average rank for tied values
                float rank = (start + i + 2) / 2.0f; // Average of positions (rank starts at 1)
                for (int j = start; j <= i; j++)
                {
                    ranks[indexedValues[j].Index] = rank;
                }

                i++;
            }

            return ranks;
        }

        private void logMlMessage(ChannelMessageKind messageType, string message)
        {
            switch (messageType)
            {
                case ChannelMessageKind.Error:
                    _logger.LogError(message);
                    break;

                case ChannelMessageKind.Warning:
                    _logger.LogWarning(message);
                    break;

                case ChannelMessageKind.Info:
                    _logger.LogInformation(message);
                    break;

                default:
                    // do nothing
                    break;
            }
        }

        private CrossValidationMetrics runCrossValidationAnalysis()
        {
            if (_crossValidationResults == null)
            {
                throw new InvalidOperationException("Cross-Validation Results are not available.");
            }

            try
            {
                // Store predictions from each model
                var predictionsList = new List<float[]>();

                foreach (var result in _crossValidationResults)
                {
                    var model = result.Model;

                    // Predict on the same dataset for all models
                    var transformedData = model.Transform(_trainAndTestData.TestSet);
                    var predictions = _mlContext.Data.CreateEnumerable<PredictionResult>(
                        transformedData, reuseRowObject: false
                    )
                    .Select(pred => pred.Score)
                    .ToArray();

                    predictionsList.Add(predictions);
                }

                List<float> pearsonCorrelations = new List<float>();
                List<float> spearmanRankCorrelations = new List<float>();

                // Compare predictions between models
                for (int i = 0; i < predictionsList.Count; i++)
                {
                    for (int j = i + 1; j < predictionsList.Count; j++)
                    {
                        var pearsonCorrelation = calculatePearsonCorrelation(predictionsList[i], predictionsList[j]);
                        _logger.LogInformation($"Pearson Correlation between Model {i} and Model {j}: {pearsonCorrelation}");
                        pearsonCorrelations.Add(pearsonCorrelation);

                        var spearmanRankCorrelation = calculateSpearmanRankCorrelation(predictionsList[i], predictionsList[j]);
                        _logger.LogInformation($"Spearman Rank Correlation between Model {i} and Model {j}: {spearmanRankCorrelation}");
                        spearmanRankCorrelations.Add(spearmanRankCorrelation);
                    }
                }

                // Log cross-validation metrics
                foreach (var result in _crossValidationResults)
                {
                    _logger.LogInformation($"Fold: {result.Fold}, R^2: {result.Metrics.RSquared}, RMSE: {result.Metrics.RootMeanSquaredError}");
                }

                return new CrossValidationMetrics()
                {
                    AveragePearsonCorrelation = pearsonCorrelations.Average(),
                    MinimumPearsonCorrelation = pearsonCorrelations.Min(),
                    AverageSpearmanRankCorrelation = spearmanRankCorrelations.Average(),
                    MinimumSpearmanRankCorrelation = spearmanRankCorrelations.Min(),
                    AverageMeanAbsoluteError = _crossValidationResults.Average(result => result.Metrics.MeanAbsoluteError),
                    MaximumMeanAbsoluteError = _crossValidationResults.Max(result => result.Metrics.MeanAbsoluteError),
                    AverageRootMeanSquaredError = _crossValidationResults.Average(result => result.Metrics.RootMeanSquaredError),
                    MaximumRootMeanSquaredError = _crossValidationResults.Max(result => result.Metrics.RootMeanSquaredError),
                    AverageRSquared = _crossValidationResults.Average(result => result.Metrics.RSquared),
                    MaximumRSquared = _crossValidationResults.Max(result => result.Metrics.RSquared)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating cross-validation model agreement.");
                throw;
            }
        }

        /// <summary>
        /// Saves the model to it's default spot in Blob Storage
        /// </summary>
        /// <returns></returns>
        public async Task Save()
        {
            // Saving the model with a default name
            var defaultModelName = $"Regression-{CompositeId}-{IndexTicker}-{TargetDurationInMonths}-{_timestamp.ToString(datetimeTagFormat)}";
            if (_datasetSizeLimit != null)
            {
                defaultModelName += $"-Limit{_datasetSizeLimit}";
            }
            await _dataServices.PredictionModels.SaveModel(defaultModelName, this);
        }

        public void Save(Stream stream)
        {
            _mlContext.Model.Save(Predictor, TrainingSchema, stream);
        }

        public ITransformer Train()
        {
            // Preparing the data
            _modelingDataset = buildDataLoader();
            StringBuilder sb = new StringBuilder($"Loaded Modeling Dataset:\n{_modelingDataset.Schema}\n");
            _modelingDataset.Schema.ToList().ForEach(column => sb.Append($"\t{column}\n"));
            sb.Append("---------");
            _logger.LogInformation(sb.ToString());

            _trainAndTestData = _mlContext.Data.TrainTestSplit(_modelingDataset, testFraction: defaultTestSampling);

            // Building and Training the Model
            switch (TrainingAlgorithm)
            {
                case TrainingAlgorithm.FastTree:
                    Predictor = buildFastTreeModel();
                    break;

                case TrainingAlgorithm.Auto:
                    Predictor = buildAutoMLModel(_maxTrainingTimeInSeconds);
                    break;

                default:
                    throw new ArgumentException("Invalid Training Algorithm");
            }

            DateTime trainingCompleteTime = DateTime.Now;
            int trainingMinutes = Convert.ToInt32(Math.Ceiling((trainingCompleteTime - _timestamp).TotalMinutes));
            _logger.LogInformation($"Trained Model in {trainingMinutes} minutes");

            return Predictor;
        }

    }
}
