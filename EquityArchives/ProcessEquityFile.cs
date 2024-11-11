using System.Data;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Visavi.Quantis
{
    public class ProcessEquityFile
    {
        private readonly ILogger<ProcessEquityFile> _logger;
        private readonly string _connectionString;

        public ProcessEquityFile(ILogger<ProcessEquityFile> logger)
        {
            _logger = logger;

#pragma warning disable CS8601 // Possible null reference assignment.
            _connectionString = Environment.GetEnvironmentVariable("QuantisDbConnection");
#pragma warning restore CS8601 // Possible null reference assignment.
            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogError("QuantisDbConnection environment variable is missing.");
            }
        }

        [Function(nameof(ProcessEquityFile))]
        public async Task Run([BlobTrigger("equity-archives/{name}", Connection = "QuantisStorage:blob")] Stream stream, string triggerName)
        {
            using var blobStreamReader = new StreamReader(stream);
            using var csv = new CsvReader(blobStreamReader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";"
            });

            csv.Context.RegisterClassMap<EquityPropertiesMap>();

            var records = new List<EquityProperties>();

            await foreach (var record in csv.GetRecordsAsync<EquityProperties>())
            {
                records.Add(record);
                if (records.Count() > 1000)
                {
                    await BulkInsertEquityProperties(records);
                    records = new List<EquityProperties>();
                }
            }
            await BulkInsertEquityProperties(records);

            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {triggerName}");
        }


        private async Task BulkInsertEquityProperties(List<EquityProperties> records)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = @"
                INSERT INTO dbo.EquityHistory ([Ticker], [SimFinId], [Date], [Open], [High], [Low], [Close], [AdjClose], [Volume], [Dividend], [SharesOutstanding], [MarketCap], [PriceToEarningsQuarterly], [PriceToEarningsTTM], [PriceToSalesQuarterly], [PriceToSalesTTM], [PriceToBookValue], [PriceToFreeCashFlowQuarterly], [PriceToFreeCashFlowTTM], [EnterpriseValue], [EnterpriseValueToEBITDA], [EnterpriseValueToSales], [EnterpriseValueToFreeCashFlow], [BookToMarketValue], [OperatingIncomeToEnterpriseValue], [AltmanZScore], [DividendYield], [PriceToEarningsAdjusted])
                VALUES (@Ticker, @SimFinId, @Date, @Open, @High, @Low, @Close, @AdjClose, @Volume, @Dividend, @SharesOutstanding, @MarketCap, @PriceToEarningsQuarterly, @PriceToEarningsTTM, @PriceToSalesQuarterly, @PriceToSalesTTM, @PriceToBookValue, @PriceToFreeCashFlowQuarterly, @PriceToFreeCashFlowTTM, @EnterpriseValue, @EnterpriseValueToEBITDA, @EnterpriseValueToSales, @EnterpriseValueToFreeCashFlow, @BookToMarketValue, @OperatingIncomeToEnterpriseValue, @AltmanZScore, @DividendYield, @PriceToEarningsAdjusted)";

            _logger.LogInformation($"Initiating bulk insert of {records?.Count() ?? 0}, transaction {transaction.GetHashCode()}, starting with {records?.FirstOrDefault()}.");
            foreach (var record in records)
            {
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@Ticker", record.Ticker);
                command.Parameters.AddWithValue("@SimFinId", record.SimFinId);
                command.Parameters.AddWithValue("@Date", record.Date);
                command.Parameters.AddWithValue("@Open", record.Open);
                command.Parameters.AddWithValue("@High", record.High);
                command.Parameters.AddWithValue("@Low", record.Low);
                command.Parameters.AddWithValue("@Close", record.Close);
                command.Parameters.AddWithValue("@AdjClose", record.AdjClose);
                command.Parameters.AddWithValue("@Volume", record.Volume);
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                command.Parameters.AddWithValue("@Dividend", (object)record.Dividend ?? DBNull.Value);
                command.Parameters.AddWithValue("@SharesOutstanding", (object)record.SharesOutstanding ?? DBNull.Value);
                command.Parameters.AddWithValue("@MarketCap", (object)record.MarketCap ?? DBNull.Value);
                command.Parameters.AddWithValue("@PriceToEarningsQuarterly", (object)record.PriceToEarningsQuarterly ?? DBNull.Value);
                command.Parameters.AddWithValue("@PriceToEarningsTTM", (object)record.PriceToEarningsTTM ?? DBNull.Value);
                command.Parameters.AddWithValue("@PriceToSalesQuarterly", (object)record.PriceToSalesQuarterly ?? DBNull.Value);
                command.Parameters.AddWithValue("@PriceToSalesTTM", (object)record.PriceToSalesTTM ?? DBNull.Value);
                command.Parameters.AddWithValue("@PriceToBookValue", (object)record.PriceToBookValue ?? DBNull.Value);
                command.Parameters.AddWithValue("@PriceToFreeCashFlowQuarterly", (object)record.PriceToFreeCashFlowQuarterly ?? DBNull.Value);
                command.Parameters.AddWithValue("@PriceToFreeCashFlowTTM", (object)record.PriceToFreeCashFlowTTM ?? DBNull.Value);
                command.Parameters.AddWithValue("@EnterpriseValue", (object)record.EnterpriseValue ?? DBNull.Value);
                command.Parameters.AddWithValue("@EnterpriseValueToEBITDA", (object)record.EnterpriseValueToEBITDA ?? DBNull.Value);
                command.Parameters.AddWithValue("@EnterpriseValueToSales", (object)record.EnterpriseValueToSales ?? DBNull.Value);
                command.Parameters.AddWithValue("@EnterpriseValueToFreeCashFlow", (object)record.EnterpriseValueToFreeCashFlow ?? DBNull.Value);
                command.Parameters.AddWithValue("@BookToMarketValue", (object)record.BookToMarketValue ?? DBNull.Value);
                command.Parameters.AddWithValue("@OperatingIncomeToEnterpriseValue", (object)record.OperatingIncomeToEnterpriseValue ?? DBNull.Value);
                command.Parameters.AddWithValue("@AltmanZScore", (object)record.AltmanZScore ?? DBNull.Value);
                command.Parameters.AddWithValue("@DividendYield", (object)record.DividendYield ?? DBNull.Value);
                command.Parameters.AddWithValue("@PriceToEarningsAdjusted", (object)record.PriceToEarningsAdjusted ?? DBNull.Value);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            _logger.LogInformation($"Committed transaction {transaction.GetHashCode()}.");
        }
    }
}
