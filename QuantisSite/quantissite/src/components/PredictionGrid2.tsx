import React, { useState, useEffect } from "react";
import { AgGridReact } from 'ag-grid-react'; // React Data Grid Component
import "ag-grid-community/styles/ag-grid.css"; // Mandatory CSS required by the Data Grid
import "ag-grid-community/styles/ag-theme-alpine.css"; // Optional Theme applied to the Data Grid

const serverUrl = import.meta.env.VITE_SERVER;

const PredictionGrid2 = () => {

  // Row Data: The data to be displayed.
  const [predictions, setRowData] = useState([
    { Ticker: "Loading", model: "", StartingPrice: 0, EndingDate: false },
  ]);

  
  // Column Definitions: Defines the columns to be displayed.
  const [predictionColumns, setColDefs] = useState([
    { field: 'Ticker', headerName: 'Ticker', width: 100 },
    { field: 'StartingDate', headerName: 'Starting Date', width: 150 },
    { field: 'StartingPrice', headerName: 'Starting Price', width: 150, },
    { field: 'EndingDate', headerName: 'Ending Date', width: 150 },
    { field: 'PredictedEndingPrice', headerName: 'Predicted Price', width: 150 },
    { field: 'ExpectedPriceRangeLow', headerName: 'Expected Low Price', width: 150 },
    { field: 'ExpectedPriceRangeHigh', headerName: 'Expected High Price', width: 150 },
    { field: 'PredictedCagr', headerName: 'Predicted CAGR', width: 150 },
  ]);
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
      style={{ height: 1000, width: 1000 }} // the Data Grid will fill the size of the parent container
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

  /*
  useEffect(() => {
    const fetchPredictions = async () => {
      try {
        // Fetch the data from the API
        const response = await fetch(`${serverUrl}api/Predictions/8?ticker=SPY`);
        const predictionArray = await response.json();

        // Update state
        setPredictions(predictionArray);
      } catch (error) {
        console.error("Error fetching predictions:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchPredictions();
  }, []); // Empty dependency array means this runs once after the component mounts


  return (
    <div className="ag-theme-quartz" >
      <h2>Predictions</h2>
      {loading ? (
        <p>Loading predictions...</p>
      ) : (
        <div>
          <p>Prediction Count: {predictions.length}</p>
          <AgGridReact
              rowData={rowData}
              columnDefs={colDefs}
            />
        </div>
      )}
    </div>
  );
  */
};

export default PredictionGrid2;