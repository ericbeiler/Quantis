
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
    <div>
      <h2>Models</h2>
      <ul>
        {modelList.map((model) => (
          <li key={model.Id} onClick={() => setSelectedModel(model.Id)}>
            {model.Name}
          </li>
      ))}
      </ul>
      <h3>Selected: {selectedModel}</h3>
    </div>
  );
}

export default ModelSelector;