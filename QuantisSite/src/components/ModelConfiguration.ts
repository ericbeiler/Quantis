import TrainingGranularity from "./TrainingGranularity";

interface ModelConfiguration {
  Granularity: TrainingGranularity;
  NumberOfTrees: number;
  NumberOfLeaves: number;
  MinimumExampleCountPerLeaf: number;
  Features: string[] | null;
}

export default ModelConfiguration