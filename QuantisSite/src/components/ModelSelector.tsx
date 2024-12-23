
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
        console.info('Fetching model data from ' + serverUrl + 'api/Model')
        const response = await fetch(`${serverUrl}api/Model`);
        const models = await response.json();

        // Update state
        setModelList(models);
      } catch (error) {
        console.error("Error fetching models:", error);
      } 
    };

    fetchModels();
  }, []); // Empty dependency array means this runs once after the component mounts

  return (
    <div className="p-6 bg-white space-y-4 mx-auto max-w-lg rounded-xl shadow-md">
      <h2 className="text-gray-900 text-xl font-bold">Models</h2>
      <ul className="space-y-2"> {modelList.map((model) => (
        <li key={model.Id} className={`cursor-pointer p-2 rounded-lg ${selectedModel === model.Id ? 'bg-blue-100' : 'bg-gray-100'} hover:bg-blue-200`}
          onClick={() => setSelectedModel(model.Id)} > {model.Name} </li>))}
      </ul>
      <h3 className="text-gray-700 text-lg font-semibold">Selected: {selectedModel}</h3>
    </div>);
}

export default ModelSelector;