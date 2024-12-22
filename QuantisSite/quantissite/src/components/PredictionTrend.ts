import PredictionPoint from "./PredictionPoint";

interface PredictionTrend {
  Ticker: string;
  PricePoints: PredictionPoint[];
}

export default PredictionTrend;