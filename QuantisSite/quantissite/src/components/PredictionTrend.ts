import PredictionPoint from "./PredictionPoint";

interface PredictionTrend {
  Ticker: string;
  PredictionPoints: PredictionPoint[];
}

export default PredictionTrend;