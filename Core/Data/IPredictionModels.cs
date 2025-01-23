
using Visavi.Quantis.Modeling;

namespace Visavi.Quantis.Data
{
    public interface IPredictionModels
    {
        Task<int> CreateCompositeModel(TrainModelMessage trainingParameters);
        Task<IEnumerable<ModelSummary>> GetModelSummaryList(ModelType modelType = ModelType.Composite, bool includeDeleted = false);
        Task<CompositeModelDetails> GetCompositeModelDetails(int compositeModelId);
        Task<PriceTrendPredictor> GetPriceTrendPredictor(int compositeModelId);
        Task<IPredictor> GetPricePredictor(int id);
        string[] GetFeatureList();
        Task SaveModel(string modelName, RegressionModel trainedModel);
        Task UpdateModelState(int modelId, ModelState modelState);
        Task UpdateModelName(int modelId, string name);
        Task UpdateModelDescription(int modelId, string description);
        Task UpdateQualityScore(int modelId, int qualityScore);
    }
}