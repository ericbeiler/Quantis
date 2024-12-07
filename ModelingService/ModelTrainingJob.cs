using Microsoft.ML;
using Microsoft.ML.Data;
using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    internal class ModelTrainingJob : ITrainedPredictionModel
    {
        private const float l1RegularizationValue = 2.0f;
        private const float l2RegularizationValue = 0.5f;

        private const double testSampling = 0.2;
        private const string datetimeTagFormat = "yyMMddHHmm";

        private readonly ILogger _logger;
        private readonly DateTime _timestamp = DateTime.Now;
        private readonly MLContext _mlContext = new MLContext();
        private readonly CancellationToken _stoppingToken;
        private readonly TrainModelMessage _trainingParameters;
        private IDataView _modelingDataset;
        private IDataView _trainingData;
        private IDataView _testingData;
        private IEstimator<ITransformer> _trainingPipeline;
        private ITransformer _trainedModel;
        private RegressionMetrics _metrics;
        private IDataServices _dataServices;

        internal ModelTrainingJob(TrainModelMessage trainingParameters, IDataServices dataServices, ILogger logger, CancellationToken stoppingToken)
        {
            _logger = logger;
            _stoppingToken = stoppingToken;
            _trainingParameters = trainingParameters;
            _dataServices = dataServices;
        }

        public string TickerIndex => _trainingParameters.Index;

        public int TargetDuration => _trainingParameters.TargetDurationInMonths;

        public DataViewSchema TrainingSchema => _modelingDataset.Schema;

        public ITransformer Transformer => _trainedModel;

        public RegressionMetrics Metrics => _metrics;

        public DateTime Timestamp => _timestamp;

        public MLContext MLContext => _mlContext;

        internal async Task ExecuteAsync()
        {
            // Preparing the data
            _modelingDataset = buildDataLoader();
            var trainTestData = _mlContext.Data.TrainTestSplit(_modelingDataset, testFraction: testSampling);
            _trainingData = trainTestData.TrainSet;
            _testingData = trainTestData.TestSet;

            // Building the training pipeline
            _trainingPipeline = loadTrainingPipeline();

            // Training the model
            _logger.LogInformation($"Training Model, Index: {_trainingParameters?.Index}, Target Duration: {_trainingParameters?.TargetDurationInMonths}, Dataset Size Limit: {_trainingParameters?.DatasetSizeLimit}");
            _trainedModel = _trainingPipeline.Fit(_trainingData);
            DateTime trainingCompleteTime = DateTime.Now;
            int trainingMinutes = Convert.ToInt32(Math.Ceiling((trainingCompleteTime - _timestamp).TotalMinutes));
            _logger.LogInformation($"Trained Model in {trainingMinutes} minutes");

            // Testing the model
            _logger.LogInformation($"Testing Model:");
            IDataView predictions = _trainedModel.Transform(_testingData);
            _metrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: "Label", scoreColumnName: "Score");

            int testingMinutes = Convert.ToInt32(Math.Ceiling((DateTime.Now - trainingCompleteTime).TotalMinutes));
            _logger.LogInformation($"Completed Testing in {testingMinutes} minute(s):\n\tRoot Mean Squared Error: {_metrics.RootMeanSquaredError}\n\tAbsolute Error: {_metrics.MeanAbsoluteError}\n\tR Squared: {_metrics.RSquared}");

            // Saving the model
            await _dataServices.PredictionModels.SaveModel($"{_trainingParameters?.Index}-{_trainingParameters?.TargetDurationInMonths}-{_timestamp.ToString(datetimeTagFormat)}", this);


            _logger.LogInformation($"Validating Model:");

        }

        private IDataView buildDataLoader(int? datasetSizeLimit = null)
        {
            //Create ML Context with seed for repeteable/deterministic results
            var loader = _mlContext.Data.CreateDatabaseLoader<PredictionModelInput>();
            return loader.Load(_dataServices.EquityArchives.GetTrainingDataQuerySource(_trainingParameters.Index, _trainingParameters.TargetDurationInMonths, datasetSizeLimit));
        }

        private IEstimator<ITransformer> loadTrainingPipeline()
        {
            // STEP 2: Common data process configuration with pipeline data transformations
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
                                        .Append(_mlContext.Transforms.Concatenate("Features", nameof(PredictionModelInput.MarketCap), nameof(PredictionModelInput.PriceToEarningsQuarterly), nameof(PredictionModelInput.PriceToEarningsTTM)
                                                                                , nameof(PredictionModelInput.PriceToSalesQuarterly), nameof(PredictionModelInput.PriceToSalesTTM), nameof(PredictionModelInput.PriceToBookValue)
                                                                                , nameof(PredictionModelInput.PriceToFreeCashFlowQuarterly), nameof(PredictionModelInput.PriceToFreeCashFlowTTM), nameof(PredictionModelInput.EnterpriseValue)
                                                                                , nameof(PredictionModelInput.EnterpriseValueToEBITDA), nameof(PredictionModelInput.EnterpriseValueToSales), nameof(PredictionModelInput.EnterpriseValueToFreeCashFlow)
                                                                                , nameof(PredictionModelInput.BookToMarketValue), nameof(PredictionModelInput.OperatingIncomeToEnterpriseValue), nameof(PredictionModelInput.AltmanZScore)
                                                                                , nameof(PredictionModelInput.DividendYield), nameof(PredictionModelInput.PriceToEarningsAdjusted)));

            // STEP 3: Set the training algorithm, then create and config the modelBuilder - Selected Trainer (SDCA Regression algorithm)                            
            // var trainer = _mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", featureColumnName: "Features", l1Regularization: l1RegularizationValue, l2Regularization: l2RegularizationValue);
            // var trainer = _mlContext.Regression.Trainers.LightGbm(labelColumnName: "Label", featureColumnName: "Features", numberOfLeaves: 20, minimumExampleCountPerLeaf: 10);
            var trainer = _mlContext.Regression.Trainers.FastTree(labelColumnName: "Label", featureColumnName: "Features");

            return dataProcessPipeline.Append(trainer);
        }
    }
}
