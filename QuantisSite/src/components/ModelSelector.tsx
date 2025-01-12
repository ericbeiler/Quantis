import { useState, useEffect } from "react";
import ModelSummary from "./ModelSummary";
import ModelSelectorProps from "./ModelSelectorProps";

const serverUrl = import.meta.env.VITE_SERVER;

const ModelSelector: React.FC<ModelSelectorProps> = ({ selectedModel, setSelectedModel }) => {
  const [modelList, setModelList] = useState<ModelSummary[]>([]);

  useEffect(() => {
    const fetchModels = async () => {
      try {
        // Fetch the data from the API
        const response = await fetch(`${serverUrl}api/Model`);
        const models = await response.json();

        // Update state
        setModelList(models);
      } catch (error) {
        console.error("Error fetching models:", error);
      }
    };

    fetchModels();
  }, []);

  return (
    <div className="resizable mx-auto max-w-lg space-y-4 rounded-xl bg-white p-6 shadow-md">
      <h2 className="text-xl font-bold text-gray-900">Models</h2>
      <ul className="space-y-2">
        {modelList.map((model) => (
          <li
            key={model.Id}
            className={`cursor-pointer p-2 rounded-lg ${selectedModel === model.Id ? "bg-blue-100" : "bg-gray-100"} hover:bg-blue-200`}
            onClick={() => setSelectedModel(model.Id)}
            title={`Model: ${model.Name}`}
          >
            {model.Name}
          </li>
        ))}
      </ul>
      <h3 className="text-lg font-semibold text-gray-700">Selected: {selectedModel}</h3>
    </div>
  );
};

export default ModelSelector;