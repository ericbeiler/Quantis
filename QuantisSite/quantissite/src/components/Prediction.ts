interface Prediction {
  Ticker: string;
  StartingDate: string;
  StartingPrice: number;
  EndingDate: string;
  PredictedEndingPrice: number | null;
  ExpectedPriceRangeLow: number | null;
  ExpectedPriceRangeHigh: number | null;
  PredictedCagr: number | null;
}

export default Prediction;