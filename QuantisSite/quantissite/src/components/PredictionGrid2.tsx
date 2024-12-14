import { useState, useEffect } from "react";
import { AgGridReact } from 'ag-grid-react'; // React Data Grid Component
import { ColDef } from 'ag-grid-community'; // Column Definition Interface
import "ag-grid-community/styles/ag-grid.css"; // Mandatory CSS required by the Data Grid
import "ag-grid-community/styles/ag-theme-alpine.css"; // Optional Theme applied to the Data Grid
import Prediction from "./Prediction";

const serverUrl = import.meta.env.VITE_SERVER;

const PredictionGrid2 = () => {

  // Row Data: The data to be displayed.
  const [predictions, setRowData] = useState([
    { Ticker: "Loading", model: "", StartingPrice: 0, EndingDate: false },
  ]);

  const fieldName = (name: keyof Prediction) => name;
  
  // Column Definitions: Defines the columns to be displayed.
  const predictionColumns: ColDef[] = [
    { field: fieldName('Ticker'), headerName: 'Ticker', width: 100 },
    { field: fieldName('StartingDate'), headerName: 'Starting Date', width: 150 },
    { field: fieldName('StartingPrice'), headerName: 'Starting Price', width: 150, },
    { field: fieldName('EndingDate'), headerName: 'Ending Date', width: 150 },
    { field: fieldName('PredictedEndingPrice'), headerName: 'Predicted Price', width: 150 },
    { field: fieldName('ExpectedPriceRangeLow'), headerName: 'Expected Low Price', width: 150 },
    { field: fieldName('ExpectedPriceRangeHigh'), headerName: 'Expected High Price', width: 150 },
    { field: fieldName('PredictedCagr'), headerName: 'Predicted CAGR', width: 150 }
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