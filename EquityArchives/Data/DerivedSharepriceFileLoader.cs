using CsvHelper;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Globalization;

namespace Visavi.Quantis.Data
{
    internal class DerivedSharepriceFileLoader
    {
        private const int batchSize = 5000;
        private const int MergeWarningThresholdMs = 8000;
        private Connections _connections;
        internal const string FileDelimiter = ";";
        private ILogger _logger => _connections.Logger;

        public DerivedSharepriceFileLoader(Connections connections)
        {
            _connections = connections;
        }

        internal async Task<int> LoadRecords(Stream derivedSharePricesDailyStream)
        {
            var totalRecordCount = 0;
            try
            {
                using var blobStreamReader = new StreamReader(derivedSharePricesDailyStream);
                using var csv = new CsvReader(blobStreamReader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = FileDelimiter
                });

                csv.Context.RegisterClassMap<DerivedSharepriceCsvToEquityRecordMap>();
                var records = new List<EquityModelingRecord>();

                foreach (var record in csv.GetRecords<EquityModelingRecord>())
                {
                    records.Add(record);
                    if (records.Count() > batchSize)
                    {
                        await BulkMergeEquityProperties(records);
                        totalRecordCount += records.Count();
                        records = new List<EquityModelingRecord>();
                    }
                }
                return totalRecordCount;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error loading equity records after completing {totalRecordCount}: {ex}");
                throw;
            }
        }


