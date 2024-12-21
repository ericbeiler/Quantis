using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    public class PredictionModelInput
    {
        public static readonly float MinimumAltmanZScore = Convert.ToSingle(EquityArchives.MinAltmanZScore);
        public static readonly float MinimumPriceToSales = Convert.ToSingle(EquityArchives.MinPriceToSales);
        public static readonly float MinimumPriceToEarnings = Convert.ToSingle(EquityArchives.MinPriceToEarnings);
        public static readonly float MinimumDividendYield = Convert.ToSingle(EquityArchives.MinDividendYield);
        public static readonly float MinimumPriceToCashFlow = Convert.ToSingle(EquityArchives.MinPriceToCashFlow);

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

    public static class PredictionModelInputExtensions
    {
        public static PredictionModelInput ToPredictionModelInput(this DailyEquityRecord dailyEquityRecord)
        {
            try
            {
                return new PredictionModelInput
                {
                    MarketCap = dailyEquityRecord.MarketCap.Value,
                    PriceToEarningsQuarterly = dailyEquityRecord.PriceToEarningsQuarterly ?? PredictionModelInput.MinimumPriceToEarnings,
                    PriceToEarningsTTM = dailyEquityRecord.PriceToEarningsTTM ?? PredictionModelInput.MinimumPriceToEarnings,
                    PriceToSalesQuarterly = dailyEquityRecord.PriceToSalesQuarterly ?? PredictionModelInput.MinimumPriceToSales,
                    PriceToSalesTTM = dailyEquityRecord.PriceToSalesTTM ?? PredictionModelInput.MinimumPriceToSales,
                    PriceToBookValue = dailyEquityRecord.PriceToBookValue.Value,
                    PriceToFreeCashFlowQuarterly = dailyEquityRecord.PriceToFreeCashFlowQuarterly ?? PredictionModelInput.MinimumPriceToCashFlow,
                    PriceToFreeCashFlowTTM = dailyEquityRecord.PriceToFreeCashFlowTTM ?? PredictionModelInput.MinimumPriceToCashFlow,
                    EnterpriseValue = dailyEquityRecord.EnterpriseValue.Value,
                    EnterpriseValueToEBITDA = dailyEquityRecord.EnterpriseValueToEBITDA.Value,
                    EnterpriseValueToSales = dailyEquityRecord.EnterpriseValueToSales ?? PredictionModelInput.MinimumPriceToSales,
                    EnterpriseValueToFreeCashFlow = dailyEquityRecord.EnterpriseValueToFreeCashFlow ?? PredictionModelInput.MinimumPriceToCashFlow,
                    BookToMarketValue = dailyEquityRecord.BookToMarketValue.Value,
                    OperatingIncomeToEnterpriseValue = dailyEquityRecord.OperatingIncomeToEnterpriseValue.Value,
                    AltmanZScore = dailyEquityRecord.AltmanZScore ?? PredictionModelInput.MinimumAltmanZScore,
                    DividendYield = dailyEquityRecord.DividendYield ?? PredictionModelInput.MinimumDividendYield,
                    PriceToEarningsAdjusted = dailyEquityRecord.PriceToEarningsAdjusted ?? PredictionModelInput.MinimumPriceToEarnings
                };
            }
            catch (Exception ex)
            {
                throw new Exception(@$"Unable to convert to model Input for {dailyEquityRecord.Ticker}:
                                    MarketCap = {dailyEquityRecord.MarketCap}
                                    PriceToEarningsQuarterly = {dailyEquityRecord.PriceToEarningsQuarterly}
                                    PriceToEarningsTTM = {dailyEquityRecord.PriceToEarningsTTM}
                                    PriceToSalesQuarterly = {dailyEquityRecord.PriceToSalesQuarterly}
                                    PriceToSalesTTM = {dailyEquityRecord.PriceToSalesTTM}
                                    PriceToBookValue = {dailyEquityRecord.PriceToBookValue}
                                    PriceToFreeCashFlowQuarterly = {dailyEquityRecord.PriceToFreeCashFlowQuarterly}
                                    PriceToFreeCashFlowTTM = {dailyEquityRecord.PriceToFreeCashFlowTTM}
                                    EnterpriseValue = {dailyEquityRecord.EnterpriseValue}
                                    EnterpriseValueToEBITDA = {dailyEquityRecord.EnterpriseValueToEBITDA}
                                    EnterpriseValueToSales = {dailyEquityRecord.EnterpriseValueToSales}
                                    EnterpriseValueToFreeCashFlow = {dailyEquityRecord.EnterpriseValueToFreeCashFlow}
                                    BookToMarketValue = {dailyEquityRecord.BookToMarketValue}
                                    OperatingIncomeToEnterpriseValue = {dailyEquityRecord.OperatingIncomeToEnterpriseValue}
                                    AltmanZScore = {dailyEquityRecord.AltmanZScore}
                                    DividendYield = {dailyEquityRecord.DividendYield}
                                    PriceToEarningsAdjusted = {dailyEquityRecord.PriceToEarningsAdjusted}", ex);
            }
        }
    }
}
