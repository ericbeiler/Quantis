import React, { useState } from "react";

export enum TrainingGranularity {
  Daily = "Daily",
  Weekly = "Weekly",
  Monthly = "Monthly",
}

interface ConfigureModelModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (parameters: {
    numberOfTrees: number;
    numberOfLeaves: number;
    minimumExampleCountPerLeaf: number;
    trainingGranularity: TrainingGranularity;
  }) => void;
}

const ConfigureModelModal: React.FC<ConfigureModelModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
}) => {
  const [parameters, setParameters] = useState({
    numberOfTrees: 100,
    numberOfLeaves: 20,
    minimumExampleCountPerLeaf: 10,
    trainingGranularity: TrainingGranularity.Monthly,
  });

  const [showInfo, setShowInfo] = useState(false); // Track info modal visibility

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setParameters((prev) => ({
      ...prev,
      [name]: parseInt(value, 10),
    }));
  };

  const handleSelectChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const { name, value } = e.target;
    setParameters((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleSubmit = () => {
    onSubmit(parameters);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="w-96 rounded-lg bg-white p-6 shadow-lg">
        <h2 className="mb-4 text-xl font-bold">Configure Model</h2>
        <div className="mb-4">
          <label className="block text-sm font-medium text-gray-700">
            Training Granularity
          </label>
          <select
            name="trainingGranularity"
            value={parameters.trainingGranularity}
            onChange={handleSelectChange}
            className="mt-1 w-full rounded border-gray-300 shadow-sm focus:border-blue-500 focus:ring focus:ring-blue-500"
            disabled
          >
            {Object.values(TrainingGranularity).map((granularity) => (
              <option key={granularity} value={granularity}>
                {granularity}
              </option>
            ))}
          </select>
        </div>
        <div className="mb-4 flex items-center">
          <label className="block flex-grow text-sm font-medium text-gray-700">
            Number of Trees
          </label>
          <input
            type="number"
            name="numberOfTrees"
            value={parameters.numberOfTrees}
            onChange={handleInputChange}
            className="mt-1 w-full rounded border-gray-300 shadow-sm focus:border-blue-500 focus:ring focus:ring-blue-500"
          />
        </div>
        <div className="mb-4 flex items-center">
          <label className="block flex-grow text-sm font-medium text-gray-700">
            Number of Leaves
          </label>
          <input
            type="number"
            name="numberOfLeaves"
            value={parameters.numberOfLeaves}
            onChange={handleInputChange}
            className="mt-1 w-full rounded border-gray-300 shadow-sm focus:border-blue-500 focus:ring focus:ring-blue-500"
          />
        </div>
        <div className="mb-4 flex items-center">
          <label className="block flex-grow text-sm font-medium text-gray-700">
            Minimum Example Count per Leaf
          </label>
          <input
            type="number"
            name="minimumExampleCountPerLeaf"
            value={parameters.minimumExampleCountPerLeaf}
            onChange={handleInputChange}
            className="mt-1 w-full rounded border-gray-300 shadow-sm focus:border-blue-500 focus:ring focus:ring-blue-500"
          />
        </div>
        <div>
          <button
            className="ml-2 text-blue-500 underline"
            onClick={() => setShowInfo(true)}
          >
            Parameter Descriptions
          </button>
        </div>
        <div className="flex justify-end">
          <button
            className="mr-2 rounded bg-gray-200 px-4 py-2 shadow hover:bg-gray-300"
            onClick={onClose}
          >
            Cancel
          </button>
          <button
            className="rounded bg-blue-600 px-4 py-2 text-white shadow hover:bg-blue-700"
            onClick={handleSubmit}
          >
            Build Model
          </button>
        </div>
      </div>

      {/* Info Modal */}
      {showInfo && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
          <div className="w-96 rounded-lg bg-white p-6 shadow-lg">
            <h3 className="mb-4 text-lg font-bold">Parameter Descriptions</h3>
            <p className="mb-2">
              <strong>Number of Trees:</strong> Specifies how many decision trees are in the ensemble.
              <ul className="ml-4 list-disc">
                <li>
                  <strong>Impact:</strong> More trees can capture complex patterns but increase training time. Too many trees may overfit.
                </li>
                <li>
                  <strong>Heuristic:</strong> Start with (log(record count) * 10). For 50,000 records, 50-100 trees are reasonable.
                </li>
              </ul>
            </p>
            <p className="mb-2">
              <strong>Number of Leaves:</strong> Determines the maximum depth of each tree by specifying the number of final decisions (leaf nodes).
              <ul className="ml-4 list-disc">
                <li>
                  <strong>Impact:</strong> More leaves allow trees to fit finer patterns but may overfit.
                </li>
                <li>
                  <strong>Heuristic:</strong> (min(sqrt(record_count), 2^(feature_count)). For 50,000 records with 20 features, start with 100-250 leaves.
                </li>
              </ul>
            </p>
            <p className="mb-2">
              <strong>Minimum Example Count per Leaf:</strong> Controls how many records must exist in a leaf to create a split.
              <ul className="ml-4 list-disc">
                <li>
                  <strong>Impact:</strong> Prevents overfitting by enforcing generalization. Higher values reduce overfitting.
                </li>
                <li>
                  <strong>Heuristic:</strong> [record_count] / (5 * [Number of Leaves]). For 50,000 records and 200 leaves, start with 50.
                </li>
              </ul>
            </p>
            <button
              className="mt-4 rounded bg-blue-600 px-4 py-2 text-white shadow hover:bg-blue-700"
              onClick={() => setShowInfo(false)}
            >
              Close
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default ConfigureModelModal;
