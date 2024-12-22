
using Visavi.Quantis.Modeling;

namespace Visavi.Quantis.Data
{
    public interface IPredictionModels
    {
        Task<int> CreateCompositeModel(TrainModelMessage trainingParameters);
        Task<CompositeModel> GetCompositeModel(int compositeModelId);
        Task<IEnumerable<ModelSummary>> GetModelSummaryList(ModelType modelType = ModelType.Composite);
        Task<IPredictor> GetPricePredictor(int id);
        Task SaveModel(string modelName, RegressionModel trainedModel);
    }
}