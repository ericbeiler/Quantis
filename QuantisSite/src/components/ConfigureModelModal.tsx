import { Dialog, Transition } from "@headlessui/react";
import React, { Fragment, useState } from "react";
import TrainingGranularity from "./TrainingGranularity";
import ModelConfiguration from "./ModelConfiguration";

interface ConfigureModelModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (parameters: ModelConfiguration) => void;
}

const ConfigureModelModal: React.FC<ConfigureModelModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
}) => {
  const [parameters, setParameters] = useState<ModelConfiguration>({
    NumberOfTrees: 100,
    NumberOfLeaves: 20,
    MinimumExampleCountPerLeaf: 10,
    Granularity: TrainingGranularity.Monthly,
    Features: [],
  });

  const [featureList, setFeatureList] = useState<string[]>([]);

  React.useEffect(() => {
    const fetchFeatureList = async () => {
      try {
        const response = await fetch(
          `${import.meta.env.VITE_SERVER}api/FeatureList`
        );
        if (!response.ok) {
          throw new Error("Failed to fetch current feature list");
        }
        const data: string[] = await response.json();
        setFeatureList(data);
      } catch (error) {
        console.error("Error fetching feature list:", error);
      }
    };
    fetchFeatureList();
  }, []);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setParameters((prev) => ({
      ...prev,
      [name]: parseInt(value, 10),
    }));
  };

  const handleFeatureToggle = (feature: string) => {
    setParameters((prev) => ({
      ...prev,
      Features: prev.Features.includes(feature)
        ? prev.Features.filter((f) => f !== feature)
        : [...prev.Features, feature],
    }));
  };

  const handleSubmit = () => {
    onSubmit(parameters);
    onClose();
  };

  return (
    <Transition appear show={isOpen} as={Fragment}>
      <Dialog as="div" className="relative z-50" onClose={onClose}>
        <Transition.Child
          as={Fragment}
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black bg-opacity-50" />
        </Transition.Child>

        <div className="fixed inset-0 overflow-y-auto">
          <div className="flex min-h-full items-center justify-center p-4">
            <Transition.Child
              as={Fragment}
              enter="ease-out duration-300"
              enterFrom="opacity-0 scale-95"
              enterTo="opacity-100 scale-100"
              leave="ease-in duration-200"
              leaveFrom="opacity-100 scale-100"
              leaveTo="opacity-0 scale-95"
            >
              <Dialog.Panel className="w-full max-w-4xl transform overflow-hidden rounded-lg bg-white p-6 text-left shadow-xl transition-all">
                {/* Title */}
                <Dialog.Title
                  as="h3"
                  className="text-xl font-bold text-gray-900"
                >
                  Configure Model
                </Dialog.Title>

                {/* Input Fields */}
                <div className="mt-6 space-y-6">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Number of Trees
                    </label>
                    <input
                      type="number"
                      name="NumberOfTrees"
                      value={parameters.NumberOfTrees}
                      onChange={handleInputChange}
                      className="mt-1 w-full rounded border-gray-300 shadow-sm focus:border-blue-500 focus:ring focus:ring-blue-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Number of Leaves
                    </label>
                    <input
                      type="number"
                      name="NumberOfLeaves"
                      value={parameters.NumberOfLeaves}
                      onChange={handleInputChange}
                      className="mt-1 w-full rounded border-gray-300 shadow-sm focus:border-blue-500 focus:ring focus:ring-blue-500"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">
                      Minimum Example Count Per Leaf
                    </label>
                    <input
                      type="number"
                      name="MinimumExampleCountPerLeaf"
                      value={parameters.MinimumExampleCountPerLeaf}
                      onChange={handleInputChange}
                      className="mt-1 w-full rounded border-gray-300 shadow-sm focus:border-blue-500 focus:ring focus:ring-blue-500"
                    />
                  </div>
                </div>

                {/* Features in Two Columns */}
                <div className="mt-6">
                  <h4 className="mb-4 text-lg font-semibold text-gray-700">
                    Features
                  </h4>
                  <div className="grid-cols-2 grid gap-4">
                    {featureList.map((feature) => (
                      <div key={feature} className="flex items-center">
                        <input
                          type="checkbox"
                          id={feature}
                          value={feature}
                          checked={parameters.Features.includes(feature)}
                          onChange={() => handleFeatureToggle(feature)}
                          className="mr-2 rounded border-gray-300 text-blue-600 shadow-sm focus:ring focus:ring-blue-500"
                        />
                        <label
                          htmlFor={feature}
                          className="text-sm font-medium text-gray-700"
                        >
                          {feature}
                        </label>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Footer Buttons */}
                <div className="mt-8 flex justify-end space-x-4">
                  <button
                    className="rounded bg-gray-200 px-4 py-2 text-gray-700 shadow hover:bg-gray-300"
                    onClick={onClose}
                  >
                    Cancel
                  </button>
                  <button
                    className="rounded bg-blue-600 px-4 py-2 text-white shadow hover:bg-blue-700"
                    onClick={handleSubmit}
                  >
                    Build Model
                  </button>
                </div>
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition>
  );
};

export default ConfigureModelModal;
