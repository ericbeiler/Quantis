using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    public class ModelService
    {
        private const int monthsPerYear = 12;
        private readonly ILogger<ModelService> _logger;
        private readonly IDataServices _dataServices;

        public ModelService(ILogger<ModelService> logger, IDataServices dataServices)
        {
            _logger = logger;
            _dataServices = dataServices;
        }

        private decimal calculateEndingPrice(double startingPrice, double cagr, int durationInMonths)
        {
            try
            {
                return Convert.ToDecimal(startingPrice * Math.Pow(1 + cagr / 100, durationInMonths / monthsPerYear));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Unable to calculate the ending price for starting price {startingPrice}, cagr {cagr}, durationInMonths {durationInMonths}");
                return decimal.MinusOne;
            }
        }

        internal async Task<Prediction> PredictAsync(int modelId, string ticker, DateTime? predictionDay = null)
        {
            return (await PredictAsync(modelId, [ticker], predictionDay))[0];
        }

        internal async Task<Prediction[]> PredictAsync(int modelId, string[] tickers, DateTime? predictionDay = null)
        {
            var _tickers = tickers;
            if (tickers.Count() == 1 && await _dataServices.EquityArchives.IsIndexTicker(tickers[0]))
            {
                _tickers = (await _dataServices.EquityArchives.GetEquityTickers(tickers[0])).ToArray();
            }
            try
            {
                MLContext mlContext = new MLContext();
                var predictionModel = await _dataServices.PredictionModels.GetPredictionModelAsync(modelId);
                ITransformer inferencingModel = mlContext.Model.Load(predictionModel.InferencingModel.ToStream(), out DataViewSchema predictionSchema);
                var predictionEngine = mlContext.Model.CreatePredictionEngine<PredictionModelInput, PredictionModelOutput>(inferencingModel);

                var predictions = new List<Prediction>();
                foreach (string ticker in _tickers)
                {
                    DailyEquityRecord? equityRecord = new() { Ticker = ticker };
                    float cagr = float.NaN;
                    try
                    {
                        equityRecord = await _dataServices.EquityArchives.GetEquityRecordAsync(ticker, predictionDay);
                        var inputData = equityRecord.ToPredictionModelInput();
                        cagr = predictionEngine.Predict(inputData).PredictedCagr;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Could not predict CAGR for {ticker}. Check Model Inputs.");
                    }

                    predictions.Add(new Prediction()
                    {
                        Ticker = ticker,
                        StartingDate = equityRecord?.Date ?? DateTime.MinValue,
                        StartingPrice = Convert.ToDecimal(equityRecord?.Close),
                        PredictedCagr = cagr,
                        EndingDate = equityRecord != null ? equityRecord.Date.AddMonths(predictionModel.TargetDuration) : DateTime.MinValue,
                        PredictedEndingPrice = calculateEndingPrice(equityRecord.Close, cagr, predictionModel.TargetDuration),
                        ExpectedCagrRangeLow = Convert.ToSingle(cagr - predictionModel.RootMeanSquaredError),
                        ExpectedPriceRangeLow = calculateEndingPrice(equityRecord.Close, cagr - predictionModel.RootMeanSquaredError, predictionModel.TargetDuration),
                        ExpectedCagrRangeHigh = Convert.ToSingle(cagr + predictionModel.RootMeanSquaredError),
                        ExpectedPriceRangeHigh = calculateEndingPrice(equityRecord.Close, cagr + predictionModel.RootMeanSquaredError, predictionModel.TargetDuration),
                    });

                    _logger.LogInformation($"Predicted CAGR for {ticker} is {cagr}");
                }
                return predictions.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Could not predict through inference engine: {ex.Message}");
                throw;
            }
        }
    }
}
