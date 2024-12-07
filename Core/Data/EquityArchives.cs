using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Data;
using System.Data;

namespace Visavi.Quantis.Data
{
    internal class EquityArchives : IEquityArchives
    {
        private const int timeoutInSeconds = 3600;
        private const int MergeWarningThresholdMs = 8000;

        private const decimal minMarketCap = 10000000;
        private const decimal maxMarketCap = 10000000000000;

        private const decimal minPriceToEarnings = -10000;
        private const decimal maxPriceToEarnings = 10000;

        private const decimal minPriceToBook = -1000;
        private const decimal maxPriceToBook = 1000;

        private const decimal minDividendYield = 0;
        private const decimal maxDividendYield = 100;

        private const decimal minAltmanZScore = 0;
        private const decimal maxAltmanZScore = 100;

        private const decimal minCagr = -100;
        private const decimal maxCagr = 5000;

        private const decimal minPriceToSales = -10000;
        private const decimal maxPriceToSales = 10000;

        private const decimal minPriceToCashFlow = -10000;
        private const decimal maxPriceToCashFlow = 10000;

        private readonly Connections _connections;
        private readonly ILogger _logger;

        public EquityArchives(Connections connections, ILogger logger)
        {
            _connections = connections;
            _logger = logger;
        }

        public async Task<List<int>> GetEquityIds(string? equityIndex = null)
        {
            List<int> simFinIds = new List<int>();
            using var connection = _connections.DbConnection;
            {
                if (string.IsNullOrWhiteSpace(equityIndex))
                {
                    simFinIds = (await connection.QueryAsync<int>("SELECT DISTINCT SimFinId FROM EquityHistory")).ToList();
                }
                else
                {
                    simFinIds = (await connection.QueryAsync<int>("SELECT DISTINCT SimFinId FROM IndexEquities WHERE IndexTicker = @equityIndex", new { equityIndex })).ToList();
                }
            }
            return simFinIds;
        }

        public async Task<DailyEquityRecord> GetEquityRecordAsync(string ticker, DateTime? date = null)
        {
            using var connection = _connections.DbConnection;
            return await connection.QueryFirstOrDefaultAsync<DailyEquityRecord>("SELECT TOP 1 * FROM EquityHistory WHERE Ticker = @ticker ORDER BY [Date] DESC", new { ticker });
        }

        public async Task<DateTime> GetLastUpdateAsync()
        {
            using var connection = _connections.DbConnection;
            return (await connection.QueryAsync<DateTime>("Select Top 1 [Date] from EquityHistory order by [Date] desc")).FirstOrDefault();
        }

        public DatabaseSource GetTrainingDataQuerySource(string indexTicker, int targetDuration, int? datasetSizeLimit)
        {
            return new DatabaseSource(SqlClientFactory.Instance, _connections.DbConnectionString, getTrainModelQuery(indexTicker, targetDuration, datasetSizeLimit), timeoutInSeconds);
        }

