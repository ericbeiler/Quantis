import { useState } from "react";
import './App.css'

import 'react-data-grid/lib/styles.css'
import PredictionGrid from './components/PredictionGrid'  
import ModelSelector from './components/ModelSelector'


function App() {
  const [selectedModel, setSelectedModel] = useState(7);

  return (
    <div className="container mx-auto p-4">
      <header className="mb-4 rounded-md bg-blue-600 px-6 py-4 text-black">
        <h1 className="text-3xl font-bold">Quartis Predictions</h1>
      </header>
      <nav className="menu mb-4 rounded-md bg-gray-100 px-6 py-4">
        <ModelSelector
          selectedModel={selectedModel}
          setSelectedModel={setSelectedModel}
        />
      </nav>
      <main className="content mb-4 rounded-md bg-white px-6 py-4 shadow-md">
        <PredictionGrid
          selectedModel={selectedModel}
          setSelectedModel={setSelectedModel}
        />
      </main>
      <footer className="footer mt-4 text-center text-sm text-gray-500">
        <p>Visavi Software, (c) 2024</p>
      </footer>
    </div>
  );
}

export default App
