using CsvHelper;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Visavi.Quantis.Data
{
    internal class SimFinDerivedSharepriceFileLoader
    {
        private readonly IOrchestrator _dataServices;
        private readonly ILogger _logger;
        private const int batchSize = 5000;
        internal const string FileDelimiter = ";";

        public SimFinDerivedSharepriceFileLoader(IOrchestrator dataServices, ILogger logger)
        {
            _dataServices = dataServices;
            _logger = logger;
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

                csv.Context.RegisterClassMap<SimFinDerivedSharepriceCsvToEquityRecordMap>();
                var records = new List<DailyEquityRecord>();

                foreach (var record in csv.GetRecords<DailyEquityRecord>())
                {
                    records.Add(record);
                    if (records.Count() > batchSize)
                    {
                        await _dataServices.EquityArchives.BulkMergeAsync(records);
                        totalRecordCount += records.Count();
                        records = new List<DailyEquityRecord>();
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


    }
}
