using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    internal class ModelTrainingJob
    {
        private const decimal minMarketCap = 10000000;
        private const decimal maxMarketCap = 10000000000000;

        private const decimal minPriceToEarnings = 0;
        private const decimal maxPriceToEarnings = 10000;

        private const decimal minPriceToBook = 0;
        private const decimal maxPriceToBook = 1000;

        private const decimal minDividendYield = 0;
        private const decimal maxDividendYield = 100;

        private const decimal minAltmanZScore = 0;
        private const decimal maxAltmanZScore = 100;

        private const decimal minCagr = -100;
        private const decimal maxCagr = 5000;

        private const decimal minPriceToSales = 0;
        private const decimal maxPriceToSales = 10000;

        private const decimal minPriceToCashFlow = 0;
        private const decimal maxPriceToCashFlow = 10000;

        private const float l1RegularizationValue = 2.0f;
        private const float l2RegularizationValue = 0.5f;

        private const double testSampling = 0.2;
        private const string datetimeTagFormat = "yyMMddHHmm";
        private const string equityModelsContainer = "equity-models";
        private const string dbConnectionString = "Server=tcp:quantis.database.windows.net,1433;Initial Catalog=db-quantis;Persist Security Info=False;User ID=ebeiler;Password=1076Roan!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        private const int timeoutInSeconds = 3600;

        private readonly ILogger _logger;
        private readonly DateTime _startTime;
        private readonly MLContext _mlContext = new MLContext(seed: 0);
        private readonly CancellationToken _stoppingToken;
        private readonly TrainModelMessage _trainingParameters;
        private IDataView _modelingDataset;
        private IDataView _trainingData;
        private IDataView _testingData;
        private IEstimator<ITransformer> _trainingPipeline;
        private ITransformer trainedModel;
        private RegressionMetrics _metrics;

        internal ModelTrainingJob(TrainModelMessage trainingParameters, ILogger logger, CancellationToken stoppingToken)
        {
            _logger = logger;
            _mlContext = new MLContext();
            _startTime = DateTime.Now;
            _stoppingToken = stoppingToken;
            _trainingParameters = trainingParameters;
        }

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
            _logger.LogInformation($"Training Model, Index: {_trainingParameters?.Index}, Target Duration: {_trainingParameters?.TargetDuration}");
            var trainedModel = _trainingPipeline.Fit(_trainingData);
            DateTime trainingCompleteTime = DateTime.Now;
            int trainingMinutes = Convert.ToInt32(Math.Ceiling((trainingCompleteTime - _startTime).TotalMinutes));
            _logger.LogInformation($"Trained Model in {trainingMinutes} minutes");

            // Testing the model
            _logger.LogInformation($"Testing Model:");
            IDataView predictions = trainedModel.Transform(_testingData);
            _metrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: "Label", scoreColumnName: "Score");

            int testingMinutes = Convert.ToInt32(Math.Ceiling((DateTime.Now - trainingCompleteTime).TotalMinutes));
            _logger.LogInformation($"Completed Testing in {testingMinutes} minute(s):\n\tRoot Mean Squared Error: {_metrics.RootMeanSquaredError}\n\tAbsolute Error: {_metrics.MeanAbsoluteError}\n\tR Squared: {_metrics.RSquared}");

            // Saving the model
            await saveModel();
        }

        private string geTrainModelQuery()
        {
            var indexFilter = "";
            if (!string.IsNullOrWhiteSpace(_trainingParameters?.Index))
            {
                indexFilter +=
                    @$" AND SimFinId IN
                    (
                        SELECT SimFinId
                        FROM IndexEquities
                        WHERE IndexTicker = '{_trainingParameters.Index}'
                    )";
            }

            return $@"
                        SELECT  Ticker, 
                                [Date], 
                                CAST(MarketCap AS REAL) AS MarketCap,
                                CAST(PriceToEarningsQuarterly AS REAL) AS PriceToEarningsQuarterly,
                                CAST(PriceToEarningsTTM AS REAL) AS PriceToEarningsTTM,
                                CAST(PriceToSalesQuarterly AS REAL) AS PriceToSalesQuarterly,
                                CAST(PriceToSalesTTM AS REAL) AS PriceToSalesTTM,
                                CAST(PriceToBookValue AS REAL) AS PriceToBookValue,
                                CAST(PriceToFreeCashFlowQuarterly AS REAL) AS PriceToFreeCashFlowQuarterly,
                                CAST(PriceToFreeCashFlowTTM AS REAL) AS PriceToFreeCashFlowTTM,
                                CAST(EnterpriseValue AS REAL) AS EnterpriseValue,
                                CAST(EnterpriseValueToEBITDA AS REAL) AS EnterpriseValueToEBITDA,
                                CAST(EnterpriseValueToSales AS REAL) AS EnterpriseValueToSales,
                                CAST(EnterpriseValueToFreeCashFlow AS REAL) AS EnterpriseValueToFreeCashFlow,
                                CAST(BookToMarketValue AS REAL) AS BookToMarketValue,
                                CAST(OperatingIncomeToEnterpriseValue AS REAL) AS OperatingIncomeToEnterpriseValue,
                                CAST(AltmanZScore AS REAL) AS AltmanZScore,
                                CAST(DividendYield AS REAL) AS DividendYield,
                                CAST(PriceToEarningsAdjusted AS REAL) AS PriceToEarningsAdjusted,
                                CAST(Y{_trainingParameters?.TargetDuration}Cagr AS REAL) AS Cagr
                        FROM EquityHistory
                        WHERE [Y{_trainingParameters?.TargetDuration}Cagr] IS NOT NULL AND [Y{_trainingParameters?.TargetDuration}Cagr] > {minCagr} AND [Y{_trainingParameters?.TargetDuration}Cagr] < {maxCagr}
                                AND MarketCap IS NOT NULL AND MarketCap > {minMarketCap} AND MarketCap < {maxMarketCap}
                                AND PriceToEarningsQuarterly IS NOT NULL AND PriceToEarningsQuarterly > {minPriceToEarnings} AND PriceToEarningsQuarterly < {maxPriceToEarnings}
                                AND PriceToEarningsTTM IS NOT NULL AND PriceToEarningsTTM > {minPriceToEarnings} AND PriceToEarningsTTM < {maxPriceToEarnings}
                                AND PriceToSalesQuarterly IS NOT NULL AND PriceToSalesQuarterly > {minPriceToSales} AND PriceToSalesQuarterly < {maxPriceToSales}
                                AND PriceToSalesTTM IS NOT NULL AND PriceToSalesTTM > {minPriceToSales} AND PriceToSalesTTM < {maxPriceToSales}
                                AND PriceToBookValue IS NOT NULL AND PriceToBookValue > {minPriceToBook} AND PriceToBookValue < {maxPriceToBook}
                                AND PriceToFreeCashFlowQuarterly IS NOT NULL AND PriceToFreeCashFlowQuarterly > {minPriceToCashFlow} AND PriceToFreeCashFlowQuarterly < {maxPriceToCashFlow}
                                AND PriceToFreeCashFlowTTM IS NOT NULL AND PriceToFreeCashFlowTTM > {minPriceToCashFlow} AND PriceToFreeCashFlowTTM < {maxPriceToCashFlow}
                                AND EnterpriseValue IS NOT NULL AND EnterpriseValue > {minMarketCap} AND EnterpriseValue < {maxMarketCap}
                                AND EnterpriseValueToEBITDA IS NOT NULL AND EnterpriseValueToEBITDA > {minPriceToEarnings}  AND EnterpriseValueToEBITDA < {maxPriceToEarnings}
                                AND EnterpriseValueToSales IS NOT NULL AND EnterpriseValueToSales > {minPriceToSales} AND EnterpriseValueToSales < {maxPriceToSales}
                                AND EnterpriseValueToFreeCashFlow IS NOT NULL AND EnterpriseValueToFreeCashFlow > {minPriceToCashFlow} AND EnterpriseValueToFreeCashFlow < {maxPriceToCashFlow}
                                AND BookToMarketValue IS NOT NULL AND BookToMarketValue > {minPriceToBook} AND BookToMarketValue < {maxPriceToBook}
                                AND OperatingIncomeToEnterpriseValue IS NOT NULL AND OperatingIncomeToEnterpriseValue > {minPriceToEarnings} AND OperatingIncomeToEnterpriseValue < {maxPriceToEarnings}
                                AND AltmanZScore IS NOT NULL AND AltmanZScore > {minAltmanZScore} AND AltmanZScore < {maxAltmanZScore}
                                AND DividendYield IS NOT NULL AND DividendYield >= {minDividendYield} AND DividendYield < {maxDividendYield}
                                AND PriceToEarningsAdjusted IS NOT NULL AND PriceToEarningsAdjusted > {minPriceToEarnings} AND PriceToEarningsAdjusted < {maxPriceToEarnings}
                                {indexFilter}";
        }

        private async Task saveModel()
        {
            string blobModelName = $"{_trainingParameters?.Index}-{_trainingParameters?.TargetDuration}-{_startTime.ToString(datetimeTagFormat)}.zip";
            _logger?.LogInformation($"Writing blob model: {blobModelName}");

            BlobServiceClient blobServiceClient = blobServiceClient = new BlobServiceClient(TrainModelsService.StorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(equityModelsContainer);
            containerClient.CreateIfNotExists();

            BlobClient workingSetWriterClient = containerClient.GetBlobClient(blobModelName);
            using (var modelBlobStream = workingSetWriterClient.OpenWrite(true))
            {
                _mlContext.Model.Save(trainedModel, _modelingDataset.Schema, modelBlobStream);
                modelBlobStream.Flush();
                modelBlobStream.Close();
            }

            _logger.LogInformation("Saved Model, updating metadata");
            using var connection = new SqlConnection(dbConnectionString);
            await connection.ExecuteAsync("INSERT INTO EquityModels ([Type], [Index], [TargetDuration], [Timestamp], [Path], [MeanAbsoluteError], [RootMeanSquaredError], [LossFunction], [RSquared])" +
                                    "Values (@Type, @Index, @TargetDuration, @Timestamp, @Path, @MeanAbsoluteError, @RootMeanSquaredError, @LossFunction, @RSquared)",
                                    new
                                    {
                                        Type = "Regression",
                                        _trainingParameters?.Index,
                                        TargetDuration = _trainingParameters?.TargetDuration * 12,
                                        Timestamp = _startTime,
                                        Path = blobModelName,
                                        _metrics.MeanAbsoluteError,
                                        _metrics.RootMeanSquaredError,
                                        _metrics.LossFunction,
                                        _metrics.RSquared
                                    });
        }

        private IDataView buildDataLoader()
        {
            //Create ML Context with seed for repeteable/deterministic results
            var loader = _mlContext.Data.CreateDatabaseLoader<EquityModeling>();
            var dbSource = new DatabaseSource(SqlClientFactory.Instance, dbConnectionString, geTrainModelQuery(), timeoutInSeconds);
            return loader.Load(dbSource);
        }

        private IEstimator<ITransformer> loadTrainingPipeline()
        {
            // STEP 2: Common data process configuration with pipeline data transformations
            var dataProcessPipeline = _mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(EquityModeling.Cagr))
                                        .Append(_mlContext.Transforms.Conversion.ConvertType([
                                                new InputOutputColumnPair(nameof(EquityModeling.MarketCap)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.PriceToEarningsQuarterly)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.PriceToEarningsTTM)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.PriceToSalesQuarterly)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.PriceToSalesTTM)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.PriceToBookValue)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.PriceToFreeCashFlowQuarterly)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.PriceToFreeCashFlowTTM)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.EnterpriseValue)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.EnterpriseValueToEBITDA)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.EnterpriseValueToSales)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.EnterpriseValueToFreeCashFlow)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.BookToMarketValue)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.OperatingIncomeToEnterpriseValue)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.AltmanZScore)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.DividendYield)),
                                                    new InputOutputColumnPair(nameof(EquityModeling.PriceToEarningsAdjusted))
                                        ], DataKind.Single))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.MarketCap)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToEarningsQuarterly)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToEarningsTTM)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToSalesQuarterly)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToSalesTTM)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToBookValue)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToFreeCashFlowQuarterly)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToFreeCashFlowTTM)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.EnterpriseValue)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.EnterpriseValueToEBITDA)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.EnterpriseValueToSales)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.EnterpriseValueToFreeCashFlow)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.BookToMarketValue)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.OperatingIncomeToEnterpriseValue)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.AltmanZScore)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.DividendYield)))
                                        .Append(_mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToEarningsAdjusted)))
                                        .Append(_mlContext.Transforms.Concatenate("Features", nameof(EquityModeling.MarketCap), nameof(EquityModeling.PriceToEarningsQuarterly), nameof(EquityModeling.PriceToEarningsTTM)
                                                                                , nameof(EquityModeling.PriceToSalesQuarterly), nameof(EquityModeling.PriceToSalesTTM), nameof(EquityModeling.PriceToBookValue)
                                                                                , nameof(EquityModeling.PriceToFreeCashFlowQuarterly), nameof(EquityModeling.PriceToFreeCashFlowTTM), nameof(EquityModeling.EnterpriseValue)
                                                                                , nameof(EquityModeling.EnterpriseValueToEBITDA), nameof(EquityModeling.EnterpriseValueToSales), nameof(EquityModeling.EnterpriseValueToFreeCashFlow)
                                                                                , nameof(EquityModeling.BookToMarketValue), nameof(EquityModeling.OperatingIncomeToEnterpriseValue), nameof(EquityModeling.AltmanZScore)
                                                                                , nameof(EquityModeling.DividendYield), nameof(EquityModeling.PriceToEarningsAdjusted)));

            // STEP 3: Set the training algorithm, then create and config the modelBuilder - Selected Trainer (SDCA Regression algorithm)                            
            // var trainer = _mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", featureColumnName: "Features", l1Regularization: l1RegularizationValue, l2Regularization: l2RegularizationValue);
            // var trainer = _mlContext.Regression.Trainers.LightGbm(labelColumnName: "Label", featureColumnName: "Features", numberOfLeaves: 20, minimumExampleCountPerLeaf: 10);
            var trainer = _mlContext.Regression.Trainers.FastTree(labelColumnName: "Label", featureColumnName: "Features");

            return dataProcessPipeline.Append(trainer);
        }
    }
}
