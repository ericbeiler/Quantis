using Azure.Storage.Blobs;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Formats.Asn1;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Visavi.Quantis
{
    public class EquityArchives
    {
        private readonly ILogger<EquityArchives> _logger;
        private static readonly string BlobConnectionString = Environment.GetEnvironmentVariable("VisaviStorageAccount");
        private static readonly string ContainerName = "stocks-data";
        private static readonly string AlphaVantageApiKey = Environment.GetEnvironmentVariable("AlphaVantageApiKey");

        public EquityArchives(ILogger<EquityArchives> logger)
        {
            _logger = logger;
        }

        [Function("EquityArchives")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("Processing request to export stock data to CSV");

            if (string.IsNullOrEmpty(AlphaVantageApiKey))
            {
                _logger.LogError("Alpha Vantage API key is missing. Set AlphaVantageApiKey Environment Variable to a valid key.");
                return new BadRequestObjectResult("Alpha Vantage API key is missing. Set AlphaVantageApiKey Environment Variable to a valid key.");
            }

            // Define stocks of interest
            var tickers = new[] { "AAPL", "MSFT", "GOOGL" }; // Add desired tickers

            // Fetch stock data for each ticker
            var stocks = new List<StockData>();
            foreach (var ticker in tickers)
            {
                var stockData = await FetchStockDataAsync(ticker);
                if (stockData != null)
                {
                    stocks.Add(stockData);
                }
            }

           stocks.ForEach(stock => _logger.LogInformation(stock.ToString()));

            // Write data to CSV
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(stocks);
                writer.Flush();

                // Upload to Blob Storage
                var blobServiceClient = new BlobServiceClient(BlobConnectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
                await blobContainerClient.CreateIfNotExistsAsync();

                var blobClient = blobContainerClient.GetBlobClient($"stocks_data_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
                stream.Position = 0;
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            return new OkObjectResult(stocks);
        }

        private async Task<StockData> FetchStockDataAsync(string ticker)
        {
            // Alpha Vantage API endpoints
            string overviewUrl = $"https://www.alphavantage.co/query?function=OVERVIEW&symbol={ticker}&apikey={AlphaVantageApiKey}";
            string quoteUrl = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={ticker}&apikey={AlphaVantageApiKey}";

            using var httpClient = new HttpClient();

            // Fetch company overview
            var overviewResponse = await httpClient.GetAsync(overviewUrl);
            if (!overviewResponse.IsSuccessStatusCode)
            {
                _logger?.LogError($"Failed to fetch company overview for {ticker}, {overviewResponse.ReasonPhrase}");
                return null;
            }
            var overviewJson = await overviewResponse.Content.ReadAsStringAsync();
            var overviewData = JsonSerializer.Deserialize<CompanyOverview>(overviewJson);

            // Fetch global quote
            var quoteResponse = await httpClient.GetAsync(quoteUrl);
            if (!quoteResponse.IsSuccessStatusCode)
            {
                _logger?.LogError($"Failed to fetch company quote for {ticker}, {quoteResponse.ReasonPhrase}");
                return null;
            }
            var quoteJson = await quoteResponse.Content.ReadAsStringAsync();
            var quoteData = JsonSerializer.Deserialize<GlobalQuoteResponse>(quoteJson);

            if (overviewData == null)
            {
                return null;
            }

            // Map data to StockData
            return new StockData
            {
                Ticker = ticker,
                CompanyName = overviewData.Name,
                Price = decimal.TryParse(quoteData?.GlobalQuote?.Price, out var price) ? price : (decimal?)null,
                MarketCap = decimal.TryParse(overviewData.MarketCapitalization, out var marketCap) ? marketCap : (decimal?)null,
                Industry = overviewData.Industry,
                PE = decimal.TryParse(overviewData.PERatio, out var pe) ? pe : (decimal?)null,
                PEG = decimal.TryParse(overviewData.PEGRatio, out var peg) ? peg : (decimal?)null,
                QuickRatio = decimal.TryParse(overviewData.QuickRatio, out var quickRatio) ? quickRatio : (decimal?)null,
                DebtToEquity = decimal.TryParse(overviewData.DebtToEquityRatio, out var debtToEquity) ? debtToEquity : (decimal?)null,
                FairValue = null // Alpha Vantage does not provide fair value directly
            };
        }

        public record StockData
        {
            public string Ticker { get; set; }
            public string CompanyName { get; set; }
            public decimal? Price { get; set; }
            public decimal? MarketCap { get; set; }
            public string Industry { get; set; }
            public decimal? PE { get; set; }
            public decimal? PEG { get; set; }
            public decimal? QuickRatio { get; set; }
            public decimal? DebtToEquity { get; set; }
            public decimal? FairValue { get; set; }
        }

        public class CompanyOverview
        {
            public string Symbol { get; set; }
            public string Name { get; set; }
            public string MarketCapitalization { get; set; }
            public string Industry { get; set; }
            public string PERatio { get; set; }
            public string PEGRatio { get; set; }
            public string QuickRatio { get; set; }
            public string DebtToEquityRatio { get; set; }
        }

        public class GlobalQuoteResponse
        {
            [JsonPropertyName("Global Quote")]
            public GlobalQuote GlobalQuote { get; set; }
        }

        public class GlobalQuote
        {
            [JsonPropertyName("05. price")]
            public string Price { get; set; }
        }
    }
}
