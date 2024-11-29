using System;

namespace Visavi.Quantis.Data
{
    public class EquityModeling
    {
        public float MarketCap { get; set; }
        public float PriceToEarningsQuarterly { get; set; }
        public float PriceToEarningsTTM { get; set; }
        public float PriceToSalesQuarterly { get; set; }
        public float PriceToSalesTTM { get; set; }
        public float PriceToBookValue { get; set; }
        public float PriceToFreeCashFlowQuarterly { get; set; }
        public float PriceToFreeCashFlowTTM { get; set; }
        public float EnterpriseValue { get; set; }
        public float EnterpriseValueToEBITDA { get; set; }
        public float EnterpriseValueToSales { get; set; }
        public float EnterpriseValueToFreeCashFlow { get; set; }
        public float BookToMarketValue { get; set; }
        public float OperatingIncomeToEnterpriseValue { get; set; }
        public float AltmanZScore { get; set; }
        public float DividendYield { get; set; }
        public float PriceToEarningsAdjusted { get; set; }
        public float Cagr { get; set; }
    }
}
