using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Queues.Models;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.Json;
using Visavi.Quantis.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Visavi.Quantis.Modeling
{
    public class ModelingService
    {
        internal const string ContainerFolderSeperator = "/";
        private readonly ILogger<ModelingService> _logger;
        private readonly Connections _connections;

        // Queue Requests
        internal const string ModelingQueueName = "quantis-modeling";
        private const int timeoutInSeconds = 3600;
        private static readonly List<string> _workingMessageList = new List<string>();  // TODO: Scalable solution needed




        public ModelingService(ILogger<ModelingService> logger, Connections connections)
        {
            _logger = logger;
            _connections = connections;
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
                                MarketCap,
                                PriceToEarningsQuarterly,
                                PriceToEarningsTTM,
                                PriceToSalesQuarterly,
                                PriceToSalesTTM,
                                PriceToBookValue,
                                PriceToFreeCashFlowQuarterly,
                                PriceToFreeCashFlowTTM,
                                EnterpriseValue,
                                EnterpriseValueToEBITDA,
                                EnterpriseValueToSales,
                                EnterpriseValueToFreeCashFlow,
                                BookToMarketValue,
                                OperatingIncomeToEnterpriseValue,
                                AltmanZScore,
                                DividendYield,
                                PriceToEarningsAdjusted,
                                Y{targetDuration}Cagr AS Cagr
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

        [Function("ProcessModelingQueue")]
        public async Task ProcessModelingQueue([QueueTrigger(ModelingQueueName, Connection = "QuantisStorageConnection")] QueueMessage message)
        {
            var startTime = DateTime.Now;
            var messageBody = message.Body.ToString();
            _logger?.LogInformation($"Processing Queue Message: {message.MessageId}, {messageBody}");
            bool alreadyLoading = true;
            lock (_workingMessageList)
            {
                if (!_workingMessageList.Contains(messageBody))
                {
                    _workingMessageList.Add(messageBody);
                    alreadyLoading = false;
                }
            }

            if (alreadyLoading)
            {
                _logger?.LogWarning($"Skipping redundant processing of message {messageBody}.");
                return;
            }

            try
            {
                // Deserialize the message body to get the ReloadDays
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
                var dbSource = new DatabaseSource(SqlClientFactory.Instance, _connections.DbConnectionString, geTrainModelQuery(queueMessage.TargetDuration, queueMessage.Index), timeoutInSeconds);
                IDataView baseTrainingDataView = loader.Load(dbSource);

                //Sample code of removing extreme data like "outliers" for FareAmounts higher than $150 and lower than $1 which can be error-data 
                // var cnt = baseTrainingDataView.GetColumn<float>(nameof(TaxiTrip.FareAmount)).Count();
                // IDataView trainingDataView = mlContext.Data.FilterRowsByColumn(baseTrainingDataView, nameof(TaxiTrip.FareAmount), lowerBound: 1, upperBound: 150);
                // var cnt2 = trainingDataView.GetColumn<float>(nameof(TaxiTrip.FareAmount)).Count();

                // STEP 2: Common data process configuration with pipeline data transformations
                var dataProcessPipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Cagr", inputColumnName: nameof(EquityModeling.Cagr))
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
                var trainer = mlContext.Regression.Trainers.Sdca(labelColumnName: "Cagr", featureColumnName: "Features");
                var trainingPipeline = dataProcessPipeline.Append(trainer);

                _logger.LogInformation($"Training Model, Index: {queueMessage.Index}, Target Duration: {queueMessage.Index}");
                var preview = mlContext.Data.CreateEnumerable<EquityModeling>(baseTrainingDataView, false).Take(5);
                foreach (var record in preview)
                {
                    _logger.LogInformation($"  Price/Earnings:{record.PriceToEarningsTTM}, Cagr:{record.Cagr}");
                }
                var trainedModel = trainingPipeline.Fit(baseTrainingDataView);
            }
            finally
            {
                lock (_workingMessageList)
                {
                    _workingMessageList.Remove(messageBody);
                }

            }
        }
    }
}
