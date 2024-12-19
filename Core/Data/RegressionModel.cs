using Microsoft.Extensions.Logging;
using Microsoft.ML.Data;
using Microsoft.ML.AutoML;
using System.Text;
using Visavi.Quantis.Modeling;
using static Microsoft.ML.DataOperationsCatalog;
using Microsoft.ML;
using Microsoft.ML.Runtime;

namespace Visavi.Quantis.Data
{
    public class RegressionModel
    {
        private const double testSampling = 0.2;
        private const string datetimeTagFormat = "yyMMddHHmm";

        private const int defaultMaxTrainingTimeInSeconds = 300;

        private static readonly string[] numericInputColumnNames = [
                    nameof(PredictionModelInput.MarketCap), nameof(PredictionModelInput.PriceToEarningsQuarterly), nameof(PredictionModelInput.PriceToEarningsTTM)
                    , nameof(PredictionModelInput.PriceToSalesQuarterly), nameof(PredictionModelInput.PriceToSalesTTM), nameof(PredictionModelInput.PriceToBookValue)
                    , nameof(PredictionModelInput.PriceToFreeCashFlowQuarterly), nameof(PredictionModelInput.PriceToFreeCashFlowTTM), nameof(PredictionModelInput.EnterpriseValue)
                    , nameof(PredictionModelInput.EnterpriseValueToEBITDA), nameof(PredictionModelInput.EnterpriseValueToSales), nameof(PredictionModelInput.EnterpriseValueToFreeCashFlow)
                    , nameof(PredictionModelInput.BookToMarketValue), nameof(PredictionModelInput.OperatingIncomeToEnterpriseValue), nameof(PredictionModelInput.AltmanZScore)
                    , nameof(PredictionModelInput.DividendYield), nameof(PredictionModelInput.PriceToEarningsAdjusted)];

        private readonly ILogger _logger;
        private readonly DateTime _timestamp = DateTime.Now;
        private MLContext _mlContext = new MLContext();
        private readonly CancellationToken _stoppingToken;
        private IDataView _modelingDataset;
        private IDataServices _dataServices;
        private uint _maxTrainingTimeInSeconds;

        public RegressionModel(IDataServices dataServices, ILogger logger, string indexTicker, int tagetDurationInMonths, TrainingAlgorithm algorithm, int? compositeId = null, uint? maxTrainingTimeInSeconds = null)
        {
            Timestamp = DateTime.Now;
            _dataServices = dataServices;
            _logger = logger;
            IndexTicker = indexTicker;
            TargetDurationInMonths = tagetDurationInMonths;
            TrainingAlgorithm = algorithm;
            _maxTrainingTimeInSeconds = maxTrainingTimeInSeconds ?? defaultMaxTrainingTimeInSeconds;
            CompositeId = compositeId;

            _mlContext.Log += (_, e) => logMlMessage(e.Kind, e.Message);
        }

        public int? CompositeId { get; }
        public DateTime Timestamp { get; } 
        public string IndexTicker { get; }
        public int TargetDurationInMonths { get; }
        public TrainingAlgorithm TrainingAlgorithm { get; }
        public DataViewSchema? TrainingSchema => _modelingDataset?.Schema;
        public ITransformer? Predictor { get; private set; }
        public RegressionMetrics? Metrics { get; private set; }

        private TrainTestData _trainAndTestData;

        private IDataView buildDataLoader(int? datasetSizeLimit = null)
        {
            //Create ML Context with seed for repeteable/deterministic results
            var loader = _mlContext.Data.CreateDatabaseLoader<PredictionModelInput>();
            return loader.Load(_dataServices.EquityArchives.GetTrainingDataQuerySource(IndexTicker, TargetDurationInMonths, datasetSizeLimit));
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
                                        .Append(_mlContext.Transforms.Concatenate("Features", numericInputColumnNames));

            return dataProcessPipeline;
        }

        private ITransformer buildFastTreeModel()
        {
            var dataProcessPipeline = buildDataPipeline();
            var trainer = _mlContext.Regression.Trainers.FastTree();

            var trainingPipeline = dataProcessPipeline.Append(trainer);

            // Training the model
            _logger.LogInformation($"Training Model, Index: {IndexTicker}, Target Duration: {TargetDurationInMonths}");

            // _mlContext.Regression.CrossValidate(_modelingDataset, trainingPipeline, numberOfFolds: 4);
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

        public RegressionMetrics Evaluate()
        {
            DateTime evaluationStart = DateTime.Now;
            if (Predictor == null)
            {
                throw new InvalidOperationException("Model has not been trained yet.");
            }

            // Testing the model
            _logger.LogInformation($"Testing Model:");
            IDataView predictions = Predictor.Transform(_trainAndTestData.TestSet);
            Metrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: "Label", scoreColumnName: "Score");

            int testingMinutes = Convert.ToInt32(Math.Ceiling((DateTime.Now - evaluationStart).TotalMinutes));
            _logger.LogInformation($"Completed Testing in {testingMinutes} minute(s):\n\tRoot Mean Squared Error: {Metrics.RootMeanSquaredError}\n\tAbsolute Error: {Metrics.MeanAbsoluteError}\n\tR Squared: {Metrics.RSquared}");
            return Metrics;
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

        /// <summary>
        /// Saves the model to it's default spot in Blob Storage
        /// </summary>
        /// <returns></returns>
        public async Task Save()
        {
            // Saving the model with a default name
            await _dataServices.PredictionModels.SaveModel($"Regression-{CompositeId}-{IndexTicker}-{TargetDurationInMonths}-{_timestamp.ToString(datetimeTagFormat)}", this);
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

            _trainAndTestData = _mlContext.Data.TrainTestSplit(_modelingDataset, testFraction: testSampling);

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
