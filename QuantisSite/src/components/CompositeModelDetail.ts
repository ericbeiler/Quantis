import RegressionModelDetail from "./RegressionModelDetail.ts"
import TrainingParameters from "./TrainingParameters.ts"

interface CompositeModelDetail {
  TrainingParameters: TrainingParameters;
  Id: number;
  Name: string;
  Description: string;
  Type: number;
  RegressionModelDetails: RegressionModelDetail[];
  QualityScore: number;
}

export default CompositeModelDetail