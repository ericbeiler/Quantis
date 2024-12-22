import { useState } from "react";
import './App.css'

import 'react-data-grid/lib/styles.css'
import PredictionGrid from './components/PredictionGrid'  
import ModelSelector from './components/ModelSelector'


function App() {
  const [selectedModel, setSelectedModel] = useState(7);

  return (
    <div className="container" >
      <div className="header">
        <h1>Quartis Predictions</h1>
      </div>
      <div className="menu">
        <ModelSelector selectedModel={selectedModel} setSelectedModel={setSelectedModel} />
      </div>
      <div className="content">
        <PredictionGrid selectedModel={selectedModel} setSelectedModel={setSelectedModel} />
      </div>
      <div className="footer">
        <p>Visavi Software, (c) 2024</p>
      </div>
    </div>
  )
}

export default App
