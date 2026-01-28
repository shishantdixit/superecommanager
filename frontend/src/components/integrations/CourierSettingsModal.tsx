'use client';

import { useState, useEffect } from 'react';
import { X, CheckCircle, XCircle, Loader2, AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { courierService, CourierAccountDetailDto, ShiprocketChannel, ShiprocketPickupLocation } from '@/services/courier.service';
import { toast } from 'sonner';

interface CourierSettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
  accountId: string;
  courierName: string;
  onUpdate?: () => void;
}

export default function CourierSettingsModal({
  isOpen,
  onClose,
  accountId,
  courierName,
  onUpdate,
}: CourierSettingsModalProps) {
  const [account, setAccount] = useState<CourierAccountDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isTesting, setIsTesting] = useState(false);
  const [activeTab, setActiveTab] = useState<'credentials' | 'settings'>('credentials');
  const [channels, setChannels] = useState<ShiprocketChannel[]>([]);
  const [isLoadingChannels, setIsLoadingChannels] = useState(false);
  const [channelLoadError, setChannelLoadError] = useState<string | null>(null);
  const [pickupLocations, setPickupLocations] = useState<ShiprocketPickupLocation[]>([]);
  const [isLoadingPickupLocations, setIsLoadingPickupLocations] = useState(false);
  const [pickupLocationLoadError, setPickupLocationLoadError] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    name: '',
    apiKey: '',
    apiSecret: '',
    channelId: '',
    pickupLocation: '',
    isActive: true,
  });

  useEffect(() => {
    if (isOpen) {
      loadAccountDetails();
    }
  }, [isOpen, accountId]);

  const loadAccountDetails = async () => {
    try {
      setIsLoading(true);
      const details = await courierService.getCourierAccount(accountId);
      setAccount(details);
      setFormData({
        name: details.name,
        apiKey: '',
        apiSecret: '',
        channelId: details.channelId || '',
        pickupLocation: (details.settings?.pickupLocation as string) || '',
        isActive: details.isActive,
      });

      // Load channels and pickup locations if this is a Shiprocket account and it's connected
      if (details.courierType === 'Shiprocket' && details.isConnected) {
        loadChannels();
        loadPickupLocations();
      }
    } catch (error) {
      toast.error('Failed to load courier settings');
      console.error(error);
    } finally {
      setIsLoading(false);
    }
  };

  const loadChannels = async () => {
    try {
      setIsLoadingChannels(true);
      setChannelLoadError(null);
      const result = await courierService.getShiprocketChannels(accountId);
      setChannels(result);
    } catch (error: any) {
      const msg = error?.response?.data?.message || 'Failed to load channels';
      setChannelLoadError(msg);
      console.error('Failed to load channels:', error);
    } finally {
      setIsLoadingChannels(false);
    }
  };

  const loadPickupLocations = async () => {
    try {
      setIsLoadingPickupLocations(true);
      setPickupLocationLoadError(null);
      const result = await courierService.getShiprocketPickupLocations(accountId);
      setPickupLocations(result);
    } catch (error: any) {
      const msg = error?.response?.data?.message || 'Failed to load pickup locations';
      setPickupLocationLoadError(msg);
      console.error('Failed to load pickup locations:', error);
    } finally {
      setIsLoadingPickupLocations(false);
    }
  };

  const handleInputChange = (field: string, value: string | boolean) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const handleSave = async () => {
    try {
      setIsSaving(true);

      const updateData: any = {
        name: formData.name !== account?.name ? formData.name : undefined,
        channelId: formData.channelId || undefined,
        pickupLocation: formData.pickupLocation || undefined,
        isActive: formData.isActive,
      };

      // Only include credentials if they were changed
      if (formData.apiKey) {
        updateData.apiKey = formData.apiKey;
      }
      if (formData.apiSecret) {
        updateData.apiSecret = formData.apiSecret;
      }

      await courierService.updateCourierAccount(accountId, updateData);
      toast.success('Courier settings updated successfully');
      onUpdate?.();
      loadAccountDetails(); // Reload to get updated data
    } catch (error: any) {
      const errorMessage = error?.response?.data?.message || 'Failed to update settings';
      toast.error(errorMessage);
    } finally {
      setIsSaving(false);
    }
  };

  const handleTestConnection = async () => {
    try {
      setIsTesting(true);
      const result = await courierService.testConnection(accountId);

      if (result.isConnected) {
        toast.success('Connection test successful!');
        loadAccountDetails(); // Reload to update connection status
      } else {
        toast.error(`Connection failed: ${result.message || 'Unknown error'}`);
      }
    } catch (error: any) {
      const errorMessage = error?.response?.data?.message || 'Connection test failed';
      toast.error(errorMessage);
    } finally {
      setIsTesting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="relative w-full max-w-2xl rounded-lg bg-white shadow-xl max-h-[90vh] overflow-y-auto">
        <div className="sticky top-0 bg-white border-b px-6 py-4 flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-semibold text-gray-900">
              {courierName} Settings
            </h2>
            <p className="text-sm text-gray-600 mt-1">
              Configure your {courierName} account settings
            </p>
          </div>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600"
            disabled={isSaving}
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
          </div>
        ) : (
          <>
            {/* Connection Status Banner */}
            {account && (
              <div className={`mx-6 mt-4 p-4 rounded-lg ${
                account.isConnected
                  ? 'bg-green-50 border border-green-200'
                  : 'bg-red-50 border border-red-200'
              }`}>
                <div className="flex items-start gap-3">
                  {account.isConnected ? (
                    <CheckCircle className="h-5 w-5 text-green-600 mt-0.5" />
                  ) : (
                    <XCircle className="h-5 w-5 text-red-600 mt-0.5" />
                  )}
                  <div className="flex-1">
                    <p className={`font-medium ${
                      account.isConnected ? 'text-green-900' : 'text-red-900'
                    }`}>
                      {account.isConnected ? 'Connected' : 'Not Connected'}
                    </p>
                    {account.lastConnectedAt && (
                      <p className="text-sm text-gray-600 mt-1">
                        Last connected: {new Date(account.lastConnectedAt).toLocaleString()}
                      </p>
                    )}
                    {account.lastError && (
                      <p className="text-sm text-red-600 mt-1">
                        Error: {account.lastError}
                      </p>
                    )}
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleTestConnection}
                    disabled={isTesting}
                  >
                    {isTesting ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Testing...
                      </>
                    ) : (
                      'Test Connection'
                    )}
                  </Button>
                </div>
              </div>
            )}

            {/* Tabs */}
            <div className="border-b px-6 mt-4">
              <div className="flex gap-6">
                <button
                  onClick={() => setActiveTab('credentials')}
                  className={`pb-3 px-1 border-b-2 font-medium transition-colors ${
                    activeTab === 'credentials'
                      ? 'border-blue-600 text-blue-600'
                      : 'border-transparent text-gray-600 hover:text-gray-900'
                  }`}
                >
                  Credentials
                </button>
                <button
                  onClick={() => setActiveTab('settings')}
                  className={`pb-3 px-1 border-b-2 font-medium transition-colors ${
                    activeTab === 'settings'
                      ? 'border-blue-600 text-blue-600'
                      : 'border-transparent text-gray-600 hover:text-gray-900'
                  }`}
                >
                  Settings
                </button>
              </div>
            </div>

            <div className="p-6">
              {activeTab === 'credentials' ? (
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Account Name
                    </label>
                    <input
                      type="text"
                      value={formData.name}
                      onChange={(e) => handleInputChange('name', e.target.value)}
                      className="w-full rounded-md border border-gray-300 px-3 py-2"
                      placeholder="My Shiprocket Account"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      API Key / Email
                      {account?.hasApiKey && (
                        <span className="ml-2 text-xs text-green-600">✓ Configured</span>
                      )}
                    </label>
                    <input
                      type="text"
                      value={formData.apiKey}
                      onChange={(e) => handleInputChange('apiKey', e.target.value)}
                      className="w-full rounded-md border border-gray-300 px-3 py-2"
                      placeholder={account?.hasApiKey ? '••••••••' : 'Enter API key or email'}
                    />
                    <p className="text-xs text-gray-500 mt-1">
                      Leave empty to keep existing credentials
                    </p>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      API Secret / Password
                      {account?.hasApiSecret && (
                        <span className="ml-2 text-xs text-green-600">✓ Configured</span>
                      )}
                    </label>
                    <input
                      type="password"
                      value={formData.apiSecret}
                      onChange={(e) => handleInputChange('apiSecret', e.target.value)}
                      className="w-full rounded-md border border-gray-300 px-3 py-2"
                      placeholder={account?.hasApiSecret ? '••••••••' : 'Enter API secret or password'}
                    />
                    <p className="text-xs text-gray-500 mt-1">
                      Leave empty to keep existing credentials
                    </p>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Channel
                    </label>
                    {account?.courierType === 'Shiprocket' && account.isConnected ? (
                      isLoadingChannels ? (
                        <div className="flex items-center gap-2 py-2">
                          <Loader2 className="h-4 w-4 animate-spin text-blue-600" />
                          <span className="text-sm text-gray-500">Loading channels...</span>
                        </div>
                      ) : channelLoadError ? (
                        <>
                          <input
                            type="text"
                            value={formData.channelId}
                            onChange={(e) => handleInputChange('channelId', e.target.value)}
                            className="w-full rounded-md border border-gray-300 px-3 py-2"
                            placeholder="Enter channel ID manually"
                          />
                          <p className="text-xs text-amber-600 mt-1">
                            Could not load channels: {channelLoadError}.{' '}
                            <button
                              type="button"
                              onClick={loadChannels}
                              className="text-blue-600 hover:underline"
                            >
                              Retry
                            </button>
                          </p>
                        </>
                      ) : (
                        <>
                          <select
                            value={formData.channelId}
                            onChange={(e) => handleInputChange('channelId', e.target.value)}
                            className="w-full rounded-md border border-gray-300 px-3 py-2 bg-white"
                            aria-label="Channel"
                          >
                            <option value="">Custom / Adhoc (No channel)</option>
                            {channels.map((ch) => (
                              <option key={ch.id} value={String(ch.id)}>
                                {ch.name}{ch.type ? ` (${ch.type})` : ''}
                              </option>
                            ))}
                          </select>
                          <p className="text-xs text-gray-500 mt-1">
                            Select a channel for channel-specific orders, or &quot;Custom / Adhoc&quot; for custom orders.{' '}
                            <button
                              type="button"
                              onClick={loadChannels}
                              className="text-blue-600 hover:underline"
                            >
                              Refresh channels
                            </button>
                          </p>
                        </>
                      )
                    ) : (
                      <>
                        <input
                          type="text"
                          value={formData.channelId}
                          onChange={(e) => handleInputChange('channelId', e.target.value)}
                          className="w-full rounded-md border border-gray-300 px-3 py-2"
                          placeholder="Optional: Your channel ID"
                        />
                        {account?.courierType === 'Shiprocket' && !account.isConnected && (
                          <p className="text-xs text-amber-600 mt-1">
                            Connect the account first to load channels from Shiprocket.
                          </p>
                        )}
                      </>
                    )}
                  </div>
                </div>
              ) : (
                <div className="space-y-4">
                  <div>
                    <label htmlFor="pickup-location-select" className="block text-sm font-medium text-gray-700 mb-1">
                      Default Pickup Location
                    </label>
                    {account?.courierType === 'Shiprocket' && account.isConnected ? (
                      isLoadingPickupLocations ? (
                        <div className="flex items-center gap-2 py-2">
                          <Loader2 className="h-4 w-4 animate-spin text-blue-600" />
                          <span className="text-sm text-gray-500">Loading pickup locations...</span>
                        </div>
                      ) : pickupLocationLoadError ? (
                        <>
                          <input
                            id="pickup-location-select"
                            type="text"
                            value={formData.pickupLocation}
                            onChange={(e) => handleInputChange('pickupLocation', e.target.value)}
                            className="w-full rounded-md border border-gray-300 px-3 py-2"
                            placeholder="Enter pickup location name manually"
                          />
                          <p className="text-xs text-amber-600 mt-1">
                            Could not load pickup locations: {pickupLocationLoadError}.{' '}
                            <button
                              type="button"
                              onClick={loadPickupLocations}
                              className="text-blue-600 hover:underline"
                            >
                              Retry
                            </button>
                          </p>
                        </>
                      ) : (
                        <>
                          <select
                            id="pickup-location-select"
                            value={formData.pickupLocation}
                            onChange={(e) => handleInputChange('pickupLocation', e.target.value)}
                            className="w-full rounded-md border border-gray-300 px-3 py-2 bg-white"
                          >
                            <option value="">Select a pickup location (or use default)</option>
                            {pickupLocations.filter(loc => loc.isActive).map((loc) => {
                              const addressParts = [
                                loc.address,
                                loc.city,
                                loc.state,
                                loc.pinCode
                              ].filter(Boolean);
                              const fullAddress = addressParts.length > 0 ? ` - ${addressParts.join(', ')}` : '';
                              return (
                                <option key={loc.id} value={loc.name}>
                                  {loc.name}{fullAddress}
                                </option>
                              );
                            })}
                          </select>
                          {formData.pickupLocation && (
                            <div className="mt-2 p-3 bg-blue-50 border border-blue-200 rounded text-sm">
                              <p className="font-medium text-blue-900 mb-1">Selected Pickup Location:</p>
                              {(() => {
                                const selectedLoc = pickupLocations.find(
                                  loc => loc.name === formData.pickupLocation
                                );
                                if (selectedLoc) {
                                  return (
                                    <div className="text-blue-800 space-y-0.5">
                                      <p className="font-semibold">{selectedLoc.name}</p>
                                      {selectedLoc.address && <p>{selectedLoc.address}</p>}
                                      <p>
                                        {[selectedLoc.city, selectedLoc.state, selectedLoc.pinCode]
                                          .filter(Boolean)
                                          .join(', ')}
                                      </p>
                                      {selectedLoc.phone && <p>Phone: {selectedLoc.phone}</p>}
                                    </div>
                                  );
                                }
                                return <p className="text-blue-800">{formData.pickupLocation}</p>;
                              })()}
                            </div>
                          )}
                          <p className="text-xs text-gray-500 mt-1">
                            Select a pickup location from your Shiprocket account, or leave empty to use default.{' '}
                            <button
                              type="button"
                              onClick={loadPickupLocations}
                              className="text-blue-600 hover:underline"
                            >
                              Refresh locations
                            </button>
                          </p>
                        </>
                      )
                    ) : (
                      <>
                        <input
                          type="text"
                          value={formData.pickupLocation}
                          onChange={(e) => handleInputChange('pickupLocation', e.target.value)}
                          className="w-full rounded-md border border-gray-300 px-3 py-2"
                          placeholder="e.g., Primary"
                        />
                        <p className="text-xs text-gray-500 mt-1">
                          {account?.courierType === 'Shiprocket' && !account.isConnected
                            ? 'Connect the account first to load pickup locations from Shiprocket.'
                            : 'Default warehouse or pickup location for shipments'}
                        </p>
                      </>
                    )}
                  </div>

                  <div className="flex items-center justify-between p-4 border border-gray-200 rounded-lg">
                    <div>
                      <p className="font-medium text-gray-900">Account Status</p>
                      <p className="text-sm text-gray-600">
                        {formData.isActive ? 'Active - can be used for shipments' : 'Inactive - won\'t be used'}
                      </p>
                    </div>
                    <label className="relative inline-flex items-center cursor-pointer">
                      <input
                        type="checkbox"
                        checked={formData.isActive}
                        onChange={(e) => handleInputChange('isActive', e.target.checked)}
                        className="sr-only peer"
                      />
                      <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                    </label>
                  </div>

                  {account && (
                    <div className="bg-gray-50 p-4 rounded-lg space-y-2">
                      <h4 className="font-medium text-gray-900">Account Information</h4>
                      <div className="grid grid-cols-2 gap-3 text-sm">
                        <div>
                          <p className="text-gray-600">Courier Type</p>
                          <p className="font-medium">{account.courierTypeName}</p>
                        </div>
                        <div>
                          <p className="text-gray-600">Priority</p>
                          <p className="font-medium">{account.priority}</p>
                        </div>
                        <div>
                          <p className="text-gray-600">Default Account</p>
                          <p className="font-medium">{account.isDefault ? 'Yes' : 'No'}</p>
                        </div>
                        <div>
                          <p className="text-gray-600">Created</p>
                          <p className="font-medium">
                            {new Date(account.createdAt).toLocaleDateString()}
                          </p>
                        </div>
                      </div>
                    </div>
                  )}

                  <div className="bg-blue-50 border border-blue-200 p-4 rounded-lg">
                    <div className="flex gap-3">
                      <AlertCircle className="h-5 w-5 text-blue-600 mt-0.5 flex-shrink-0" />
                      <div>
                        <p className="font-medium text-blue-900">Service Capabilities</p>
                        <div className="mt-2 flex flex-wrap gap-2">
                          {account?.supportsCOD && (
                            <span className="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded">
                              COD Supported
                            </span>
                          )}
                          {account?.supportsReverse && (
                            <span className="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded">
                              Reverse Pickup
                            </span>
                          )}
                          {account?.supportsExpress && (
                            <span className="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded">
                              Express Delivery
                            </span>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>

            <div className="sticky bottom-0 bg-gray-50 px-6 py-4 flex gap-3 border-t">
              <Button
                type="button"
                variant="outline"
                onClick={onClose}
                disabled={isSaving}
                className="flex-1"
              >
                Cancel
              </Button>
              <Button
                type="button"
                onClick={handleSave}
                disabled={isSaving}
                className="flex-1"
              >
                {isSaving ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Saving...
                  </>
                ) : (
                  'Save Changes'
                )}
              </Button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
