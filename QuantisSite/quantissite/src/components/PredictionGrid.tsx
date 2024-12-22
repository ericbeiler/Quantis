import { useState, useEffect } from "react";
import { AgGridReact } from 'ag-grid-react'; // React Data Grid Component
import { ColDef } from 'ag-grid-community'; // Column Definition Interface
import "ag-grid-community/styles/ag-grid.css"; // Mandatory CSS required by the Data Grid
import "ag-grid-community/styles/ag-theme-alpine.css"; // Optional Theme applied to the Data Grid

import ModelSelectorProps from "./ModelSelectorProps";
import PredictionTrend from "./PredictionTrend";

const serverUrl = import.meta.env.VITE_SERVER;

const PredictionGrid: React.FC<ModelSelectorProps> = ({ selectedModel }) => {

  // Formatter function: Formats a numeric value to 2 decimal places or returns a default string.
  const formatToPrice = (value?: number | null, defaultValue: string = 'N/A') =>
    value !== undefined && value !== null ? value.toFixed(2) : defaultValue;

  // Row Data: The data to be displayed.
  const [predictions, setRowData] = useState<PredictionTrend[]>([]);
  
  // Column Definitions: Defines the columns to be displayed.
  const predictionColumns: ColDef<PredictionTrend>[] = [
    { field: 'Ticker', headerName: 'Ticker', width: 100 },
    {
      headerName: 'Starting Date', width: 120,
      valueGetter: params => params.data?.PricePoints?.[0]?.StartingDate 
    },
    {
      headerName: 'Starting Price', width: 120,
      valueFormatter: params => formatToPrice(params.data?.PricePoints?.[0]?.StartingPrice),
      valueGetter: params => params.data?.PricePoints?.[0]?.StartingPrice 
    },
    {
      headerName: 'Year 1 Price', width: 120,
      valueFormatter: params => formatToPrice(params.data?.PricePoints?.[0]?.PredictedEndingPrice),
      valueGetter: params => params.data?.PricePoints?.[0]?.PredictedEndingPrice
    },
    {
      headerName: 'Year 2 Price', width: 120,
      valueFormatter: params => formatToPrice(params.data?.PricePoints?.[1]?.PredictedEndingPrice),
      valueGetter: params => params.data?.PricePoints?.[1]?.PredictedEndingPrice
    },
    {
      headerName: 'Year 3 Price', width: 120,
      valueFormatter: params => formatToPrice(params.data?.PricePoints?.[2]?.PredictedEndingPrice),
      valueGetter: params => params.data?.PricePoints?.[2]?.PredictedEndingPrice
    },
    {
      headerName: 'Year 5 Price', width: 120,
      valueFormatter: params => formatToPrice(params.data?.PricePoints?.[3]?.PredictedEndingPrice),
      valueGetter: params => params.data?.PricePoints?.[3]?.PredictedEndingPrice
    },
    {
      headerName: 'Year 1 CAGR', width: 120,
      valueFormatter: params => formatToPrice(params.data?.PricePoints?.[0]?.PredictedCagr),
      valueGetter: params => params.data?.PricePoints?.[0]?.PredictedCagr
    },
    {
      headerName: 'Year 2 CAGR', width: 120,
      valueFormatter: params => formatToPrice(params.data?.PricePoints?.[1]?.PredictedCagr),
      valueGetter: params => params.data?.PricePoints?.[1]?.PredictedCagr
    },
    {
      headerName: 'Year 3 CAGR', width: 120,
      valueFormatter: params => formatToPrice(params.data?.PricePoints?.[2]?.PredictedCagr),
      valueGetter: params => params.data?.PricePoints?.[2]?.PredictedCagr
    },
    {
      headerName: 'Year 5 CAGR', width: 120,
      valueFormatter: params => formatToPrice(params.data?.PricePoints?.[3]?.PredictedCagr),
      valueGetter: params => params.data?.PricePoints?.[3]?.PredictedCagr
    }
//    { field: 'EndingDate', headerName: 'Ending Date', width: 150 },
//    { field: 'ExpectedPriceRangeLow', headerName: 'Expected Low Price', width: 150, valueFormatter: params => formatToPrice(params?.data?.ExpectedPriceRangeLow) },
//    { field: 'ExpectedPriceRangeHigh', headerName: 'Expected High Price', width: 150, valueFormatter: params => formatToPrice(params?.data?.ExpectedPriceRangeHigh) },
  ];
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchPredictions = async () => {
      try {
        // Fetch the data from the API
        const response = await fetch(`${serverUrl}api/Predictions/${selectedModel}?ticker=SPY`);
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
  }, [selectedModel]); // Empty dependency array means this runs once after the component mounts

  return (
    // wrapping container with theme & size
    <div
      className="ag-theme-quartz" // applying the Data Grid theme
      style={{ height: 1000 }} // the Data Grid will fill the size of the parent container
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