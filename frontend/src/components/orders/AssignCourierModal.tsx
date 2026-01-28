'use client';

import { useState, useEffect } from 'react';
import { Modal, Button, Badge } from '@/components/ui';
import { Loader2, Truck, AlertCircle, Star, Check } from 'lucide-react';
import { shipmentsService, type AvailableCourier } from '@/services/shipments.service';
import { toast } from 'sonner';
import { useQueryClient } from '@tanstack/react-query';

interface AssignCourierModalProps {
  isOpen: boolean;
  onClose: () => void;
  shipmentId: string;
  shipmentNumber: string;
  onSuccess?: () => void;
}

export function AssignCourierModal({
  isOpen,
  onClose,
  shipmentId,
  shipmentNumber,
  onSuccess,
}: AssignCourierModalProps) {
  const queryClient = useQueryClient();
  const [availableCouriers, setAvailableCouriers] = useState<AvailableCourier[]>([]);
  const [loading, setLoading] = useState(false);
  const [assigning, setAssigning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedCourierId, setSelectedCourierId] = useState<number | null>(null);

  // Fetch available couriers when modal opens
  useEffect(() => {
    if (isOpen && shipmentId) {
      loadAvailableCouriers();
    }
  }, [isOpen, shipmentId]);

  const loadAvailableCouriers = async () => {
    setLoading(true);
    setError(null);
    try {
      // Service returns the data array directly (already unwrapped from ApiResponse)
      const couriers = await shipmentsService.getAvailableCouriers(shipmentId);
      setAvailableCouriers(couriers || []);

      // Auto-select recommended courier
      const recommended = couriers?.find(c => c.isRecommended);
      if (recommended) {
        setSelectedCourierId(recommended.courierId);
      }
    } catch (err: any) {
      console.error('Failed to load available couriers:', err);
      const errorMessage = err?.response?.data?.message
        || err?.response?.data?.errors?.join(', ')
        || err?.message
        || 'Failed to load available couriers';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleAssignCourier = async () => {
    if (!selectedCourierId) {
      toast.error('Please select a courier');
      return;
    }

    setAssigning(true);
    try {
      // Service returns the Shipment directly (already unwrapped from ApiResponse)
      const result = await shipmentsService.assignCourier(shipmentId, selectedCourierId);

      toast.success('Courier assigned successfully', {
        description: `AWB: ${result?.awbNumber}`,
      });

      // Invalidate queries to refresh data
      queryClient.invalidateQueries({ queryKey: ['shipments'] });
      queryClient.invalidateQueries({ queryKey: ['shipment', shipmentId] });

      onSuccess?.();
      onClose();
    } catch (err: any) {
      console.error('Failed to assign courier:', err);
      const errorMessage = err?.response?.data?.message
        || err?.response?.data?.errors?.join(', ')
        || err?.message
        || 'Failed to assign courier';
      toast.error('Failed to assign courier', { description: errorMessage });
    } finally {
      setAssigning(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
    }).format(amount);
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Assign Courier"
      size="lg"
    >
      <div className="space-y-4">
        <div className="mb-4 flex items-center gap-2 rounded-lg bg-amber-50 p-3 text-sm text-amber-700 dark:bg-amber-900/20 dark:text-amber-300">
          <Truck className="h-5 w-5" />
          <span>
            Assign courier for shipment <strong>{shipmentNumber}</strong>
          </span>
        </div>

        {loading ? (
          <div className="flex flex-col items-center justify-center py-12">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
            <p className="mt-4 text-sm text-muted-foreground">
              Checking courier availability...
            </p>
          </div>
        ) : error ? (
          <div className="flex flex-col items-center justify-center py-8">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-error/10">
              <AlertCircle className="h-6 w-6 text-error" />
            </div>
            <p className="mt-4 text-center text-sm text-error">{error}</p>
            <Button
              variant="outline"
              size="sm"
              className="mt-4"
              onClick={loadAvailableCouriers}
            >
              Try Again
            </Button>
          </div>
        ) : availableCouriers.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-8">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-amber-100">
              <AlertCircle className="h-6 w-6 text-amber-600" />
            </div>
            <p className="mt-4 text-center text-sm text-muted-foreground">
              No couriers available for this delivery route.
            </p>
            <p className="mt-2 text-center text-xs text-muted-foreground">
              The destination pincode may not be serviceable.
            </p>
          </div>
        ) : (
          <>
            <p className="text-sm text-muted-foreground">
              Select a courier from the available options below:
            </p>

            <div className="max-h-[400px] overflow-y-auto space-y-2">
              {availableCouriers.map((courier) => (
                <div
                  key={courier.courierId}
                  className={`flex items-center justify-between rounded-lg border p-4 cursor-pointer transition-colors ${
                    selectedCourierId === courier.courierId
                      ? 'border-primary bg-primary/5'
                      : 'hover:border-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800'
                  }`}
                  onClick={() => setSelectedCourierId(courier.courierId)}
                >
                  <div className="flex items-center gap-3">
                    <div className={`flex h-5 w-5 items-center justify-center rounded-full border ${
                      selectedCourierId === courier.courierId
                        ? 'border-primary bg-primary text-white'
                        : 'border-gray-300'
                    }`}>
                      {selectedCourierId === courier.courierId && (
                        <Check className="h-3 w-3" />
                      )}
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{courier.courierName}</span>
                        {courier.isRecommended && (
                          <Badge variant="success" className="text-xs">
                            <Star className="mr-1 h-3 w-3" />
                            Recommended
                          </Badge>
                        )}
                        {courier.isSurface && (
                          <Badge variant="default" className="text-xs">Surface</Badge>
                        )}
                      </div>
                      <div className="mt-1 flex items-center gap-4 text-xs text-muted-foreground">
                        <span>ETA: {courier.estimatedDeliveryDays}</span>
                        {courier.rating > 0 && (
                          <span className="flex items-center gap-1">
                            <Star className="h-3 w-3 text-yellow-500" />
                            {courier.rating.toFixed(1)}
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="font-semibold text-primary">
                      {formatCurrency(courier.totalCharge)}
                    </p>
                    <div className="text-xs text-muted-foreground">
                      <span>Freight: {formatCurrency(courier.freightCharge)}</span>
                      {courier.codCharges > 0 && (
                        <span className="ml-2">+ COD: {formatCurrency(courier.codCharges)}</span>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </>
        )}

        {/* Actions */}
        <div className="flex justify-end gap-3 pt-4 border-t">
          <Button
            type="button"
            variant="outline"
            onClick={onClose}
            disabled={assigning}
          >
            Cancel
          </Button>
          <Button
            onClick={handleAssignCourier}
            disabled={assigning || loading || !selectedCourierId || availableCouriers.length === 0}
            leftIcon={
              assigning ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Truck className="h-4 w-4" />
              )
            }
          >
            {assigning ? 'Assigning...' : 'Assign Courier'}
          </Button>
        </div>
      </div>
    </Modal>
  );
}
