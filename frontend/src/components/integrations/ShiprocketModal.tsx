'use client';

import { useState } from 'react';
import ConnectionModal, { ConnectionField } from './ConnectionModal';
import { courierService } from '@/services/courier.service';
import { toast } from 'sonner';

interface ShiprocketModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

export default function ShiprocketModal({
  isOpen,
  onClose,
  onSuccess,
}: ShiprocketModalProps) {
  const [isConnecting, setIsConnecting] = useState(false);

  const fields: ConnectionField[] = [
    {
      name: 'name',
      label: 'Account Name',
      type: 'text',
      placeholder: 'e.g., Shiprocket - Main Account',
      required: true,
      helpText: 'A friendly name to identify this account',
    },
    {
      name: 'email',
      label: 'Shiprocket Email',
      type: 'email',
      placeholder: 'your-email@example.com',
      required: true,
      helpText: 'Your Shiprocket account email',
    },
    {
      name: 'password',
      label: 'Shiprocket Password',
      type: 'password',
      placeholder: '••••••••',
      required: true,
      helpText: 'Your Shiprocket account password',
    },
    {
      name: 'channelId',
      label: 'Channel ID',
      type: 'text',
      placeholder: 'Optional: Your Shiprocket channel ID',
      required: false,
      helpText: 'Leave empty to use default channel',
    },
    {
      name: 'pickupLocation',
      label: 'Default Pickup Location',
      type: 'text',
      placeholder: 'Optional: Pickup location name',
      required: false,
      helpText: 'Your default warehouse/pickup location',
    },
    {
      name: 'isDefault',
      label: 'Set as Default',
      type: 'select',
      required: true,
      options: [
        { value: 'true', label: 'Yes - Use as default courier' },
        { value: 'false', label: 'No - Add as alternative option' },
      ],
      helpText: 'Default couriers are automatically selected for new shipments',
    },
  ];

  const handleConnect = async (data: Record<string, string>) => {
    setIsConnecting(true);

    try {
      await courierService.createCourierAccount({
        name: data.name,
        courierType: 'Shiprocket',
        apiKey: data.email,
        apiSecret: data.password,
        channelId: data.channelId || undefined,
        isDefault: data.isDefault === 'true',
        priority: 100,
      });

      toast.success('Shiprocket connected successfully!');
      onClose();
      onSuccess?.();
    } catch (error: any) {
      const errorMessage = error?.response?.data?.message || 'Failed to connect Shiprocket account';
      toast.error(errorMessage);
      throw error;
    } finally {
      setIsConnecting(false);
    }
  };

  return (
    <ConnectionModal
      isOpen={isOpen}
      onClose={onClose}
      onConnect={handleConnect}
      title="Connect Shiprocket"
      description="Enter your Shiprocket account credentials to start shipping orders."
      fields={fields}
      isConnecting={isConnecting}
    />
  );
}
