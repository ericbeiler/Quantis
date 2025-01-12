
interface RegressionModelDetail {
  Id: number;
  Type: string;
  TargetDuration: number;
  Timestamp: string; // ISO 8601 string
  MeanAbsoluteError: number;
  RootMeanSquaredError: number;
  LossFunction: number;
  RSquared: number;
  AveragePearsonCorrelation: number;
  MinimumPearsonCorrelation: number;
  AverageSpearmanRankCorrelation: number;
  MinimumSpearmanRankCorrelation: number;
  CrossValAverageMeanAbsoluteError: number;
  CrossValMaximumMeanAbsoluteError: number;
  CrossValAverageRootMeanSquaredError: number;
  CrossValMaximumRootMeanSquaredError: number;
  CrossValAverageRSquared: number;
  CrossValMaximumRSquared: number;
}

export default RegressionModelDetail