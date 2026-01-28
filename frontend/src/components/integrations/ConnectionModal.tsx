'use client';

import { useState } from 'react';
import { X } from 'lucide-react';
import { Button } from '@/components/ui/button';

export interface ConnectionField {
  name: string;
  label: string;
  type: 'text' | 'password' | 'email' | 'url' | 'select';
  placeholder?: string;
  required?: boolean;
  helpText?: string;
  options?: { value: string; label: string }[];
}

interface ConnectionModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConnect: (data: Record<string, string>) => Promise<void>;
  title: string;
  description?: string;
  fields: ConnectionField[];
  isConnecting?: boolean;
}

export default function ConnectionModal({
  isOpen,
  onClose,
  onConnect,
  title,
  description,
  fields,
  isConnecting = false,
}: ConnectionModalProps) {
  const [formData, setFormData] = useState<Record<string, string>>({});
  const [errors, setErrors] = useState<Record<string, string>>({});

  if (!isOpen) return null;

  const handleInputChange = (name: string, value: string) => {
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (errors[name]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[name];
        return newErrors;
      });
    }
  };

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    fields.forEach((field) => {
      if (field.required && !formData[field.name]?.trim()) {
        newErrors[field.name] = `${field.label} is required`;
      }
    });

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    try {
      await onConnect(formData);
      setFormData({});
      setErrors({});
    } catch (error) {
      console.error('Connection error:', error);
    }
  };

  const handleClose = () => {
    setFormData({});
    setErrors({});
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="relative w-full max-w-md max-h-[90vh] flex flex-col rounded-lg bg-white shadow-xl">
        {/* Header */}
        <div className="flex-shrink-0 border-b border-gray-200 px-6 py-4">
          <button
            type="button"
            onClick={handleClose}
            className="absolute right-4 top-4 text-gray-400 hover:text-gray-600"
            disabled={isConnecting}
            aria-label="Close modal"
          >
            <X className="h-5 w-5" />
          </button>

          <h2 className="text-xl font-semibold text-gray-900 pr-8">{title}</h2>
          {description && (
            <p className="mt-1 text-sm text-gray-600">{description}</p>
          )}
        </div>

        {/* Scrollable Content */}
        <div className="flex-1 overflow-y-auto px-6 py-4">
          <form onSubmit={handleSubmit} className="space-y-4" id="connection-form">
            {fields.map((field) => (
              <div key={field.name}>
                <label
                  htmlFor={field.name}
                  className="block text-sm font-medium text-gray-700 mb-1"
                >
                  {field.label}
                  {field.required && <span className="text-red-500"> *</span>}
                </label>

                {field.type === 'select' ? (
                  <select
                    id={field.name}
                    value={formData[field.name] || ''}
                    onChange={(e) => handleInputChange(field.name, e.target.value)}
                    className={`block w-full rounded-md border px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 ${
                      errors[field.name] ? 'border-red-500' : 'border-gray-300'
                    }`}
                    disabled={isConnecting}
                  >
                    <option value="">Select {field.label}</option>
                    {field.options?.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                ) : (
                  <input
                    id={field.name}
                    type={field.type}
                    value={formData[field.name] || ''}
                    onChange={(e) => handleInputChange(field.name, e.target.value)}
                    placeholder={field.placeholder}
                    className={`block w-full rounded-md border px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500 ${
                      errors[field.name] ? 'border-red-500' : 'border-gray-300'
                    }`}
                    disabled={isConnecting}
                  />
                )}

                {field.helpText && (
                  <p className="mt-1.5 text-xs text-gray-500">{field.helpText}</p>
                )}

                {errors[field.name] && (
                  <p className="mt-1.5 text-xs text-red-500">{errors[field.name]}</p>
                )}
              </div>
            ))}
          </form>
        </div>

        {/* Footer */}
        <div className="flex-shrink-0 border-t border-gray-200 px-6 py-4">
          <div className="flex gap-3">
            <Button
              type="button"
              variant="outline"
              onClick={handleClose}
              disabled={isConnecting}
              className="flex-1"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              form="connection-form"
              disabled={isConnecting}
              className="flex-1"
            >
              {isConnecting ? 'Connecting...' : 'Connect'}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
