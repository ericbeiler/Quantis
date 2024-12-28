import { useState } from "react";

import PredictionGrid from "./components/PredictionGrid";
import ModelSelector from "./components/ModelSelector";
import ConfigureModelModal from "./components/ConfigureModelModal";

const serverUrl = import.meta.env.VITE_SERVER;

function App() {
  const [selectedModel, setSelectedModel] = useState(7);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const handleBuildModel = async (parameters: {
    numberOfTrees: number;
    numberOfLeaves: number;
    minimumExampleCountPerLeaf: number;
  }) => {
    try {
      const response = await fetch(`${serverUrl}api/Model?equityIndex=SPY&numberOfTrees=${parameters.numberOfTrees}&numberOfLeaves=${parameters.numberOfLeaves}&minimumExampleCountPerLeaf=${parameters.minimumExampleCountPerLeaf}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(parameters),
      });
      if (!response.ok) {
        throw new Error("Failed to build the model");
      }
      alert("Model built successfully!");
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
        <nav className="w-64 bg-gray-100 p-4 shadow-md">
          <ModelSelector
            selectedModel={selectedModel}
            setSelectedModel={setSelectedModel}
          />
        </nav>

        {/* Main Content */}
        <main className="flex-1 bg-white p-6 shadow-md">
          <PredictionGrid
            selectedModel={selectedModel}
            setSelectedModel={setSelectedModel}
          />
        </main>
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
