
using Visavi.Quantis.Modeling;

namespace Visavi.Quantis.Data
{
    public interface IPredictionModels
    {
        Task<int> CreateCompositeModel(TrainModelMessage trainingParameters);
        Task<CompositeModel> GetCompositeModel(int compositeModelId);
        Task<IEnumerable<PredictionModelSummary>> GetModelSummaryList();
        Task<IPredictor> GetPricePredictor(int id);
        Task SaveModel(string modelName, RegressionModel trainedModel);
    }
}