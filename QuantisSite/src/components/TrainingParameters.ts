interface TrainingParameters {
  CompositeModelId: number | null;
  TargetDurationsInMonths: number[] | null;
  Index: string | null;
  DatasetSizeLimit: number | null;
  Algorithm: string | null;
  Granularity: number | null;
  MaxTrainingTime: number | null;
  NumberOfTrees: number | null;
  NumberOfLeaves: number | null;
  MinimumExampleCountPerLeaf: number | null;
}

export default TrainingParameters