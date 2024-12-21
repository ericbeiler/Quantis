import { useState, useEffect } from "react";
import { AgGridReact } from 'ag-grid-react'; // React Data Grid Component
import { ColDef } from 'ag-grid-community'; // Column Definition Interface
import "ag-grid-community/styles/ag-grid.css"; // Mandatory CSS required by the Data Grid
import "ag-grid-community/styles/ag-theme-alpine.css"; // Optional Theme applied to the Data Grid

import PredictionTrend from "./PredictionTrend";

const serverUrl = import.meta.env.VITE_SERVER;

const PredictionGrid = () => {

  // Formatter function: Formats a numeric value to 2 decimal places or returns a default string.
//  const formatToPrice = (value?: number | null, defaultValue: string = 'N/A') =>
//    value !== undefined && value !== null ? value.toFixed(2) : defaultValue;

  // Row Data: The data to be displayed.
  const [predictions, setRowData] = useState<PredictionTrend[]>([]);
  
  // Column Definitions: Defines the columns to be displayed.
  const predictionColumns: ColDef<PredictionTrend>[] = [
    { field: 'Ticker', headerName: 'Ticker', width: 100 }//,
//    { field: 'PredictionPoints[0].StartingDate', headerName: 'Starting Date', width: 150 },
//    { field: 'StartingPrice', headerName: 'Starting Price', width: 150, valueFormatter: params => formatToPrice(params?.data?.StartingPrice) },
//    { field: 'EndingDate', headerName: 'Ending Date', width: 150 },
//    { field: 'PredictedEndingPrice', headerName: 'Predicted Price', width: 150, valueFormatter: params => formatToPrice(params?.data?.PredictedEndingPrice) },
//    { field: 'ExpectedPriceRangeLow', headerName: 'Expected Low Price', width: 150, valueFormatter: params => formatToPrice(params?.data?.ExpectedPriceRangeLow) },
//    { field: 'ExpectedPriceRangeHigh', headerName: 'Expected High Price', width: 150, valueFormatter: params => formatToPrice(params?.data?.ExpectedPriceRangeHigh) },
//    { field: 'PredictedCagr', headerName: 'Predicted CAGR', width: 150, valueFormatter: params => formatToPrice(params?.data?.PredictedCagr) }
  ];
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchPredictions = async () => {
      try {
        // Fetch the data from the API
        const response = await fetch(`${serverUrl}api/Predictions/5?ticker=SPY`);
        const predictionArray = await response.json();

        // Update state
        setRowData(predictionArray);
      } catch (error) {
        console.error("Error fetching predictions:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchPredictions();
  }, []); // Empty dependency array means this runs once after the component mounts

  return (
    // wrapping container with theme & size
    <div
      className="ag-theme-quartz" // applying the Data Grid theme
      style={{ height: 1000, width: 1400 }} // the Data Grid will fill the size of the parent container
    >
      <AgGridReact
        rowData={predictions}
        columnDefs={predictionColumns}
      />
      {loading ?
        (<p>Loading Data...</p>
        ) : (
          <p>Prediction Count: {predictions.length}</p>
        )}
    </div>
  )
};

export default PredictionGrid;