using Microsoft.ML.Data;

namespace Visavi.Quantis.Data
{
    public interface IEquityArchives
    {
        Task BulkMergeAsync(List<DailyEquityRecord> records);
        Task<List<int>> GetEquityIds(string? equityIndex = null);
        Task<DailyEquityRecord> GetEquityRecordAsync(string ticker, DateTime? date = null);
        Task<DateTime> GetLastUpdateAsync();
        DatabaseSource GetTrainingDataQuerySource(string indexTicker, int targetDuration, int? datasetSizeLimit = null);
    }
}