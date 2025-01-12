import React, { useState } from "react";
import CompositeModelDetail from "./CompositeModelDetail";

type ModelDetailsProps = {
  selectedModel: number;
};

const ModelDetails: React.FC<ModelDetailsProps> = ({ selectedModel }) => {
  const [modelDetails, setModelDetails] = React.useState<CompositeModelDetail | null>(null);
  const [showTrainingParams, setShowTrainingParams] = useState(false);
  const [showRegressionDetails, setShowRegressionDetails] = useState(false);

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
          <button
            onClick={() => setShowRegressionDetails(!showRegressionDetails)}
            className="flex items-center text-blue-500 underline focus:outline-none"
          >
            {carrotIcon(showRegressionDetails)} Regression Model Details
          </button>
          {showRegressionDetails && (
            <ul className="list-circle space-y-1 pl-5">
              {modelDetails.RegressionModelDetails.map((detail) => (
                <li key={detail.Id}>
                  <strong>Type:</strong> {detail.Type}, <strong>Target Duration:</strong> {detail.TargetDuration}, <strong>RSquared:</strong> {detail.RSquared}
                </li>
              ))}
            </ul>
          )}
        </li>
      </ul>
    </div>
  );
};

export default ModelDetails;
