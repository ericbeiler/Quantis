using CsvHelper.Configuration;
using System.Globalization;


namespace Visavi.Quantis.Data
{
    public class SimFinDerivedSharepriceCsvToEquityRecordMap : ClassMap<DailyEquityRecord>
    {
        public SimFinDerivedSharepriceCsvToEquityRecordMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.Ticker).Name("Ticker");
            Map(m => m.SimFinId).Name("SimFinId");
            Map(m => m.Date).Name("Date");
            Map(m => m.Open).Name("Open");
            Map(m => m.High).Name("High");
            Map(m => m.Low).Name("Low");
            Map(m => m.Close).Name("Close");
            Map(m => m.AdjClose).Name("Adj. Close");
            Map(m => m.Volume).Name("Volume");
            Map(m => m.Dividend).Name("Dividend");
            Map(m => m.SharesOutstanding).Name("Shares Outstanding");
            Map(m => m.MarketCap).Name("Market-Cap");
            Map(m => m.PriceToEarningsQuarterly).Name("Price to Earnings Ratio (quarterly)");
            Map(m => m.PriceToEarningsTTM).Name("Price to Earnings Ratio (ttm)");
            Map(m => m.PriceToSalesQuarterly).Name("Price to Sales Ratio (quarterly)");
            Map(m => m.PriceToSalesTTM).Name("Price to Sales Ratio (ttm)");
            Map(m => m.PriceToBookValue).Name("Price to Book Value");
            Map(m => m.PriceToFreeCashFlowQuarterly).Name("Price to Free Cash Flow (quarterly)");
            Map(m => m.PriceToFreeCashFlowTTM).Name("Price to Free Cash Flow (ttm)");
            Map(m => m.EnterpriseValue).Name("Enterprise Value");
            Map(m => m.EnterpriseValueToEBITDA).Name("EV/EBITDA");
            Map(m => m.EnterpriseValueToSales).Name("EV/Sales");
            Map(m => m.EnterpriseValueToFreeCashFlow).Name("EV/FCF");
            Map(m => m.BookToMarketValue).Name("Book to Market Value");
            Map(m => m.OperatingIncomeToEnterpriseValue).Name("Operating Income/EV");
            Map(m => m.AltmanZScore).Name("Altman Z Score");
            Map(m => m.DividendYield).Name("Dividend Yield");
            Map(m => m.PriceToEarningsAdjusted).Name("Price to Earnings Ratio (Adjusted)");
        }
    }
}
