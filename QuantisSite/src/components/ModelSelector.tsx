﻿import { useState, useEffect } from "react";
import ModelSummary from "./ModelSummary";
import ConfigureModelModal from "./ConfigureModelModal";
import ModelConfiguration from "./ModelConfiguration";
import ModelSelectorProps from "./ModelSelectorProps";
import ModelState from "./ModelState";
import * as signalR from "@microsoft/signalr";

const serverUrl = import.meta.env.VITE_SERVER;

const ModelSelector: React.FC<ModelSelectorProps> = ({ selectedModel, setSelectedModel }) => {
  const [modelList, setModelList] = useState<ModelSummary[]>([]);
  const [editingModel, setEditingModel] = useState<number | null>(null);
  const [editName, setEditName] = useState("");
  const [editDescription, setEditDescription] = useState("");
  const [isModalOpen, setIsModalOpen] = useState(false); // State for modal visibility

  useEffect(() => {
    const fetchModels = async () => {
      try {
        const response = await fetch(`${serverUrl}api/Model`);
        const models = await response.json();
        setModelList(models);
      } catch (error) {
        console.error("Error fetching models:", error);
      }
    };

    fetchModels();

    // Set up SignalR connection
    const negotiateUrl = serverUrl + 'api';
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(negotiateUrl)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Debug)
      .build();

    // Start the SignalR connection
    const startConnection = async () => {
      try {
        await connection.start();
        console.log("SignalR Connected");

        // Subscribe to the "modelUpdated" event
        connection.on("modelUpdated", (updatedModel: string) => {
          console.log("Received model update:", updatedModel);

          fetchModels();
        });
      } catch (error: any) {
        console.error("Error starting SignalR connection:", error);
      }
    };

    startConnection();

    // Clean up the connection when the component unmounts
    return () => {
      connection.stop().catch((error) => console.error("Error stopping SignalR connection:", error));
    };
  }, []);

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

  const handleDelete = async (id: number) => {
    try {
      await fetch(`${serverUrl}api/Model/${id}`, {
        method: "DELETE",
      });
      setModelList(modelList.filter((model) => model.Id !== id));
    } catch (error) {
      console.error("Error deleting model:", error);
    }
  };

  const handleEditSave = async (id: number) => {
    try {
      await fetch(`${serverUrl}api/Model/${id}`, {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ Name: editName, Description: editDescription }),
      });
      setModelList(
        modelList.map((model) =>
          model.Id === id ? { ...model, Name: editName, Description: editDescription } : model
        )
      );
      setEditingModel(null);
    } catch (error) {
      console.error("Error updating model:", error);
    }
  };

  return (
    <div className="resizable mx-auto h-full max-w-lg space-y-4 overflow-y-auto rounded-xl bg-white p-6 shadow-md">

      {/* Models Title with Add Button */}
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-bold text-gray-900">Models</h2>
        <button
          className="rounded-full bg-blue-300 p-2 text-white shadow-md hover:bg-blue-700"
          onClick={() => setIsModalOpen(true)}
          title="Add New Model"
        >
          ➕
        </button>
      </div>


      {/* List of Models */}
      <ul className="space-y-2">
        {modelList.map((model) => (
          <li
            key={model.Id}
            className={`p-4 rounded-lg ${selectedModel === model.Id ? "bg-blue-100" : "bg-gray-100"
              } hover:bg-blue-200 flex flex-col justify-between`}
          >
            {editingModel === model.Id ? (
              <div className="space-y-2">
                <input
                  type="text"
                  className="w-full rounded border p-2"
                  placeholder="New Name"
                  value={editName}
                  onChange={(e) => setEditName(e.target.value)}
                />
                <input
                  type="text"
                  className="w-full rounded border p-2"
                  placeholder="New Description"
                  value={editDescription}
                  onChange={(e) => setEditDescription(e.target.value)}
                />
                <div className="flex justify-end space-x-2">
                  <button
                    className="rounded bg-green-500 px-4 py-2 text-white hover:bg-green-600"
                    onClick={() => handleEditSave(model.Id)}
                  >
                    Save
                  </button>
                  <button
                    className="rounded bg-gray-500 px-4 py-2 text-white hover:bg-gray-600"
                    onClick={() => setEditingModel(null)}
                  >
                    Cancel
                  </button>
                </div>
              </div>
            ) : (
              <div className="flex flex-col space-y-2">
                <div
                  className="cursor-pointer"
                  onClick={() => setSelectedModel(model.Id)}
                  title={`Model: ${model.Name}`}
                >
                  {model.Name}
                  <ul>
                      <li><strong>State:</strong> {ModelState[model.State]}</li>
                    <li><strong>Score:</strong> {model.QualityScore}</li>
                    <li><strong>Created:</strong> {model.Created}</li>
                  </ul>
                </div>
                <div className="mt-auto flex justify-end space-x-2">
                  <button
                    className="text-blue-500 hover:text-blue-700"
                    onClick={() => {
                      setEditingModel(model.Id);
                      setEditName(model.Name);
                      setEditDescription(model.Description || "");
                    }}
                    title="Edit"
                  >
                    ✏️
                  </button>
                  <button
                    className="text-blue-500 hover:text-blue-700"
                    onClick={() => handleDelete(model.Id)}
                    title="Delete"
                  >
                    🗑️
                  </button>
                </div>
              </div>
            )}
          </li>
        ))}
      </ul>

      {/* Configure Model Modal */}
      <ConfigureModelModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSubmit={handleBuildModel}
      />
    </div>
  );
};

export default ModelSelector;
