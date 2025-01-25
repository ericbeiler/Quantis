import React from "react";

type ModalProps = {
  isOpen: boolean;
  onClose: () => void;
  children: React.ReactNode;
};

const Modal: React.FC<ModalProps> = ({ isOpen, onClose, children }) => {
  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50"
      onClick={onClose} // Close modal when clicking on the background
    >
      <div
        className="w-1/2 rounded-lg bg-white p-6 shadow-lg"
        onClick={(e) => e.stopPropagation()} // Prevent closing when clicking inside the modal
      >
        <button
          className="absolute right-2 top-2 text-gray-400 hover:text-gray-600"
          onClick={onClose}
        >
          ?
        </button>
        {children}
      </div>
    </div>
  );
};

export default Modal;
