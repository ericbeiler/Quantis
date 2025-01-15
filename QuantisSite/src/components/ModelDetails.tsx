﻿import React, { useState } from "react";
import CompositeModelDetail from "./CompositeModelDetail";

type ModelDetailsProps = {
  selectedModel: number;
};

const ModelDetails: React.FC<ModelDetailsProps> = ({ selectedModel }) => {
  const [modelDetails, setModelDetails] = React.useState<CompositeModelDetail | null>(null);
  const [showTrainingParams, setShowTrainingParams] = useState(false);
  const [expandedRegressionModels, setExpandedRegressionModels] = useState<{ [key: number]: boolean }>({});

  React.useEffect(() => {
    const fetchModelDetails = async () => {
      try {
        const response = await fetch(`${import.meta.env.VITE_SERVER}api/Model/${selectedModel}`);
        if (!response.ok) {
          throw new Error("Failed to fetch model details");
        }
        const data: CompositeModelDetail = await response.json();
        setModelDetails(data);
      } catch (error) {
        console.error("Error fetching model details:", error);
      }
    };

    if (selectedModel) {
      fetchModelDetails();
    }
  }, [selectedModel]);

  if (!modelDetails) {
    return <div className="p-4">Loading model details...</div>;
  }

  const carrotIcon = (isOpen: boolean) => (isOpen ? "▼" : "▶");

  const toggleRegressionModel = (id: number) => {
    setExpandedRegressionModels((prev) => ({ ...prev, [id]: !prev[id] }));
  };

  const formatNumber = (value: number) => value.toFixed(2);

  return (
    <div className="p-4">
      <h2 className="text-lg font-bold">Model Details</h2>
      <ul className="list-disc space-y-2 pl-5">
        <li><strong>ID:</strong> {modelDetails.Id}</li>
        <li><strong>Name:</strong> {modelDetails.Name}</li>
        <li><strong>Description:</strong> {modelDetails.Description}</li>
        <li><strong>Type:</strong> {modelDetails.Type}</li>
        <li><strong>Quality Score:</strong> {modelDetails.QualityScore}</li>

        <li>
          <button
            onClick={() => setShowTrainingParams(!showTrainingParams)}
            className="flex items-center text-blue-500 underline focus:outline-none"
          >
            {carrotIcon(showTrainingParams)} Training Parameters
          </button>
          {showTrainingParams && (
            <ul className="list-circle space-y-1 pl-5">
              <li><strong>CompositeModelId:</strong> {modelDetails.TrainingParameters.CompositeModelId}</li>
              <li><strong>TargetDurationsInMonths:</strong> {modelDetails.TrainingParameters.TargetDurationsInMonths?.join(", ") || "None"}</li>
              <li><strong>Index:</strong> {modelDetails.TrainingParameters.Index}</li>
              <li><strong>DatasetSizeLimit:</strong> {modelDetails.TrainingParameters.DatasetSizeLimit}</li>
              <li><strong>Algorithm:</strong> {modelDetails.TrainingParameters.Algorithm}</li>
              <li><strong>Granularity:</strong> {modelDetails.TrainingParameters.Granularity}</li>
              <li><strong>MaxTrainingTime:</strong> {modelDetails.TrainingParameters.MaxTrainingTime}</li>
              <li><strong>NumberOfTrees:</strong> {modelDetails.TrainingParameters.NumberOfTrees}</li>
              <li><strong>NumberOfLeaves:</strong> {modelDetails.TrainingParameters.NumberOfLeaves}</li>
              <li><strong>MinimumExampleCountPerLeaf:</strong> {modelDetails.TrainingParameters.MinimumExampleCountPerLeaf}</li>
            </ul>
          )}
        </li>


        <li>
          <h3 className="text-lg font-semibold">Regression Model Details</h3>
          <ul className="list-circle space-y-2 pl-5">
            {modelDetails.RegressionModelDetails.map((detail) => (
              <li key={detail.Id} className="space-y-1">
                <button
                  onClick={() => toggleRegressionModel(detail.Id)}
                  className="flex items-center text-blue-500 underline focus:outline-none"
                >
                  {carrotIcon(expandedRegressionModels[detail.Id])} <strong>Target Duration:</strong> {detail.TargetDuration} Months
                </button>
                {expandedRegressionModels[detail.Id] && (
                  <ul className="list-disc space-y-1 pl-5">
                    <li><strong>Timestamp:</strong> {detail.Timestamp}</li>
                    <li><strong>Type:</strong> {detail.Type}</li>
                    <li><strong>Target Duration:</strong> {detail.TargetDuration}</li>
                    <li><strong>RSquared:</strong> {formatNumber(detail.RSquared)}</li>
                    <li><strong>Mean Absolute Error:</strong> {formatNumber(detail.MeanAbsoluteError)}</li>
                    <li><strong>Root Mean Squared Error:</strong> {formatNumber(detail.RootMeanSquaredError)}</li>
                    <li><strong>Loss Function:</strong> {formatNumber(detail.LossFunction)}</li>
                    <li>
                      <strong>Cross Validations</strong>
                      <ul>
                        <li><strong>Average Pearson Correlation:</strong> {formatNumber(detail.AveragePearsonCorrelation)}</li>
                        <li><strong>Minimum Pearson Correlation:</strong> {formatNumber(detail.MinimumPearsonCorrelation)}</li>
                        <li><strong>Average Spearman Rank Correlation:</strong> {formatNumber(detail.AverageSpearmanRankCorrelation)}</li>
                        <li><strong>Minimum Spearman Rank Correlation:</strong> {formatNumber(detail.MinimumSpearmanRankCorrelation)}</li>
                        <li><strong>Average Mean Absolute Error:</strong> {formatNumber(detail.CrossValAverageMeanAbsoluteError)}</li>
                        <li><strong>Maximum Mean Absolute Error:</strong> {formatNumber(detail.CrossValMaximumMeanAbsoluteError)}</li>
                        <li><strong>Average Root Mean Squared Error:</strong> {formatNumber(detail.CrossValAverageRootMeanSquaredError)}</li>
                        <li><strong>Maximum Root Mean Squared Error:</strong> {formatNumber(detail.CrossValMaximumRootMeanSquaredError)}</li>
                        <li><strong>Average RSquared:</strong> {formatNumber(detail.CrossValAverageRSquared)}</li>
                        <li><strong>Maximum RSquared:</strong> {formatNumber(detail.CrossValMaximumRSquared)}</li>
                      </ul>
                    </li>
                  </ul>
                )}
              </li>
            ))}
          </ul>
        </li>
      </ul>
    </div>
  );
};

export default ModelDetails;
