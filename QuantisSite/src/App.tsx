import { useState } from "react";

import PredictionGrid from './components/PredictionGrid'  
import ModelSelector from './components/ModelSelector'


function App() {
  const [selectedModel, setSelectedModel] = useState(7);

  return (
    <div className="flex h-screen flex-col">
      {/* Header */}
      <header className="flex items-center justify-between bg-blue-600 p-4 text-white shadow-md">
        <h1 className="text-xl font-bold">Quartis Predictions</h1>
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
    </div>
  );
}

export default App
