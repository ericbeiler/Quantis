
namespace Visavi.Quantis.Data
{
    public interface IPredictionModels
    {
        Task<IEnumerable<PredictionModelSummary>> GetModelSummaryListAsync();
        Task<PredictionModel> GetPredictionModelAsync(int id);
        Task SaveModel(string modelName, ITrainedPredictionModel trainedModel);
    }
}