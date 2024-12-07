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
            return Convert.ToDecimal(startingPrice * Math.Pow(1 + cagr / 100, durationInMonths / monthsPerYear));
        }

        internal async Task<Prediction> PredictAsync(int modelId, string ticker, DateTime? predictionDay = null)
        {
            return (await PredictAsync(modelId, [ticker], predictionDay))[0];
        }

        internal async Task<Prediction[]> PredictAsync(int modelId, string[] tickers, DateTime? predictionDay = null)
        {
            try
            {
                MLContext mlContext = new MLContext();
                var predictionModel = await _dataServices.PredictionModels.GetPredictionModelAsync(modelId);
                ITransformer inferencingModel = mlContext.Model.Load(predictionModel.InferencingModel.ToStream(), out DataViewSchema predictionSchema);
                var predictionEngine = mlContext.Model.CreatePredictionEngine<PredictionModelInput, PredictionModelOutput>(inferencingModel);

                var predictions = new List<Prediction>();
                foreach (string ticker in tickers)
                {
                    var equityRecord = await _dataServices.EquityArchives.GetEquityRecordAsync(ticker, predictionDay);
                    var inputData = equityRecord.ToPredictionModelInput();
                    var cagr = predictionEngine.Predict(inputData).PredictedCagr;
                    var prediction = new Prediction()
                    {
                        Ticker = ticker,
                        StartingDate = equityRecord.Date,
                        StartingPrice = Convert.ToDecimal(equityRecord.Close),
                        PredictedCagr = cagr,
                        EndingDate = equityRecord.Date.AddMonths(predictionModel.TargetDuration),
                        PredictedEndingPrice = calculateEndingPrice(equityRecord.Close, cagr, predictionModel.TargetDuration),
                        ExpectedCagrRangeLow = Convert.ToSingle(cagr - predictionModel.RootMeanSquaredError),
                        ExpectedPriceRangeLow = calculateEndingPrice(equityRecord.Close, cagr - predictionModel.RootMeanSquaredError, predictionModel.TargetDuration),
                        ExpectedCagrRangeHigh = Convert.ToSingle(cagr + predictionModel.RootMeanSquaredError),
                        ExpectedPriceRangeHigh = calculateEndingPrice(equityRecord.Close, cagr + predictionModel.RootMeanSquaredError, predictionModel.TargetDuration),
                    };

                    _logger.LogInformation($"Predicted CAGR for {ticker} is {prediction.PredictedCagr}");
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
