import { useState } from "react";

import PredictionGrid from "./components/PredictionGrid";
import ModelSelector from "./components/ModelSelector";
import ConfigureModelModal from "./components/ConfigureModelModal";
import ModelDetails from "./components/ModelDetails";
import ModelConfiguration from "./components/ModelConfiguration";

const serverUrl = import.meta.env.VITE_SERVER;

function App() {
  const [selectedModel, setSelectedModel] = useState(0);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isModelDetailsOpen, setIsModelDetailsOpen] = useState(true); // Toggle model details sidebar

  const handleBuildModel = async (parameters: ModelConfiguration) => {
    try {
      const response = await fetch(`${serverUrl}api/Model`, {
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
      <header className="relative flex items-center justify-between bg-blue-600 p-4 text-white shadow-md">
        <h1 className="text-xl font-bold">Quartis Predictions</h1>
        <button
          className="rounded bg-white px-4 py-2 text-blue-600 shadow hover:bg-gray-200"
          onClick={() => setIsModalOpen(true)}
        >
          Configure Model
        </button>
      </header>

      <div className="flex-1 relative flex">
        {/* Sidebar on Left (Model Selector) */}
        <nav className="resizable-sidebar w-64 bg-gray-100 p-4 shadow-md">
          <ModelSelector selectedModel={selectedModel} setSelectedModel={setSelectedModel} />
        </nav>

        {/* Main Content */}
        <main className="flex-1 bg-white p-6 shadow-md">
          <PredictionGrid selectedModel={selectedModel} />
        </main>

        {/* Toggle Button Placement */}
        {isModelDetailsOpen ? (
          <button
            className="absolute right-[25%] top-16 rounded bg-blue-500 p-2 text-white shadow hover:bg-blue-600"
            onClick={() => setIsModelDetailsOpen(false)}
          >
            ▶
          </button>
        ) : (
          <button
            className="absolute right-4 top-20 rounded bg-blue-500 p-2 text-white shadow hover:bg-blue-600"
            onClick={() => setIsModelDetailsOpen(true)}
          >
            ◀
          </button>
        )}

        {/* Model Details Sidebar */}
        <aside
          className={`transition-transform transform bg-gray-200 p-4 shadow-md ${isModelDetailsOpen ? "translate-x-0 w-1/4" : "translate-x-full w-0"
            }`}
        >
          {isModelDetailsOpen && <ModelDetails selectedModel={selectedModel} />}
        </aside>
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
