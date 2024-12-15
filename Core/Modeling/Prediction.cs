namespace Visavi.Quantis.Modeling
{
    public class Prediction
    {
        public Prediction() { }
        public string Ticker { get; set; }
        public DateOnly StartingDate { get; set; }
        public decimal StartingPrice { get; set; }
        public DateOnly EndingDate { get; set; }
        public decimal? PredictedEndingPrice { get; set; }
        public decimal? ExpectedPriceRangeLow { get; set; }
        public decimal? ExpectedPriceRangeHigh { get; set; }
        public float? PredictedCagr { get; set; }
        public float? ExpectedCagrRangeLow { get; set; }
        public float? ExpectedCagrRangeHigh { get; set; }
    }
}
