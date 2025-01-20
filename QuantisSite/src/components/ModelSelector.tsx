import { useState, useEffect } from "react";
import ModelSummary from "./ModelSummary";
import ModelSelectorProps from "./ModelSelectorProps";
import * as signalR from "@microsoft/signalr";

const serverUrl = import.meta.env.VITE_SERVER;

const ModelSelector: React.FC<ModelSelectorProps> = ({ selectedModel, setSelectedModel }) => {
  const [modelList, setModelList] = useState<ModelSummary[]>([]);
  const [editingModel, setEditingModel] = useState<number | null>(null);
  const [editName, setEditName] = useState("");
  const [editDescription, setEditDescription] = useState("");

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
        connection.on("modelUpdated", (updatedModel) => {
          console.log("Received model update:", updatedModel);

          // Update the modelList state
          setModelList((prevModels) =>
            prevModels.map((model) =>
              model.Id === updatedModel.Id ? { ...model, ...updatedModel } : model
            )
          );
        });

        // Subscribe to the "modelAdded" event
        connection.on("modelAdded", (newModel) => {
          console.log("Received new model:", newModel);

          // Add the new model to the modelList state
          setModelList((prevModels) => [...prevModels, newModel]);
        });

        // Subscribe to the "modelDeleted" event
        connection.on("modelDeleted", (deletedModelId) => {
          console.log("Received model delete:", deletedModelId);

          // Remove the deleted model from the modelList state
          setModelList((prevModels) =>
            prevModels.filter((model) => model.Id !== deletedModelId)
          );
        });
      } catch (error) {
        console.error("Error starting SignalR connection:", error);
      }
    };

    startConnection();

    // Clean up the connection when the component unmounts
    return () => {
      connection.stop().catch((error) => console.error("Error stopping SignalR connection:", error));
    };
  }, []);

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
    <div className="resizable mx-auto max-w-lg space-y-4 rounded-xl bg-white p-6 shadow-md">
      <h2 className="text-xl font-bold text-gray-900">Models</h2>
      <ul className="space-y-2">
        {modelList.map((model) => (
          <li
            key={model.Id}
            className={`p-2 rounded-lg ${selectedModel === model.Id ? "bg-blue-100" : "bg-gray-100"
              } hover:bg-blue-200`}
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
            ) : (
              <div className="flex items-center justify-between">
                <div
                  className="cursor-pointer"
                  onClick={() => setSelectedModel(model.Id)}
                  title={`Model: ${model.Name}`}
                >
                  {model.Name}
                </div>
                <div className="flex space-x-2">
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
    </div>
  );
};

export default ModelSelector;