        private async Task BulkMergeEquityProperties(List<EquityModelingRecord> records)
        {
            var start = DateTime.Now;
            var firstRecord = records?.FirstOrDefault();
            _logger?.LogDebug($"Creating temp table of {records?.Count() ?? 0}, starting with {firstRecord}.");
            DataTable newRows = new DataTable("dbo.NewEquitiesDataType");
            newRows.Columns.Add("SimFinId", typeof(int));
            newRows.Columns.Add("Ticker", typeof(string));
            newRows.Columns.Add("Date", typeof(DateTime));
            newRows.Columns.Add("Open", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("High", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("Low", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("Close", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("AdjClose", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("Volume", typeof(long)).AllowDBNull = true;
            newRows.Columns.Add("Dividend", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("SharesOutstanding", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("MarketCap", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("PriceToEarningsQuarterly", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("PriceToEarningsTTM", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("PriceToSalesQuarterly", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("PriceToSalesTTM", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("PriceToBookValue", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("PriceToFreeCashFlowQuarterly", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("PriceToFreeCashFlowTTM", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("EnterpriseValue", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("EnterpriseValueToEBITDA", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("EnterpriseValueToSales", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("EnterpriseValueToFreeCashFlow", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("BookToMarketValue", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("OperatingIncomeToEnterpriseValue", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("AltmanZScore", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("DividendYield", typeof(float)).AllowDBNull = true;
            newRows.Columns.Add("PriceToEarningsAdjusted", typeof(float)).AllowDBNull = true;

            foreach (var record in records ?? new List<EquityModelingRecord>())
            {
                DataRow dataRow = newRows.NewRow();
                dataRow["SimFinId"] = record.SimFinId;
                dataRow["Ticker"] = record.Ticker.Length < 8 ? record.Ticker : record.Ticker.Substring(0, 8);
                dataRow["Date"] = record.Date;
                dataRow["Open"] = (object)record.Open ?? DBNull.Value;
                dataRow["High"] = (object)record.High ?? DBNull.Value;
                dataRow["Low"] = (object)record.Low ?? DBNull.Value;
                dataRow["Close"] = (object)record.Close ?? DBNull.Value;
                dataRow["AdjClose"] = (object)record.AdjClose ?? DBNull.Value;
                dataRow["Volume"] = (object)record.Volume ?? DBNull.Value;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                dataRow["Dividend"] = (object)record.Dividend ?? DBNull.Value;
                dataRow["SharesOutstanding"] = (object)record.SharesOutstanding ?? DBNull.Value;
                dataRow["MarketCap"] = (object)record.MarketCap ?? DBNull.Value;
                dataRow["PriceToEarningsQuarterly"] = (object)record.PriceToEarningsQuarterly ?? DBNull.Value;
                dataRow["PriceToEarningsTTM"] = (object)record.PriceToEarningsTTM ?? DBNull.Value;
                dataRow["PriceToSalesQuarterly"] = (object)record.PriceToSalesQuarterly ?? DBNull.Value;
                dataRow["PriceToSalesTTM"] = (object)record.PriceToSalesTTM ?? DBNull.Value;
                dataRow["PriceToBookValue"] = (object)record.PriceToBookValue ?? DBNull.Value;
                dataRow["PriceToFreeCashFlowQuarterly"] = (object)record.PriceToFreeCashFlowQuarterly ?? DBNull.Value;
                dataRow["PriceToFreeCashFlowTTM"] = (object)record.PriceToFreeCashFlowTTM ?? DBNull.Value;
                dataRow["EnterpriseValue"] = (object)record.EnterpriseValue ?? DBNull.Value;
                dataRow["EnterpriseValueToEBITDA"] = (object)record.EnterpriseValueToEBITDA ?? DBNull.Value;
                dataRow["EnterpriseValueToSales"] = (object)record.EnterpriseValueToSales ?? DBNull.Value;
                dataRow["EnterpriseValueToFreeCashFlow"] = (object)record.EnterpriseValueToFreeCashFlow ?? DBNull.Value;
                dataRow["BookToMarketValue"] = (object)record.BookToMarketValue ?? DBNull.Value;
                dataRow["OperatingIncomeToEnterpriseValue"] = (object)record.OperatingIncomeToEnterpriseValue ?? DBNull.Value;
                dataRow["AltmanZScore"] = (object)record.AltmanZScore ?? DBNull.Value;
                dataRow["DividendYield"] = (object)record.DividendYield ?? DBNull.Value;
                dataRow["PriceToEarningsAdjusted"] = (object)record.PriceToEarningsAdjusted ?? DBNull.Value;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                newRows.Rows.Add(dataRow);
            }
            _logger?.LogDebug($"Completed creation of temp table of {records?.Count() ?? 0}, starting with {firstRecord}.");

            using var connection = _connections.DbConnection;
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.Transaction = transaction;
            command.CommandText = @"
                MERGE INTO dbo.EquityHistory AS target
                USING @newEquities AS source
                ON target.SimFinId = source.SimFinId AND target.Date = source.Date
                WHEN MATCHED THEN
                    UPDATE SET
                        target.[Ticker] = source.[Ticker],
                        target.[Open] = source.[Open],
                        target.[High] = source.[High],
                        target.[Low] = source.[Low],
                        target.[Close] = source.[Close],
                        target.[AdjClose] = source.[AdjClose],
                        target.[Volume] = source.[Volume],
                        target.[Dividend] = source.[Dividend],
                        target.[SharesOutstanding] = source.[SharesOutstanding],
                        target.[MarketCap] = source.[MarketCap],
                        target.[PriceToEarningsQuarterly] = source.[PriceToEarningsQuarterly],
                        target.[PriceToEarningsTTM] = source.[PriceToEarningsTTM],
                        target.[PriceToSalesQuarterly] = source.[PriceToSalesQuarterly],
                        target.[PriceToSalesTTM] = source.[PriceToSalesTTM],
                        target.[PriceToBookValue] = source.[PriceToBookValue],
                        target.[PriceToFreeCashFlowQuarterly] = source.[PriceToFreeCashFlowQuarterly],
                        target.[PriceToFreeCashFlowTTM] = source.[PriceToFreeCashFlowTTM],
                        target.[EnterpriseValue] = source.[EnterpriseValue],
                        target.[EnterpriseValueToEBITDA] = source.[EnterpriseValueToEBITDA],
                        target.[EnterpriseValueToSales] = source.[EnterpriseValueToSales],
                        target.[EnterpriseValueToFreeCashFlow] = source.[EnterpriseValueToFreeCashFlow],
                        target.[BookToMarketValue] = source.[BookToMarketValue],
                        target.[OperatingIncomeToEnterpriseValue] = source.[OperatingIncomeToEnterpriseValue],
                        target.[AltmanZScore] = source.[AltmanZScore],
                        target.[DividendYield] = source.[DividendYield],
                        target.[PriceToEarningsAdjusted] = source.[PriceToEarningsAdjusted]                
                    WHEN NOT MATCHED BY TARGET THEN
                        INSERT ([SimFinId], [Ticker], [Date], [Open], [High], [Low], [Close], [AdjClose], [Volume], [Dividend], [SharesOutstanding], [MarketCap], [PriceToEarningsQuarterly], [PriceToEarningsTTM], [PriceToSalesQuarterly], [PriceToSalesTTM], [PriceToBookValue], [PriceToFreeCashFlowQuarterly], [PriceToFreeCashFlowTTM], [EnterpriseValue], [EnterpriseValueToEBITDA], [EnterpriseValueToSales], [EnterpriseValueToFreeCashFlow], [BookToMarketValue], [OperatingIncomeToEnterpriseValue], [AltmanZScore], [DividendYield], [PriceToEarningsAdjusted])
                        VALUES (source.[SimFinId], source.[Ticker], source.[Date], source.[Open], source.[High], source.[Low], source.[Close], source.[AdjClose], source.[Volume], source.[Dividend], source.[SharesOutstanding], source.[MarketCap], source.[PriceToEarningsQuarterly], source.[PriceToEarningsTTM], source.[PriceToSalesQuarterly], source.[PriceToSalesTTM], source.[PriceToBookValue], source.[PriceToFreeCashFlowQuarterly], source.[PriceToFreeCashFlowTTM], source.[EnterpriseValue], source.[EnterpriseValueToEBITDA], source.[EnterpriseValueToSales], source.[EnterpriseValueToFreeCashFlow], source.[BookToMarketValue], source.[OperatingIncomeToEnterpriseValue], source.[AltmanZScore], source.[DividendYield], source.[PriceToEarningsAdjusted]);";

            var parameter = command.Parameters.AddWithValue("@newEquities", newRows);
            parameter.SqlDbType = SqlDbType.Structured;
            parameter.TypeName = "dbo.NewEquitiesDataType";

            _logger?.LogDebug($"Initiating bulk merge of {records?.Count() ?? 0}, transaction {transaction.GetHashCode()}, starting with {firstRecord}.");
            try
            {
                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error during bulk merge:, transaction {transaction.GetHashCode()}");
                throw;
            }

            var completionMs = (int)Math.Ceiling((DateTime.Now - start).TotalMilliseconds);
            if (completionMs > MergeWarningThresholdMs)
            {
                _logger?.LogWarning($"Excessive delay in completed transaction {transaction.GetHashCode()} ({firstRecord}). Duration: {completionMs}");
            }
            else
            {
                _logger?.LogDebug($"Completed transaction {transaction.GetHashCode()} ({firstRecord?.Ticker}) in {completionMs} ms.");
            }
            _logger?.LogMetric("BulkMergeDurationMs", completionMs);
        }
    }
}
