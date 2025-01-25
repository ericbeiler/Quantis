import { useState } from "react";
import { Layout, Responsive, WidthProvider } from "react-grid-layout";
import "react-grid-layout/css/styles.css";
import "react-resizable/css/styles.css";

import PredictionGrid from "./components/PredictionGrid";
import ModelSelector from "./components/ModelSelector";
import ConfigureModelModal from "./components/ConfigureModelModal";
import ModelDetails from "./components/ModelDetails";

const serverUrl = import.meta.env.VITE_SERVER;

function App() {
  const ResponsiveGridLayout = WidthProvider(Responsive);
  const [layout, setLayout] = useState<Layout[]>([
    { i: "selector", x: 0, y: 0, w: 2, h: 4, isDraggable: false, minH: 4, minW: 1, maxW: 4, resizeHandles: ["e"] }, // Selector spans 2 columns, 1 row
    { i: "grid", x: 2, y: 0, w: 7, h: 4, isDraggable: false, minH: 4, minW: 2, maxW: 10, resizeHandles: ["e", "s"] },     // Grid spans 7 columns, 1 row
    { i: "details", x: 9, y: 0, w: 3, h: 4, isDraggable: false, minW: 1, maxW: 4, isResizable: false, minH: 4 },  // Details spans 3 columns, 1 row
  ]);

  const [selectedModel, setSelectedModel] = useState(0);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const handleLayoutChange = (newLayout: Layout[]) => {
    console.log("New Layout:", newLayout);
    // Example: Ensure "details" always fits the remaining space
    const totalColumns = 12;

    // Find the "selector" and "grid" components' widths
    const selector = newLayout.find((item) => item.i === "selector");
    const grid = newLayout.find((item) => item.i === "grid");
    const details = newLayout.find((item) => item.i === "details");

    if (selector && grid && details) {
      // Calculate remaining space for "details"
      const usedColumns = selector.w + grid.w;
      details.w = totalColumns - usedColumns;
      details.x = usedColumns;
      details.y = 0

      // Ensure "details" does not overlap or shrink too small
      details.w = Math.max(details.w, details.minW || 1); // Respect minW
    }

    // Save the updated layout
    setLayout([...newLayout]);
  }

  return (
    <div className="flex h-screen flex-col">
      {/* Header */}
      <header className="relative flex items-center justify-between bg-blue-600 p-4 text-white shadow-md">
        <h1 className="text-xl font-bold">Quartis Predictions</h1>
      </header>

      {/* Grid Layout */}
      <div className="flex-1">
        <ResponsiveGridLayout
          className="layout"
          layouts={{ lg: layout }}
          breakpoints={{ lg: 1200, md: 996, sm: 768, xs: 480, xxs: 0 }}
          cols={{ lg: 12, md: 10, sm: 8, xs: 4, xxs: 1 }}
          rowHeight={200}
          compactType="horizontal" // Force horizontal alignment
          onLayoutChange={(newLayout: Layout[]) => handleLayoutChange(newLayout) }
        >
          <div key="selector" className="h-full w-full overflow-hidden bg-gray-100 p-4 shadow-md">
            <ModelSelector selectedModel={selectedModel} setSelectedModel={setSelectedModel} />
          </div>
          <div key="grid" className="h-full w-full overflow-hidden bg-white p-4 shadow-md">
            <PredictionGrid selectedModel={selectedModel} />
          </div>
          <div key="details" className="h-full w-full overflow-hidden bg-gray-100 p-4 shadow-md">
            <ModelDetails selectedModel={selectedModel} />
          </div>
        </ResponsiveGridLayout>
      </div>

      {/* Footer */}
      <footer className="bg-gray-800 p-4 text-center text-sm text-gray-400">
        <p>Visavi Software, (c) 2024</p>
      </footer>
    </div>
  );
}

export default App;
