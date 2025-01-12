
using Visavi.Quantis.Modeling;

namespace Visavi.Quantis.Data
{
    public interface IPredictionModels
    {
        Task<int> CreateCompositeModel(TrainModelMessage trainingParameters);
        Task<IEnumerable<ModelSummary>> GetModelSummaryList(ModelType modelType = ModelType.Composite);
        Task<CompositeModelDetails> GetCompositeModelDetails(int compositeModelId);
        Task<PriceTrendPredictor> GetPriceTrendPredictor(int compositeModelId);
        Task<IPredictor> GetPricePredictor(int id);
        Task SaveModel(string modelName, RegressionModel trainedModel);
        Task UpdateModelState(int modelId, ModelState modelState);
        Task UpdateQualityScore(int modelId, double qualityScore);
    }
}