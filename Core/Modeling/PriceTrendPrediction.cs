using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visavi.Quantis.Modeling
{
    public class PriceTrendPrediction
    {

        public PriceTrendPrediction(string ticker, PricePointPrediction[] pricePoints)
        {
            Ticker = ticker;
            PricePoints = pricePoints;
        }

        public string Ticker { get; }
        public PricePointPrediction[] PricePoints { get; }
    }
}
