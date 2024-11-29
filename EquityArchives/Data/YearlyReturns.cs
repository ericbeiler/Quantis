using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Visavi.Quantis.Data
{
    internal class YearlyReturns
    {
        private readonly ILogger? _logger;
        private readonly string? _dbConnectionString;
        private const int updateWarningThresholdInSeconds = 300;

        public YearlyReturns(ILogger? logger = null)
        {
            _logger = logger;

            _dbConnectionString = Environment.GetEnvironmentVariable("QuantisDbConnection");
            if (string.IsNullOrEmpty(_dbConnectionString))
            {
                _logger?.LogError("QuantisDbConnection environment variable is missing.");
            }
        }

        internal async Task<int> UpdateYearlyReturns(DateOnly startDate, DateOnly endDate)
        {
            var start = DateTime.Now;
            try
            {
                using var connection = new SqlConnection(_dbConnectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
                using var command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.Transaction = transaction;
                command.CommandTimeout = 600;
                command.CommandText = @"
                        DECLARE @Timestamp DATE
                        SET @Timestamp = (SELECT MAX ([Date]) FROM dbo.EquityHistory);

                        WITH YearReturns AS (
                            SELECT
                                y0.ID,
                                y0.SimFinId AS SimFinId,
                                y0.Date AS Y0Date,
                                y0.AdjClose AS Y0AdjClose,

                                COALESCE(y1a.Date, y1b.Date, y1c.Date) AS Y1Date,
                                COALESCE(y1a.AdjClose, y1b.AdjClose, y1c.AdjClose) AS Y1AdjClose,

                                COALESCE(y2a.Date, y2b.Date, y2c.Date) AS Y2Date,
                                COALESCE(y2a.AdjClose, y2b.AdjClose, y2c.AdjClose) AS Y2AdjClose,

                                COALESCE(y3a.Date, y3b.Date, y3c.Date) AS Y3Date,
                                COALESCE(y3a.AdjClose, y3b.AdjClose, y3c.AdjClose) AS Y3AdjClose,

                                COALESCE(y5a.Date, y5b.Date, y5c.Date) AS Y5Date,
                                COALESCE(y5a.AdjClose, y5b.AdjClose, y5c.AdjClose) AS Y5AdjClose

                            FROM
                                dbo.EquityHistory y0  WITH (NOLOCK)
                            LEFT JOIN
                                dbo.EquityHistory y1a
                                ON y0.SimFinId = y1a.SimFinId
                                AND y0.Date BETWEEN '2000-01-01' AND '3000-01-01'
                                AND y1a.Date = DATEADD(DAY, 364, y0.Date)
                            LEFT JOIN
                                dbo.EquityHistory y1b
                                ON y0.SimFinId = y1b.SimFinId
                                AND y0.Date BETWEEN '2000-01-01' AND '3000-01-01'
                                AND y1b.Date = DATEADD(DAY, 363, y0.Date)
                            LEFT JOIN
                                dbo.EquityHistory y1c
                                ON y0.SimFinId = y1c.SimFinId
                                AND y0.Date BETWEEN '2000-01-01' AND '3000-01-01'
                                AND y1c.Date = DATEADD(DAY, 361, y0.Date)

                            LEFT JOIN
                                dbo.EquityHistory y2a
                                ON y0.SimFinId = y2a.SimFinId
                                AND y0.Date BETWEEN '2000-01-01' AND '3000-01-01'
                                AND y2a.Date = DATEADD(DAY, 728, y0.Date)
                            LEFT JOIN
                                dbo.EquityHistory y2b
                                ON y0.SimFinId = y2b.SimFinId
                                AND y0.Date BETWEEN '2000-01-01' AND '3000-01-01'
                                AND y2b.Date = DATEADD(DAY, 727, y0.Date)
                            LEFT JOIN
                                dbo.EquityHistory y2c
                                ON y0.SimFinId = y2c.SimFinId
                                AND y0.Date BETWEEN '2000-01-01' AND '3000-01-01'
                                AND y2c.Date = DATEADD(DAY, 725, y0.Date)

                            LEFT JOIN
                                dbo.EquityHistory y3a
                                ON y0.SimFinId = y3a.SimFinId
                                AND y0.Date BETWEEN '2000-01-01' AND '3000-01-01'
                                AND y3a.Date = DATEADD(DAY, 1092, y0.Date)
                            LEFT JOIN
                                dbo.EquityHistory y3b
                                ON y0.SimFinId = y3b.SimFinId
                                AND y0.Date BETWEEN '2000-01-01' AND '3000-01-01'
                                AND y3b.Date = DATEADD(DAY, 1091, y0.Date)
                            LEFT JOIN
                                dbo.EquityHistory y3c
                                ON y0.SimFinId = y3c.SimFinId
                                AND y0.Date BETWEEN '2000-01-01' AND '3000-01-01'
                                AND y3c.Date = DATEADD(DAY, 1089, y0.Date)

                            LEFT JOIN
                                dbo.EquityHistory y5a
                                ON y0.SimFinId = y5a.SimFinId
                                AND y0.Date BETWEEN '2000-01-01' AND '3000-01-01'
                                AND y5a.Date = DATEADD(DAY, 1827, y0.Date)
                            LEFT JOIN
                                dbo.EquityHistory y5b
                                ON y0.SimFinId = y5b.SimFinId
                                AND y0.Date BETWEEN '2000-01-01' AND '3000-01-01'
                                AND y5b.Date = DATEADD(DAY, 1826, y0.Date)
                            LEFT JOIN
                                dbo.EquityHistory y5c
                                ON y0.SimFinId = y5c.SimFinId
                                AND y0.Date BETWEEN '2000-01-01' AND '3000-01-01'
                                AND y5c.Date = DATEADD(DAY, 1824, y0.Date)
                            WHERE
                                y0.[Date] BETWEEN @startDate AND @endDate
                        )
                        UPDATE dbo.EquityHistory
                        SET 
                            Y1Date = YearReturns.Y1Date,
                            Y1AdjClose = CONVERT(FLOAT, YearReturns.Y1AdjClose),
                            Y1TotalReturn = CASE
                                WHEN YearReturns.Y1AdjClose IS NOT NULL AND YearReturns.Y0AdjClose IS NOT NULL AND CONVERT(FLOAT, YearReturns.Y0AdjClose) > 0 THEN
                                    (CONVERT(FLOAT, YearReturns.Y1AdjClose - YearReturns.Y0AdjClose) / YearReturns.Y0AdjClose) * 100
                                ELSE
                                    NULL
                            END,
                            Y1Cagr = CASE
                                WHEN YearReturns.Y1AdjClose IS NOT NULL AND YearReturns.Y0AdjClose IS NOT NULL AND CONVERT(FLOAT, YearReturns.Y0AdjClose) > 0 THEN
                                    ((CONVERT(FLOAT, YearReturns.Y1AdjClose) / CONVERT(FLOAT, YearReturns.Y0AdjClose)) - 1) * 100
                                ELSE
                                    NULL
                            END,
                            Y2Date = YearReturns.Y2Date,
                            Y2AdjClose = CONVERT(FLOAT, YearReturns.Y2AdjClose), 
                            Y2TotalReturn = CASE
                                WHEN YearReturns.Y2AdjClose IS NOT NULL AND YearReturns.Y0AdjClose IS NOT NULL AND CONVERT(FLOAT, YearReturns.Y0AdjClose) > 0 THEN
                                    (CONVERT(FLOAT, YearReturns.Y2AdjClose - YearReturns.Y0AdjClose) / YearReturns.Y0AdjClose) * 100
                                ELSE
                                    NULL
                            END,
                            Y2Cagr = CASE
                                WHEN YearReturns.Y2AdjClose IS NOT NULL AND YearReturns.Y0AdjClose IS NOT NULL AND CONVERT(FLOAT, YearReturns.Y0AdjClose) > 0 THEN
                                    (POWER(CONVERT(FLOAT, YearReturns.Y2AdjClose) / CONVERT(FLOAT, YearReturns.Y0AdjClose), 1.0 / 2.0) - 1) * 100
                                ELSE 
                                    NULL
                            END,
                            Y3Date = YearReturns.Y3Date,
                            Y3AdjClose = CONVERT(FLOAT, YearReturns.Y3AdjClose),
                            Y3TotalReturn = CASE
                                WHEN YearReturns.Y3AdjClose IS NOT NULL AND YearReturns.Y0AdjClose IS NOT NULL AND CONVERT(FLOAT, YearReturns.Y0AdjClose) > 0 THEN
                                    (CONVERT(FLOAT, YearReturns.Y3AdjClose - YearReturns.Y0AdjClose) / YearReturns.Y0AdjClose) * 100
                                ELSE
                                    NULL
                            END,
                            Y3Cagr = CASE
                                WHEN YearReturns.Y2AdjClose IS NOT NULL AND YearReturns.Y0AdjClose IS NOT NULL AND CONVERT(FLOAT, YearReturns.Y0AdjClose) > 0 THEN
                                    (POWER(CONVERT(FLOAT, YearReturns.Y3AdjClose) / CONVERT(FLOAT, YearReturns.Y0AdjClose), 1.0 / 3.0) - 1) * 100
                                ELSE 
                                    NULL
                            END,
                            Y5Date = YearReturns.Y5Date,
                            Y5AdjClose = CONVERT(FLOAT, YearReturns.Y5AdjClose),
                            Y5TotalReturn = CASE
                                WHEN YearReturns.Y5AdjClose IS NOT NULL AND YearReturns.Y0AdjClose IS NOT NULL AND CONVERT(FLOAT, YearReturns.Y0AdjClose) > 0 THEN
                                    (CONVERT(FLOAT, YearReturns.Y5AdjClose - YearReturns.Y0AdjClose) / YearReturns.Y0AdjClose) * 100
                                ELSE
                                    NULL
                            END,
                            Y5Cagr =  CASE
                                WHEN YearReturns.Y5AdjClose IS NOT NULL AND YearReturns.Y0AdjClose IS NOT NULL AND CONVERT(FLOAT, YearReturns.Y0AdjClose) > 0 THEN
                                    (POWER(CONVERT(FLOAT, YearReturns.Y5AdjClose) / CONVERT(FLOAT, YearReturns.Y0AdjClose), 1.0 / 5.0) - 1) * 100
                                ELSE 
                                    NULL
                            END,
                            YearlyReturnsTimestamp = @Timestamp

                        FROM
                            YearReturns WITH (NOLOCK)
                        WHERE
                            dbo.EquityHistory.ID = YearReturns.ID";

                var startParameter = command.Parameters.AddWithValue("@startDate", startDate);
                startParameter.SqlDbType = SqlDbType.Date;
                var endParameter = command.Parameters.AddWithValue("@endDate", endDate);
                endParameter.SqlDbType = SqlDbType.Date;

                _logger?.LogDebug($"Initiating update of yearly returns for {startDate} through {endDate}, transaction {transaction.GetHashCode()}.");
                int rowsUpdated = await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();

                var seconds = (int)Math.Ceiling((DateTime.Now - start).TotalSeconds);
                if (seconds > updateWarningThresholdInSeconds)
                {
                    _logger?.LogWarning($"Excessive delay in completing update yearly returns transaction {transaction.GetHashCode()} ({startDate} - {endDate}). Duration: {seconds}");
                }
                else
                {
                    _logger?.LogInformation($"Completed transaction {transaction.GetHashCode()} (yearly returns {startDate} - {endDate}) in {seconds} seconds.");
                }
                _logger?.LogMetric("UpdateYearlyReturnsSeconds", seconds);
                return rowsUpdated;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error updating yearly returns: {ex}");
                throw;
            }
        }
    }
}