        private string getTrainModelQuery(string indexTicker, int targetDurationInMonths, int? datasetSizeLimit = null)
        {
            int targetDuraionInYears = targetDurationInMonths / 12;

            var indexFilter = "";
            var sizeLimiter = datasetSizeLimit != null ? $"TOP {datasetSizeLimit}" : "";
            if (!string.IsNullOrWhiteSpace(indexTicker))
            {
                indexFilter +=
                    @$" AND SimFinId IN
                    (
                        SELECT SimFinId
                        FROM IndexEquities
                        WHERE IndexTicker = '{indexTicker}'
                    )";
            }

            return $@"
                        SELECT  {sizeLimiter} Ticker, 
                                [Date], 
                                CAST(MarketCap AS REAL) AS MarketCap,
                                CAST(PriceToEarningsQuarterly AS REAL) AS PriceToEarningsQuarterly,
                                CAST(PriceToEarningsTTM AS REAL) AS PriceToEarningsTTM,
                                CAST(PriceToSalesQuarterly AS REAL) AS PriceToSalesQuarterly,
                                CAST(PriceToSalesTTM AS REAL) AS PriceToSalesTTM,
                                CAST(PriceToBookValue AS REAL) AS PriceToBookValue,
                                CAST(PriceToFreeCashFlowQuarterly AS REAL) AS PriceToFreeCashFlowQuarterly,
                                CAST(PriceToFreeCashFlowTTM AS REAL) AS PriceToFreeCashFlowTTM,
                                CAST(EnterpriseValue AS REAL) AS EnterpriseValue,
                                CAST(EnterpriseValueToEBITDA AS REAL) AS EnterpriseValueToEBITDA,
                                CAST(EnterpriseValueToSales AS REAL) AS EnterpriseValueToSales,
                                CAST(EnterpriseValueToFreeCashFlow AS REAL) AS EnterpriseValueToFreeCashFlow,
                                CAST(BookToMarketValue AS REAL) AS BookToMarketValue,
                                CAST(OperatingIncomeToEnterpriseValue AS REAL) AS OperatingIncomeToEnterpriseValue,
                                CAST(AltmanZScore AS REAL) AS AltmanZScore,
                                CAST(DividendYield AS REAL) AS DividendYield,
                                CAST(PriceToEarningsAdjusted AS REAL) AS PriceToEarningsAdjusted,
                                CAST(Y{targetDuraionInYears}Cagr AS REAL) AS Cagr
                        FROM EquityHistory
                        WHERE [Y{targetDuraionInYears}Cagr] IS NOT NULL AND [Y{targetDuraionInYears}Cagr] > {minCagr} AND [Y{targetDuraionInYears}Cagr] < {maxCagr}
                                AND MarketCap IS NOT NULL AND MarketCap > {minMarketCap} AND MarketCap < {maxMarketCap}
                                AND PriceToEarningsQuarterly IS NOT NULL AND PriceToEarningsQuarterly > {minPriceToEarnings} AND PriceToEarningsQuarterly < {maxPriceToEarnings}
                                AND PriceToEarningsTTM IS NOT NULL AND PriceToEarningsTTM > {minPriceToEarnings} AND PriceToEarningsTTM < {maxPriceToEarnings}
                                AND PriceToSalesQuarterly IS NOT NULL AND PriceToSalesQuarterly > {minPriceToSales} AND PriceToSalesQuarterly < {maxPriceToSales}
                                AND PriceToSalesTTM IS NOT NULL AND PriceToSalesTTM > {minPriceToSales} AND PriceToSalesTTM < {maxPriceToSales}
                                AND PriceToBookValue IS NOT NULL AND PriceToBookValue > {minPriceToBook} AND PriceToBookValue < {maxPriceToBook}
                                AND PriceToFreeCashFlowQuarterly IS NOT NULL AND PriceToFreeCashFlowQuarterly > {minPriceToCashFlow} AND PriceToFreeCashFlowQuarterly < {maxPriceToCashFlow}
                                AND PriceToFreeCashFlowTTM IS NOT NULL AND PriceToFreeCashFlowTTM > {minPriceToCashFlow} AND PriceToFreeCashFlowTTM < {maxPriceToCashFlow}
                                AND EnterpriseValue IS NOT NULL AND EnterpriseValue > {minMarketCap} AND EnterpriseValue < {maxMarketCap}
                                AND EnterpriseValueToEBITDA IS NOT NULL AND EnterpriseValueToEBITDA > {minPriceToEarnings}  AND EnterpriseValueToEBITDA < {maxPriceToEarnings}
                                AND EnterpriseValueToSales IS NOT NULL AND EnterpriseValueToSales > {minPriceToSales} AND EnterpriseValueToSales < {maxPriceToSales}
                                AND EnterpriseValueToFreeCashFlow IS NOT NULL AND EnterpriseValueToFreeCashFlow > {minPriceToCashFlow} AND EnterpriseValueToFreeCashFlow < {maxPriceToCashFlow}
                                AND BookToMarketValue IS NOT NULL AND BookToMarketValue > {minPriceToBook} AND BookToMarketValue < {maxPriceToBook}
                                AND OperatingIncomeToEnterpriseValue IS NOT NULL AND OperatingIncomeToEnterpriseValue > {minPriceToEarnings} AND OperatingIncomeToEnterpriseValue < {maxPriceToEarnings}
                                AND AltmanZScore IS NOT NULL AND AltmanZScore > {minAltmanZScore} AND AltmanZScore < {maxAltmanZScore}
                                AND DividendYield IS NOT NULL AND DividendYield >= {minDividendYield} AND DividendYield < {maxDividendYield}
                                AND PriceToEarningsAdjusted IS NOT NULL AND PriceToEarningsAdjusted > {minPriceToEarnings} AND PriceToEarningsAdjusted < {maxPriceToEarnings}
                                {indexFilter}";
        }

        public async Task BulkMergeAsync(List<DailyEquityRecord> records)
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

            foreach (var record in records ?? new List<DailyEquityRecord>())
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
        }

    }
}
