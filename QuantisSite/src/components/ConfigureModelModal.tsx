import React, { useState } from "react";
import TrainingGranularity from "./TrainingGranularity";
import ModelConfiguration from "./ModelConfiguration";

interface ConfigureModelModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (parameters: ModelConfiguration) => void;
}

const ConfigureModelModal: React.FC<ConfigureModelModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
}) => {
  React.useEffect(() => {
    const fetchFeatureList = async () => {
      try {
        const response = await fetch(`${import.meta.env.VITE_SERVER}api/FeatureList`);
        if (!response.ok) {
          throw new Error("Failed to fetch current feature list");
        }
        const data: string[] = await response.json();
        setFeatureList(data);
      } catch (error) {
        console.error("Error fetching feature list:", error);
      }
    };
    fetchFeatureList();
  }, []);

  const [parameters, setParameters] = useState<ModelConfiguration>({
    NumberOfTrees: 100,
    NumberOfLeaves: 20,
    MinimumExampleCountPerLeaf: 10,
    Granularity: TrainingGranularity.Monthly,
    Features: [],
  });

  const [showInfo, setShowInfo] = useState(false);
  const [featureList, setFeatureList] = useState<string[]>([]);

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

  const handleFeatureToggle = (feature: string) => {
    setParameters((prev) => ({
      ...prev,
      Features: prev.Features.includes(feature)
        ? prev.Features.filter((f) => f !== feature)
        : [...prev.Features, feature],
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
            value={parameters.Granularity}
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
            name="NumberOfTrees"
            value={parameters.NumberOfTrees}
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
            name="NumberOfLeaves"
            value={parameters.NumberOfLeaves}
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
            name="MinimumExampleCountPerLeaf"
            value={parameters.MinimumExampleCountPerLeaf}
            onChange={handleInputChange}
            className="mt-1 w-full rounded border-gray-300 shadow-sm focus:border-blue-500 focus:ring focus:ring-blue-500"
          />
        </div>
        <div>
          <label className="block flex-grow text-sm font-medium text-gray-700">
            Features
          </label>
          <ul className="space-y-2">
            {featureList.map((feature) => (
              <li key={feature} className="flex items-center">
                <input
                  type="checkbox"
                  id={feature}
                  value={feature}
                  checked={parameters?.Features === null || parameters.Features.includes(feature)}
                  onChange={() => handleFeatureToggle(feature)}
                  className="mr-2"
                />
                <label htmlFor={feature} className="text-sm">
                  {feature}
                </label>
              </li>
            ))}
          </ul>
        </div>
        <div>
          <button
            className="ml-2 text-blue-500 underline"
            onClick={() => setShowInfo(true)}
          >
            Parameter Descriptions
          </button>
        </div>
        <div className="mt-4 flex justify-end">
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
