using Visavi.Quantis.Data;

namespace Visavi.Quantis.Modeling
{
    public class PredictionModelInput
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

    public static class PredictionModelInputExtensions
    {
        public static PredictionModelInput ToPredictionModelInput(this DailyEquityRecord dailyEquityRecord)
        {
            try
            {
                return new PredictionModelInput
                {
                    MarketCap = dailyEquityRecord.MarketCap.Value,
                    PriceToEarningsQuarterly = dailyEquityRecord.PriceToEarningsQuarterly.Value,
                    PriceToEarningsTTM = dailyEquityRecord.PriceToEarningsTTM.Value,
                    PriceToSalesQuarterly = dailyEquityRecord.PriceToSalesQuarterly.Value,
                    PriceToSalesTTM = dailyEquityRecord.PriceToSalesTTM.Value,
                    PriceToBookValue = dailyEquityRecord.PriceToBookValue.Value,
                    PriceToFreeCashFlowQuarterly = dailyEquityRecord.PriceToFreeCashFlowQuarterly.Value,
                    PriceToFreeCashFlowTTM = dailyEquityRecord.PriceToFreeCashFlowTTM.Value,
                    EnterpriseValue = dailyEquityRecord.EnterpriseValue.Value,
                    EnterpriseValueToEBITDA = dailyEquityRecord.EnterpriseValueToEBITDA.Value,
                    EnterpriseValueToSales = dailyEquityRecord.EnterpriseValueToSales.Value,
                    EnterpriseValueToFreeCashFlow = dailyEquityRecord.EnterpriseValueToFreeCashFlow.Value,
                    BookToMarketValue = dailyEquityRecord.BookToMarketValue.Value,
                    OperatingIncomeToEnterpriseValue = dailyEquityRecord.OperatingIncomeToEnterpriseValue.Value,
                    AltmanZScore = dailyEquityRecord.AltmanZScore.Value,
                    DividendYield = dailyEquityRecord.DividendYield.Value,
                    PriceToEarningsAdjusted = dailyEquityRecord.PriceToEarningsAdjusted.Value
                };
            }
            catch (Exception ex)
            {
                throw new Exception(@$"Unable to convert to model Input:
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
