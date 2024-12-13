import { useState, useEffect} from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'

import 'react-data-grid/lib/styles.css'

import PredictionGrid from './components/PredictionGrid2'  


function App() {

  return (
    <div style={{ width: "100%" }} >
      <h1>Quartis Predictions</h1>
      <PredictionGrid />
    </div>
  )
}

export default App
