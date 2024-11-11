using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visavi.Quantis
{
    public class EquityProperties
    {
        public required string Ticker { get; set; }
        public int SimFinId { get; set; }
        public DateTime Date { get; set; }
        public float Open { get; set; }
        public float High { get; set; }
        public float Low { get; set; }
        public float Close { get; set; }
        public float AdjClose { get; set; }
        public long Volume { get; set; }
        public float? Dividend { get; set; }
        public float? SharesOutstanding { get; set; }
        public float? MarketCap { get; set; }
        public float? PriceToEarningsQuarterly { get; set; }
        public float? PriceToEarningsTTM { get; set; }
        public float? PriceToSalesQuarterly { get; set; }
        public float? PriceToSalesTTM { get; set; }
        public float? PriceToBookValue { get; set; }
        public float? PriceToFreeCashFlowQuarterly { get; set; }
        public float? PriceToFreeCashFlowTTM { get; set; }
        public float? EnterpriseValue { get; set; }
        public float? EnterpriseValueToEBITDA { get; set; }
        public float? EnterpriseValueToSales { get; set; }
        public float? EnterpriseValueToFreeCashFlow { get; set; }
        public float? BookToMarketValue { get; set; }
        public float? OperatingIncomeToEnterpriseValue { get; set; }
        public float? AltmanZScore { get; set; }
        public float? DividendYield { get; set; }
        public float? PriceToEarningsAdjusted { get; set; }

        public override string ToString()
        {
            return $"Ticker: {Ticker}, Date: {Date}, Open: {Open}, AdjClose: {AdjClose}, PriceToEarningsTTM: {PriceToEarningsTTM}, PriceToSalesTTM: {PriceToSalesTTM}, ...";
        }
    }
}
