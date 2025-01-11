using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    public class PredictionService : IPredictionService
    {
        private const int monthsPerYear = 12;
        private readonly ILogger<PredictionService> _logger;
        private readonly IDataServices _dataServices;

        public PredictionService(ILogger<PredictionService> logger, IDataServices dataServices)
        {
            _logger = logger;
            _dataServices = dataServices;
        }

        private decimal? calculateEndingPrice(double? startingPrice, double? cagr, int? durationInMonths)
        {
            try
            {
                if (startingPrice == null || cagr == null || durationInMonths == null)
                {
                    _logger.LogWarning($"Unable to calculate the ending price due to a null value in starting price {startingPrice}, cagr {cagr} or durationInMonths {durationInMonths}");
                    return null;
                }
                return Convert.ToDecimal(startingPrice * Math.Pow(1 + cagr.Value / 100, durationInMonths.Value / monthsPerYear));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unable to calculate the ending price for starting price {startingPrice}, cagr {cagr}, durationInMonths {durationInMonths}");
                return null;
            }
        }

        internal async Task<PricePointPrediction> PredictPricePointAsync(int regressionModelId, string ticker, DateTime? predictionDay = null)
        {
            return (await PredictPricePointAsync(regressionModelId, [ticker], predictionDay))[0];
        }

        internal async Task<PricePointPrediction[]> PredictPricePointAsync(int regressionModelId, string[] tickers, DateTime? predictionDay = null)
        {
            var startingEquityRecords = await getEquityPredictionData(tickers, predictionDay);

            try
            {
                MLContext mlContext = new MLContext();
                var predictionModel = await _dataServices.PredictionModels.GetPricePredictor(regressionModelId);
                ITransformer inferencingModel = mlContext.Model.Load(predictionModel.InferencingModel.ToStream(), out DataViewSchema predictionSchema);
                var predictionEngine = mlContext.Model.CreatePredictionEngine<PredictionModelInput, PredictionModelOutput>(inferencingModel);

                var predictions = new List<PricePointPrediction>();
                foreach (var equityRecord in startingEquityRecords)
                {
                    float? cagr = null;
                    try
                    {
                        var inputData = equityRecord.ToPredictionModelInput();
                        cagr = predictionEngine.Predict(inputData).PredictedCagr;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Could not predict CAGR for {equityRecord.Ticker}. Check Model Inputs.");
                    }

                    predictions.Add(new PricePointPrediction()
                    {
                        Ticker = equityRecord.Ticker,
                        ProjectionPeriodInMonths = predictionModel.TargetDuration,
                        StartingDate = equityRecord?.Date.ToDateOnly() ?? DateOnly.MinValue,
                        StartingPrice = Convert.ToDecimal(equityRecord?.Close),
                        PredictedCagr = cagr,
                        EndingDate = equityRecord != null ? equityRecord.Date.AddMonths(predictionModel.TargetDuration).ToDateOnly() : DateOnly.MinValue,
                        PredictedEndingPrice = calculateEndingPrice(equityRecord?.Close, cagr, predictionModel.TargetDuration),
                        ExpectedCagrRangeLow = cagr != null ? Convert.ToSingle(cagr - predictionModel.RootMeanSquaredError) : null,
                        ExpectedPriceRangeLow = calculateEndingPrice(equityRecord?.Close, cagr - predictionModel.RootMeanSquaredError, predictionModel.TargetDuration),
                        ExpectedCagrRangeHigh = cagr != null ? Convert.ToSingle(cagr + predictionModel.RootMeanSquaredError) : null,
                        ExpectedPriceRangeHigh = calculateEndingPrice(equityRecord?.Close, cagr + predictionModel.RootMeanSquaredError, predictionModel.TargetDuration),
                    });

                    _logger.LogInformation($"Predicted CAGR for {equityRecord?.Ticker} is {cagr}");
                }
                return predictions.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Could not predict through inference engine: {ex.Message}");
                throw;
            }
        }

        public string GetCacheKey(int compositeId, string ticker)
        {
            return $"PredictionsKey:{compositeId}-{ticker}";
        }

        public async Task<PriceTrendPrediction[]> PredictPriceTrend(int compositeModelId, string[] tickers, DateTime? predictionDay = null)
        {
            try
            {
                if (tickers.Count() == 1 && await _dataServices.EquityArchives.IsIndexTicker(tickers[0]))
                {
                    var predictions = await _dataServices.Cache.Get<PriceTrendPrediction[]>(GetCacheKey(compositeModelId, tickers[0]));
                    if (predictions != null)
                    {
                        return predictions;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not get cached predictions for {tickers[0]}");
            }

            var startingEquityRecords = await getEquityPredictionData(tickers, predictionDay);
            var compositeModel = await _dataServices.PredictionModels.GetCompositeModel(compositeModelId);
            var predictors = compositeModel.Predictors.Select(predictor => (predictor, getPredictionEngine(predictor)));

            try
            {
                var trendPredictions = new List<PriceTrendPrediction>();
                foreach (var equityRecord in startingEquityRecords.Where(record => record != null))
                {
                    var ticker = equityRecord?.Ticker ?? "";
                    var pointPredictions = new List<PricePointPrediction>();
                    foreach (var predictor in predictors)
                    {
                        float? cagr = null;
                        try
                        {
                            var inputData = equityRecord?.ToPredictionModelInput();
                            cagr = inputData != null ? predictor.Item2.Predict(inputData).PredictedCagr : null;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Could not predict CAGR for {ticker}. Check Model Inputs.");
                        }

                        pointPredictions.Add(new PricePointPrediction()
                        {
                            Ticker = ticker,
                            ProjectionPeriodInMonths = predictor.Item1.TargetDuration,
                            StartingDate = equityRecord?.Date.ToDateOnly() ?? DateOnly.MinValue,
                            StartingPrice = Convert.ToDecimal(equityRecord?.Close),
                            PredictedCagr = cagr,
                            EndingDate = equityRecord != null ? equityRecord.Date.AddMonths(predictor.Item1.TargetDuration).ToDateOnly() : DateOnly.MinValue,
                            PredictedEndingPrice = calculateEndingPrice(equityRecord?.Close, cagr, predictor.Item1.TargetDuration),
                            ExpectedCagrRangeLow = cagr != null ? Convert.ToSingle(cagr - predictor.Item1.RootMeanSquaredError) : null,
                            ExpectedPriceRangeLow = calculateEndingPrice(equityRecord?.Close, cagr - predictor.Item1.RootMeanSquaredError, predictor.Item1.TargetDuration),
                            ExpectedCagrRangeHigh = cagr != null ? Convert.ToSingle(cagr + predictor.Item1.RootMeanSquaredError) : null,
                            ExpectedPriceRangeHigh = calculateEndingPrice(equityRecord?.Close, cagr + predictor.Item1.RootMeanSquaredError, predictor.Item1.TargetDuration),
                        });

                        _logger.LogInformation($"Predicted CAGR for {ticker} is {cagr}");
                    }
                    trendPredictions.Add(new PriceTrendPrediction(ticker, pointPredictions.ToArray()));
                }
                return trendPredictions.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Could not predict through inference engine: {ex.Message}");
                throw;
            }
        }

        private async Task<PredictionEngine<PredictionModelInput, PredictionModelOutput>> getPredictionEngine(int regressionModelId)
        {
            return getPredictionEngine(await _dataServices.PredictionModels.GetPricePredictor(regressionModelId));
        }

        private PredictionEngine<PredictionModelInput, PredictionModelOutput> getPredictionEngine(IPredictor pricePointPredictor)
        {
            MLContext mlContext = new MLContext();
            ITransformer inferencingModel = mlContext.Model.Load(pricePointPredictor.InferencingModel.ToStream(), out DataViewSchema predictionSchema);
            return mlContext.Model.CreatePredictionEngine<PredictionModelInput, PredictionModelOutput>(inferencingModel);
        }

        private async Task<DailyEquityRecord[]> getEquityPredictionData(string[] tickers, DateTime? predictionDay = null)
        {
            var _tickers = tickers;
            if (tickers.Count() == 1 && await _dataServices.EquityArchives.IsIndexTicker(tickers[0]))
            {
                _tickers = (await _dataServices.EquityArchives.GetEquityTickers(tickers[0])).ToArray();
            }

            var startingEquityRecords = new List<DailyEquityRecord>();
            foreach (string ticker in _tickers)
            {
                try
                {
                    var equityRecord = await _dataServices.EquityArchives.GetEquityRecordAsync(ticker, predictionDay);
                    startingEquityRecords.Add(equityRecord);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Could not get starting equity record for {ticker}");
                }
            }
            return startingEquityRecords.ToArray();
        }

    }
}
