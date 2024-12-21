using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visavi.Quantis.Modeling
{
    public interface IPredictionService
    {
        string GetCacheKey(int compositeId, string ticker);
        Task<PriceTrendPrediction[]> PredictPriceTrend(int compositeModelId, string[] tickers, DateTime? predictionDay = null);
    }
}
