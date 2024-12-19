
using Visavi.Quantis.Modeling;

namespace Visavi.Quantis.Data
{
    public interface IPredictionModels
    {
        Task<int> CreateConglomerateModel(TrainModelMessage trainingParameters);
        Task<IEnumerable<PredictionModelSummary>> GetModelSummaryListAsync();
        Task<PredictionModel> GetPredictionModelAsync(int id);
        Task SaveModel(string modelName, RegressionModel trainedModel);
    }
}