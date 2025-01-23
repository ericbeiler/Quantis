import ModelType from "./ModelType.ts";
import RegressionModelDetail from "./RegressionModelDetail.ts"
import RegressionTrainingParameters from "./RegressionTrainingParameters.ts"

interface CompositeModelDetail {
  TrainingParameters: RegressionTrainingParameters;
  Id: number;
  Name: string;
  Description: string;
  Type: ModelType;
  RegressionModelDetails: RegressionModelDetail[];
  QualityScore: number;
  Features: string[];
}

export default CompositeModelDetail