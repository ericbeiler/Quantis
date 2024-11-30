using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.Data.SqlClient;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Visavi.Quantis.Data;
using static Azure.Core.HttpHeader;

namespace Visavi.Quantis.Modeling
{
    internal class TrainModelMessage
    {
        public string Message = "Train Model";
        public int TargetDuration { get; set; }
        public string? Index { get; set; }
    }


    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private const string storageConnectionString = "BlobEndpoint=https://quantis.blob.core.windows.net/;QueueEndpoint=https://quantis.queue.core.windows.net/;FileEndpoint=https://quantis.file.core.windows.net/;TableEndpoint=https://quantis.table.core.windows.net/;SharedAccessSignature=sv=2022-11-02&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2025-11-17T02:13:18Z&st=2024-11-16T18:13:18Z&spr=https&sig=iKRDTg8msgsX8pPrgbJo%2Fm2gZam8JxDrF%2B11PU2KsZU%3D";
        private const string dbConnectionString = "Server=tcp:quantis.database.windows.net,1433;Initial Catalog=db-quantis;Persist Security Info=False;User ID=ebeiler;Password=1076Roan!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        private const int timeoutInSeconds = 3600;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        private string geTrainModelQuery(int targetDuration, string? index)
        {
            var indexFilter = "";
            if (!string.IsNullOrWhiteSpace(index))
            {
                indexFilter +=
                    @$" AND SimFinId IN
                    (
                        SELECT SimFinId
                        FROM IndexEquities
                        WHERE IndexTicker = '{index}'
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
                                CAST(Y{targetDuration}Cagr AS REAL) AS Cagr
                        FROM EquityHistory
                        WHERE [Y{targetDuration}Cagr] IS NOT NULL
                                AND MarketCap IS NOT NULL
                                AND PriceToEarningsQuarterly IS NOT NULL
                                AND PriceToEarningsTTM IS NOT NULL
                                AND PriceToSalesQuarterly IS NOT NULL
                                AND PriceToSalesTTM IS NOT NULL
                                AND PriceToBookValue IS NOT NULL
                                AND PriceToFreeCashFlowQuarterly IS NOT NULL
                                AND PriceToFreeCashFlowTTM IS NOT NULL
                                AND EnterpriseValue IS NOT NULL
                                AND EnterpriseValueToEBITDA IS NOT NULL
                                AND EnterpriseValueToSales IS NOT NULL
                                AND EnterpriseValueToFreeCashFlow IS NOT NULL
                                AND BookToMarketValue IS NOT NULL
                                AND OperatingIncomeToEnterpriseValue IS NOT NULL
                                AND AltmanZScore IS NOT NULL
                                AND DividendYield IS NOT NULL
                                AND PriceToEarningsAdjusted IS NOT NULL
                                {indexFilter}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DateTime startTime = DateTime.Now;
            _logger.LogInformation("Worker running at: {time}", startTime);

            // Instantiate a QueueClient to create and interact with the queue
            QueueClient queueClient = new QueueClient(storageConnectionString, "quantis-modeling");
            queueClient.CreateIfNotExists();

            try
            {
                var receivedResponse = await queueClient.ReceiveMessageAsync(TimeSpan.FromMinutes(60), stoppingToken);
                if (receivedResponse.Value == null)
                {
                    _logger?.LogError("No message in the queue.");
                    return;
                }

                // Deserialize the message body to get the ReloadDays
                string messageBody = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(receivedResponse.Value.Body?.ToString()));
                if (string.IsNullOrWhiteSpace(messageBody))
                {
                    _logger?.LogError("No message in the queue.");
                    return;
                }

                var queueMessage = JsonSerializer.Deserialize<TrainModelMessage>(messageBody);
                if (queueMessage == null)
                {
                    _logger?.LogError($"Failed to deserialize message body: {messageBody}");
                    return;
                }

                //Create ML Context with seed for repeteable/deterministic results
                MLContext mlContext = new MLContext(seed: 0);

                // STEP 1: Common data loading configuration
                var loader = mlContext.Data.CreateDatabaseLoader<EquityModeling>();
                var dbSource = new DatabaseSource(SqlClientFactory.Instance, dbConnectionString, geTrainModelQuery(queueMessage.TargetDuration, queueMessage.Index), timeoutInSeconds);
                IDataView fullTrainTestDataset = loader.Load(dbSource);

                var trainTestData = mlContext.Data.TrainTestSplit(fullTrainTestDataset, testFraction: 0.2);
                IDataView trainingSet = trainTestData.TrainSet;
                IDataView testingSet = trainTestData.TestSet;

                // STEP 2: Common data process configuration with pipeline data transformations
                var dataProcessPipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(EquityModeling.Cagr))
                                            .Append(mlContext.Transforms.Conversion.ConvertType(new[]
                                            {
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
                                            }, DataKind.Single))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.MarketCap)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToEarningsQuarterly)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToEarningsTTM)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToSalesQuarterly)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToSalesTTM)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToBookValue)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToFreeCashFlowQuarterly)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToFreeCashFlowTTM)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.EnterpriseValue)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.EnterpriseValueToEBITDA)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.EnterpriseValueToSales)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.EnterpriseValueToFreeCashFlow)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.BookToMarketValue)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.OperatingIncomeToEnterpriseValue)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.AltmanZScore)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.DividendYield)))
                                            .Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EquityModeling.PriceToEarningsAdjusted)))
                                            .Append(mlContext.Transforms.Concatenate("Features", nameof(EquityModeling.MarketCap), nameof(EquityModeling.PriceToEarningsQuarterly), nameof(EquityModeling.PriceToEarningsTTM)
                                                                                    , nameof(EquityModeling.PriceToSalesQuarterly), nameof(EquityModeling.PriceToSalesTTM), nameof(EquityModeling.PriceToBookValue)
                                                                                    , nameof(EquityModeling.PriceToFreeCashFlowQuarterly), nameof(EquityModeling.PriceToFreeCashFlowTTM), nameof(EquityModeling.EnterpriseValue)
                                                                                    , nameof(EquityModeling.EnterpriseValueToEBITDA), nameof(EquityModeling.EnterpriseValueToSales), nameof(EquityModeling.EnterpriseValueToFreeCashFlow)
                                                                                    , nameof(EquityModeling.BookToMarketValue), nameof(EquityModeling.OperatingIncomeToEnterpriseValue), nameof(EquityModeling.AltmanZScore)
                                                                                    , nameof(EquityModeling.DividendYield), nameof(EquityModeling.PriceToEarningsAdjusted)));

                // STEP 3: Set the training algorithm, then create and config the modelBuilder - Selected Trainer (SDCA Regression algorithm)                            
                var trainer = mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", featureColumnName: "Features");
                var trainingPipeline = dataProcessPipeline.Append(trainer);

                _logger.LogInformation($"Training Model, Index: {queueMessage.Index}, Target Duration: {queueMessage.TargetDuration}");

                var trainedModel = trainingPipeline.Fit(trainingSet);
                DateTime trainingCompleteTime = DateTime.Now;
                int trainingMinutes = Convert.ToInt32(Math.Ceiling((trainingCompleteTime - startTime).TotalMinutes));
                _logger.LogInformation($"Trained Model: {trainedModel}, {trainedModel.Count()} transformers in {trainingMinutes} minutes");

                _logger.LogInformation($"Testing Model:");
                IDataView predictions = trainedModel.Transform(testingSet);
                var metrics = mlContext.Regression.Evaluate(predictions, labelColumnName: "Label", scoreColumnName: "Score");

                int testingMinutes = Convert.ToInt32(Math.Ceiling((DateTime.Now - trainingCompleteTime).TotalMinutes));
                _logger.LogInformation($"Completed Testing in {testingMinutes} minute(s), {trainer.ToString()}:\n\tRoot Mean Squared Error: {metrics.RootMeanSquaredError}\n\tAbsolute Error: {metrics.MeanAbsoluteError}\n\tR Squared: {metrics.RSquared}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not build model.");
                throw;
            }
        }
    }
}
