import { useState, useEffect } from "react";
import { AgGridReact } from 'ag-grid-react'; // React Data Grid Component
import { ColDef } from 'ag-grid-community'; // Column Definition Interface
import "ag-grid-community/styles/ag-grid.css"; // Mandatory CSS required by the Data Grid
import "ag-grid-community/styles/ag-theme-alpine.css"; // Optional Theme applied to the Data Grid

import Prediction from "./Prediction";

const serverUrl = import.meta.env.VITE_SERVER;

const PredictionGrid2 = () => {

  // Row Data: The data to be displayed.
  const [predictions, setRowData] = useState<Prediction[]>([]);
  
  // Column Definitions: Defines the columns to be displayed.
  const predictionColumns: ColDef<Prediction>[] = [
    { field: 'Ticker', headerName: 'Ticker', width: 100 },
    { field: 'StartingDate', headerName: 'Starting Date', width: 150 },
    { field: 'StartingPrice', headerName: 'Starting Price', width: 150, valueFormatter: params => params?.data?.StartingPrice?.toFixed(2) },
    { field: 'EndingDate', headerName: 'Ending Date', width: 150 },
    { field: 'PredictedEndingPrice', headerName: 'Predicted Price', width: 150, valueFormatter: params => params?.data?.PredictedEndingPrice?.toFixed(2) },
    { field: 'ExpectedPriceRangeLow', headerName: 'Expected Low Price', width: 150, valueFormatter: params => params?.data?.ExpectedPriceRangeLow?.toFixed(2) },
    { field: 'ExpectedPriceRangeHigh', headerName: 'Expected High Price', width: 150, valueFormatter: params => params?.data?.ExpectedPriceRangeHigh?.toFixed(2) },
    { field: 'PredictedCagr', headerName: 'Predicted CAGR', width: 150, valueFormatter: params => params?.data?.PredictedCagr?.toFixed(2) }
  ];
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchPredictions = async () => {
      try {
        // Fetch the data from the API
        const response = await fetch(`${serverUrl}api/Predictions/8?ticker=SPY`);
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

export default PredictionGrid2;