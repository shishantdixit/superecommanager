'use client';

import { useState, useEffect } from 'react';
import { Modal, Button, Input, Select } from '@/components/ui';
import { useCreateShipment } from '@/hooks';
import { Loader2, Package, TruckIcon, AlertCircle } from 'lucide-react';
import { courierService, type CourierAccountDto } from '@/services/courier.service';
import { toast } from 'sonner';

interface CreateShipmentModalProps {
  isOpen: boolean;
  onClose: () => void;
  orderId: string;
  orderNumber: string;
}

export function CreateShipmentModal({
  isOpen,
  onClose,
  orderId,
  orderNumber,
}: CreateShipmentModalProps) {
  const createShipment = useCreateShipment();
  const [courierAccounts, setCourierAccounts] = useState<CourierAccountDto[]>([]);
  const [loadingCouriers, setLoadingCouriers] = useState(false);
  const [formData, setFormData] = useState({
    courierCode: '',
    weight: '',
    length: '',
    width: '',
    height: '',
    pickupDate: '',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Fetch active courier accounts when modal opens
  useEffect(() => {
    if (isOpen) {
      loadCourierAccounts();
    }
  }, [isOpen]);

  const loadCourierAccounts = async () => {
    setLoadingCouriers(true);
    try {
      const accounts = await courierService.getActiveCourierAccounts();
      setCourierAccounts(accounts);

      // Set default courier if available
      if (accounts.length > 0) {
        const defaultAccount = accounts.find(a => a.isDefault) || accounts[0];
        setFormData(prev => ({ ...prev, courierCode: defaultAccount.id }));
      }
    } catch (error) {
      console.error('Failed to load courier accounts:', error);
      toast.error('Failed to load courier accounts');
    } finally {
      setLoadingCouriers(false);
    }
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.courierCode) {
      newErrors.courierCode = 'Courier is required';
    }

    if (!formData.weight || parseFloat(formData.weight) <= 0) {
      newErrors.weight = 'Weight must be greater than 0';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    try {
      const result = await createShipment.mutateAsync({
        orderId,
        courierCode: formData.courierCode,
        weight: parseFloat(formData.weight),
        length: formData.length ? parseFloat(formData.length) : undefined,
        width: formData.width ? parseFloat(formData.width) : undefined,
        height: formData.height ? parseFloat(formData.height) : undefined,
        pickupDate: formData.pickupDate || undefined,
      });

      toast.success('Shipment created successfully', {
        description: result?.awbNumber
          ? `AWB: ${result.awbNumber}`
          : 'Your shipment has been created and will be processed shortly.',
      });

      // Reset form and close modal
      setFormData({
        courierCode: '',
        weight: '',
        length: '',
        width: '',
        height: '',
        pickupDate: '',
      });
      setErrors({});
      onClose();
    } catch (error: any) {
      console.error('Failed to create shipment:', error);

      // Extract error message from various possible error structures
      let errorMessage = 'Failed to create shipment. Please try again.';

      if (error?.response?.data) {
        const responseData = error.response.data;

        // Check for ApiResponse structure with message field
        if (responseData.message) {
          errorMessage = responseData.message;
        }
        // Check for errors array
        else if (responseData.errors && Array.isArray(responseData.errors)) {
          errorMessage = responseData.errors.join(', ');
        }
        // Check for plain error message
        else if (typeof responseData === 'string') {
          errorMessage = responseData;
        }
      } else if (error?.message) {
        errorMessage = error.message;
      }

      toast.error('Shipment creation failed', {
        description: errorMessage,
      });
    }
  };

  const handleChange = (field: string, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    // Clear error for this field
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Create Shipment"
      size="lg"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="mb-4 flex items-center gap-2 rounded-lg bg-blue-50 p-3 text-sm text-blue-700 dark:bg-blue-900/20 dark:text-blue-300">
          <Package className="h-5 w-5" />
          <span>
            Creating shipment for order <strong>{orderNumber}</strong>
          </span>
        </div>

        {/* Courier Selection */}
        <div>
          <label className="mb-1 block text-sm font-medium">
            Courier <span className="text-error">*</span>
          </label>
          {loadingCouriers ? (
            <div className="flex items-center gap-2 rounded-md border border-gray-300 bg-gray-50 px-3 py-2 text-sm text-gray-500">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span>Loading courier accounts...</span>
            </div>
          ) : courierAccounts.length === 0 ? (
            <div className="flex items-center gap-2 rounded-md border border-amber-300 bg-amber-50 px-3 py-2 text-sm text-amber-700">
              <AlertCircle className="h-4 w-4" />
              <span>No active courier accounts found. Please add and connect a courier account first.</span>
            </div>
          ) : (
            <Select
              options={courierAccounts.map(account => ({
                value: account.id,
                label: `${account.name} (${account.courierTypeName})`
              }))}
              value={formData.courierCode}
              onChange={(e) => handleChange('courierCode', e.target.value)}
              disabled={loadingCouriers}
            />
          )}
          {errors.courierCode && (
            <p className="mt-1 text-sm text-error">{errors.courierCode}</p>
          )}
        </div>

        {/* Weight */}
        <div>
          <label className="mb-1 block text-sm font-medium">
            Weight (kg) <span className="text-error">*</span>
          </label>
          <Input
            type="number"
            step="0.01"
            min="0"
            value={formData.weight}
            onChange={(e) => handleChange('weight', e.target.value)}
            placeholder="Enter package weight"
          />
          {errors.weight && (
            <p className="mt-1 text-sm text-error">{errors.weight}</p>
          )}
        </div>

        {/* Dimensions */}
        <div className="grid grid-cols-3 gap-3">
          <div>
            <label className="mb-1 block text-sm font-medium">Length (cm)</label>
            <Input
              type="number"
              step="0.1"
              min="0"
              value={formData.length}
              onChange={(e) => handleChange('length', e.target.value)}
              placeholder="Length"
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Width (cm)</label>
            <Input
              type="number"
              step="0.1"
              min="0"
              value={formData.width}
              onChange={(e) => handleChange('width', e.target.value)}
              placeholder="Width"
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium">Height (cm)</label>
            <Input
              type="number"
              step="0.1"
              min="0"
              value={formData.height}
              onChange={(e) => handleChange('height', e.target.value)}
              placeholder="Height"
            />
          </div>
        </div>

        {/* Pickup Date */}
        <div>
          <label className="mb-1 block text-sm font-medium">
            Pickup Date (Optional)
          </label>
          <Input
            type="date"
            value={formData.pickupDate}
            onChange={(e) => handleChange('pickupDate', e.target.value)}
            min={new Date().toISOString().split('T')[0]}
          />
          <p className="mt-1 text-xs text-muted-foreground">
            Leave empty to schedule pickup for today
          </p>
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-3 pt-4">
          <Button
            type="button"
            variant="outline"
            onClick={onClose}
            disabled={createShipment.isPending}
          >
            Cancel
          </Button>
          <Button
            type="submit"
            disabled={createShipment.isPending || loadingCouriers || courierAccounts.length === 0}
            leftIcon={
              createShipment.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <TruckIcon className="h-4 w-4" />
              )
            }
          >
            {createShipment.isPending ? 'Creating...' : 'Create Shipment'}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
