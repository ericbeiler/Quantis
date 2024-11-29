using System;

namespace Visavi.Quantis.Data
{
    public class EquityModelingRecord
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
        public DateTime Y1Date { get; set; }
        public float? Y1AdjClose { get; set; }
        public float? Y1Cagr { get; set; }
        public float? Y1TotalReturn { get; set; }
        public DateTime Y2Date { get; set; }
        public float? Y2AdjClose { get; set; }
        public float? Y2Cagr { get; set; }
        public float? Y2TotalReturn { get; set; }
        public DateTime Y3Date { get; set; }
        public float? Y3AdjClose { get; set; }
        public float? Y3Cagr { get; set; }
        public float? Y3TotalReturn { get; set; }
        public DateTime Y5Date { get; set; }
        public float? Y5AdjClose { get; set; }
        public float? Y5Cagr { get; set; }
        public float? Y5TotalReturn { get; set; }
        public DateTime YearlyReturnsTimestamp { get; set; }

        public override string ToString()
        {
            return $"Equity Data: {Ticker}, {Date}, Open: {Open}, AdjClose: {AdjClose}, ...";
        }
    }
}
