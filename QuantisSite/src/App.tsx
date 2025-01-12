import { useState } from "react";

import PredictionGrid from "./components/PredictionGrid";
import ModelSelector from "./components/ModelSelector";
import ConfigureModelModal, { TrainingGranularity } from "./components/ConfigureModelModal";
import ModelDetails from "./components/ModelDetails";

const serverUrl = import.meta.env.VITE_SERVER;

function App() {
  const [selectedModel, setSelectedModel] = useState(0);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [showDetails, setShowDetails] = useState(true); // Control collapsible panel

  const handleBuildModel = async (parameters: {
    trainingGranularity: TrainingGranularity;
    numberOfTrees: number;
    numberOfLeaves: number;
    minimumExampleCountPerLeaf: number;
  }) => {
    try {
      const response = await fetch(`${serverUrl}api/Model?equityIndex=SPY&granularity=${parameters.trainingGranularity}&numberOfTrees=${parameters.numberOfTrees}&numberOfLeaves=${parameters.numberOfLeaves}&minimumExampleCountPerLeaf=${parameters.minimumExampleCountPerLeaf}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(parameters),
      });
      if (!response.ok) {
        throw new Error("Failed to build the model");
      }
      alert("Model queued for training.");
    } catch (error) {
      alert("Error: " + error);
    }
  };

  return (
    <div className="flex h-screen flex-col">
      {/* Header */}
      <header className="flex items-center justify-between bg-blue-600 p-4 text-white shadow-md">
        <h1 className="text-xl font-bold">Quartis Predictions</h1>
        <button
          className="rounded bg-white px-4 py-2 text-blue-600 shadow hover:bg-gray-200"
          onClick={() => setIsModalOpen(true)}
        >
          Configure Model
        </button>
      </header>

      <div className="flex-1 flex">
        {/* Sidebar */}
        <nav className="resizable-sidebar w-64 bg-gray-100 p-4 shadow-md">
          <ModelSelector
            selectedModel={selectedModel}
            setSelectedModel={setSelectedModel}
          />
        </nav>

        {/* Main Content */}
        <main className="flex-1 bg-white p-6 shadow-md">
          <PredictionGrid
            selectedModel={selectedModel}
          />
        </main>

        {/* Collapsible Panel */}
        {showDetails && (
          <aside className="w-1/4 bg-gray-200 p-4 shadow-md">
            <ModelDetails selectedModel={selectedModel} />
          </aside>
        )}
        <button
          className="absolute right-4 top-4 rounded bg-blue-500 p-2 text-white"
          onClick={() => setShowDetails(!showDetails)}
        >
          {showDetails ? "Hide Details" : "Show Details"}
        </button>
      </div>

      {/* Footer */}
      <footer className="bg-gray-800 p-4 text-center text-sm text-gray-400">
        <p>Visavi Software, (c) 2024</p>
      </footer>

      {/* Modal */}
      <ConfigureModelModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSubmit={handleBuildModel}
      />
    </div>
  );
}

export default App;
