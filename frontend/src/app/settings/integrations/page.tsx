'use client';

import { useState, useEffect } from 'react';
import { DashboardLayout } from '@/components/layout';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Button,
  Badge,
} from '@/components/ui';
import {
  ShoppingCart,
  Truck,
  Bell,
  CreditCard,
  Settings,
  Plus,
  CheckCircle,
  AlertCircle,
  ExternalLink,
  Trash2,
  Wallet,
  RefreshCw,
} from 'lucide-react';
import CourierSettingsModal from '@/components/integrations/CourierSettingsModal';
import { courierService, CourierAccountDto, CourierWalletBalance } from '@/services/courier.service';
import { channelsService, Channel } from '@/services/channels.service';
import { toast } from 'sonner';
import Link from 'next/link';

interface IntegrationSetting {
  id: string;
  name: string;
  type: 'channel' | 'shipping' | 'payment' | 'notification';
  isConnected: boolean;
  status: 'active' | 'inactive' | 'error';
  lastSync?: string;
  errorMessage?: string;
  accountId?: string;
}

export default function IntegrationsSettingsPage() {
  const [courierAccounts, setCourierAccounts] = useState<CourierAccountDto[]>([]);
  const [channels, setChannels] = useState<Channel[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showCourierSettings, setShowCourierSettings] = useState(false);
  const [selectedCourierId, setSelectedCourierId] = useState<string | null>(null);
  const [selectedCourierName, setSelectedCourierName] = useState<string>('');
  const [walletBalances, setWalletBalances] = useState<Record<string, CourierWalletBalance>>({});
  const [loadingWallets, setLoadingWallets] = useState<Record<string, boolean>>({});

  useEffect(() => {
    loadIntegrations();
  }, []);

  const loadIntegrations = async () => {
    try {
      setIsLoading(true);
      // Load sales channels
      const channelsData = await channelsService.getChannels();
      setChannels(channelsData);

      // Load courier/shipping integrations
      const couriers = await courierService.getCourierAccounts();
      setCourierAccounts(couriers);

      // Load wallet balances for Shiprocket accounts that are connected
      couriers.forEach((courier) => {
        if (courier.isConnected && courier.courierTypeName === 'Shiprocket') {
          loadWalletBalance(courier.id);
        }
      });

      // TODO: Load other integration types (payments, notifications)
    } catch (error) {
      console.error('Failed to load integrations:', error);
      toast.error('Failed to load integrations');
    } finally {
      setIsLoading(false);
    }
  };

  const loadWalletBalance = async (courierId: string) => {
    try {
      setLoadingWallets(prev => ({ ...prev, [courierId]: true }));
      const balance = await courierService.getWalletBalance(courierId);
      setWalletBalances(prev => ({ ...prev, [courierId]: balance }));
    } catch (error) {
      console.error(`Failed to load wallet balance for ${courierId}:`, error);
      // Don't show error toast for wallet balance failures
    } finally {
      setLoadingWallets(prev => ({ ...prev, [courierId]: false }));
    }
  };

  const handleConfigureCourier = (courier: CourierAccountDto) => {
    setSelectedCourierId(courier.id);
    setSelectedCourierName(courier.name);
    setShowCourierSettings(true);
  };

  const handleDeleteCourier = async (courier: CourierAccountDto) => {
    if (!confirm(`Are you sure you want to remove ${courier.name}?`)) {
      return;
    }

    try {
      await courierService.deleteCourierAccount(courier.id);
      toast.success('Integration removed successfully');
      loadIntegrations();
    } catch (error) {
      toast.error('Failed to remove integration');
    }
  };

  const handleTestConnection = async (courier: CourierAccountDto) => {
    try {
      const result = await courierService.testConnection(courier.id);
      if (result.isConnected) {
        toast.success('Connection test successful!');
        loadIntegrations();
      } else {
        toast.error(`Connection failed: ${result.message}`);
      }
    } catch (error) {
      toast.error('Connection test failed');
    }
  };

  return (
    <DashboardLayout title="Integration Settings">
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold">Integration Settings</h1>
            <p className="text-muted-foreground">
              Manage your connected services and integrations
            </p>
          </div>
          <Link href="/integrations">
            <Button variant="outline">
              <Plus className="mr-2 h-4 w-4" />
              Add Integration
            </Button>
          </Link>
        </div>

        {/* Summary Cards */}
        <div className="grid gap-4 md:grid-cols-4">
          <Card>
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <div className="rounded-lg bg-blue-50 p-2">
                  <ShoppingCart className="h-5 w-5 text-blue-600" />
                </div>
                <div>
                  <p className="text-sm text-gray-600">Sales Channels</p>
                  <p className="text-2xl font-bold">{channels.length}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <div className="rounded-lg bg-green-50 p-2">
                  <Truck className="h-5 w-5 text-green-600" />
                </div>
                <div>
                  <p className="text-sm text-gray-600">Shipping Partners</p>
                  <p className="text-2xl font-bold">{courierAccounts.length}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <div className="rounded-lg bg-orange-50 p-2">
                  <CreditCard className="h-5 w-5 text-orange-600" />
                </div>
                <div>
                  <p className="text-sm text-gray-600">Payment Gateways</p>
                  <p className="text-2xl font-bold">0</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <div className="rounded-lg bg-purple-50 p-2">
                  <Bell className="h-5 w-5 text-purple-600" />
                </div>
                <div>
                  <p className="text-sm text-gray-600">Notifications</p>
                  <p className="text-2xl font-bold">0</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* E-commerce Channels Section */}
        <div>
          <div className="mb-4 flex items-center gap-3">
            <div className="rounded-lg bg-blue-50 p-2">
              <ShoppingCart className="h-5 w-5 text-blue-600" />
            </div>
            <div>
              <h2 className="text-lg font-semibold">E-commerce Channels</h2>
              <p className="text-sm text-muted-foreground">
                Connected sales channels and marketplaces
              </p>
            </div>
          </div>

          {channels.length === 0 ? (
            <Card>
              <CardContent className="p-6">
                <div className="flex flex-col items-center justify-center py-8 text-center">
                  <ShoppingCart className="h-12 w-12 text-gray-300 mb-3" />
                  <p className="text-sm font-medium text-gray-900">
                    No e-commerce channels connected
                  </p>
                  <p className="text-sm text-gray-500 mt-1 mb-4">
                    Connect your Shopify, Amazon, Flipkart or other sales channels
                  </p>
                  <Link href="/integrations">
                    <Button size="sm">
                      <Plus className="mr-2 h-4 w-4" />
                      Connect Channel
                    </Button>
                  </Link>
                </div>
              </CardContent>
            </Card>
          ) : (
            <div className="grid gap-4 md:grid-cols-2">
              {channels.map((channel) => (
                <Card key={channel.id}>
                  <CardHeader>
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-3">
                        <div className="rounded-lg bg-blue-50 p-2">
                          <ShoppingCart className="h-5 w-5 text-blue-600" />
                        </div>
                        <div>
                          <h3 className="font-semibold">{channel.name}</h3>
                          <p className="text-sm text-muted-foreground">
                            {channel.type} • {channel.storeName || channel.storeUrl}
                          </p>
                        </div>
                      </div>
                      <div className="flex flex-col items-end gap-1">
                        {channel.isConnected ? (
                          <Badge variant="success" className="gap-1">
                            <CheckCircle className="h-3 w-3" />
                            Connected
                          </Badge>
                        ) : (
                          <Badge variant="error" className="gap-1">
                            <AlertCircle className="h-3 w-3" />
                            Disconnected
                          </Badge>
                        )}
                        {channel.isActive && (
                          <Badge variant="primary" className="text-xs">
                            Active
                          </Badge>
                        )}
                      </div>
                    </div>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="space-y-2">
                      <div className="flex justify-between text-sm">
                        <span className="text-muted-foreground">Total Orders:</span>
                        <span className="font-medium">{channel.totalOrders}</span>
                      </div>
                      {channel.lastSyncAt && (
                        <div className="flex justify-between text-sm">
                          <span className="text-muted-foreground">Last Sync:</span>
                          <span className="font-medium">
                            {new Date(channel.lastSyncAt).toLocaleDateString()}
                          </span>
                        </div>
                      )}
                      <div className="flex justify-between text-sm">
                        <span className="text-muted-foreground">Auto Sync Orders:</span>
                        <span className="font-medium">
                          {channel.autoSyncOrders ? 'Enabled' : 'Disabled'}
                        </span>
                      </div>
                    </div>
                    <div className="flex gap-2 pt-2">
                      <Link href={`/channels/${channel.id}`} className="flex-1">
                        <Button variant="outline" size="sm" className="w-full">
                          <Settings className="mr-2 h-4 w-4" />
                          Configure
                        </Button>
                      </Link>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => channelsService.syncChannel(channel.id)}
                      >
                        Sync Now
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </div>

        {/* Shipping Integration Section */}
        <div>
          <div className="mb-4 flex items-center gap-3">
            <div className="rounded-lg bg-green-50 p-2">
              <Truck className="h-5 w-5 text-green-600" />
            </div>
            <div>
              <h2 className="text-lg font-semibold">Shipping Integration</h2>
              <p className="text-sm text-muted-foreground">
                Courier partners and logistics providers
              </p>
            </div>
          </div>

          {courierAccounts.length === 0 ? (
            <Card>
              <CardContent className="p-6">
                <div className="flex flex-col items-center justify-center py-8 text-center">
                  <Truck className="h-12 w-12 text-gray-300 mb-3" />
                  <p className="text-sm font-medium text-gray-900">
                    No shipping partners connected
                  </p>
                  <p className="text-sm text-gray-500 mt-1 mb-4">
                    Connect Shiprocket, Blue Dart, Delhivery or other couriers
                  </p>
                  <Link href="/integrations">
                    <Button size="sm">
                      <Plus className="mr-2 h-4 w-4" />
                      Connect Courier
                    </Button>
                  </Link>
                </div>
              </CardContent>
            </Card>
          ) : (
            <div className="grid gap-4 md:grid-cols-2">
              {courierAccounts.map((courier) => (
                <Card key={courier.id}>
                  <CardHeader>
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-3">
                        <div className="rounded-lg bg-green-50 p-2">
                          <Truck className="h-5 w-5 text-green-600" />
                        </div>
                        <div>
                          <CardTitle className="text-base">{courier.name}</CardTitle>
                          <p className="text-sm text-gray-500">
                            {courier.courierTypeName}
                          </p>
                          {courier.apiUserEmail && (
                            <p className="text-xs text-gray-400 mt-0.5">
                              {courier.apiUserEmail}
                            </p>
                          )}
                        </div>
                      </div>
                      <div className="flex flex-col items-end gap-1">
                        {courier.isConnected ? (
                          <Badge variant="success" className="gap-1">
                            <CheckCircle className="h-3 w-3" />
                            Connected
                          </Badge>
                        ) : (
                          <Badge variant="error" className="gap-1">
                            <AlertCircle className="h-3 w-3" />
                            Disconnected
                          </Badge>
                        )}
                        {courier.isDefault && (
                          <Badge variant="primary" className="text-xs">
                            Default
                          </Badge>
                        )}
                      </div>
                    </div>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="space-y-2">
                      <div className="flex justify-between text-sm">
                        <span className="text-gray-600">Status:</span>
                        <span className={courier.isActive ? 'text-green-600' : 'text-gray-400'}>
                          {courier.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </div>
                      {courier.lastConnectedAt && (
                        <div className="flex justify-between text-sm">
                          <span className="text-gray-600">Last Connected:</span>
                          <span className="text-gray-900">
                            {new Date(courier.lastConnectedAt).toLocaleDateString()}
                          </span>
                        </div>
                      )}
                      {courier.isConnected && courier.courierTypeName === 'Shiprocket' && (
                        <div className="flex justify-between items-center text-sm rounded-md bg-green-50 p-2">
                          <span className="flex items-center gap-1 text-gray-700">
                            <Wallet className="h-4 w-4" />
                            Wallet Balance:
                          </span>
                          {loadingWallets[courier.id] ? (
                            <span className="text-gray-500">Loading...</span>
                          ) : walletBalances[courier.id] ? (
                            <div className="flex items-center gap-2">
                              <span className="font-semibold text-green-700">
                                ₹{walletBalances[courier.id].balance.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                              </span>
                              <button
                                type="button"
                                onClick={() => loadWalletBalance(courier.id)}
                                className="text-gray-500 hover:text-gray-700"
                                title="Refresh balance"
                              >
                                <RefreshCw className="h-3 w-3" />
                              </button>
                            </div>
                          ) : (
                            <button
                              type="button"
                              onClick={() => loadWalletBalance(courier.id)}
                              className="text-xs text-blue-600 hover:underline"
                            >
                              Load Balance
                            </button>
                          )}
                        </div>
                      )}
                      {courier.lastError && (
                        <div className="rounded-md bg-red-50 p-2">
                          <p className="text-xs text-red-700">{courier.lastError}</p>
                        </div>
                      )}
                    </div>

                    <div className="flex gap-2 pt-2 border-t">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleConfigureCourier(courier)}
                        className="flex-1"
                      >
                        <Settings className="mr-2 h-4 w-4" />
                        Settings
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleTestConnection(courier)}
                      >
                        Test
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleDeleteCourier(courier)}
                        className="text-red-600 hover:bg-red-50"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </div>

        {/* Payment Integration Section */}
        <div>
          <div className="mb-4 flex items-center gap-3">
            <div className="rounded-lg bg-orange-50 p-2">
              <CreditCard className="h-5 w-5 text-orange-600" />
            </div>
            <div>
              <h2 className="text-lg font-semibold">Payment Integration</h2>
              <p className="text-sm text-muted-foreground">
                Payment gateways and processors
              </p>
            </div>
          </div>

          <Card>
            <CardContent className="p-6">
              <div className="flex flex-col items-center justify-center py-8 text-center">
                <CreditCard className="h-12 w-12 text-gray-300 mb-3" />
                <p className="text-sm font-medium text-gray-900">
                  No payment gateways connected
                </p>
                <p className="text-sm text-gray-500 mt-1 mb-4">
                  Connect Razorpay, Paytm, PhonePe or other payment providers
                </p>
                <Link href="/integrations">
                  <Button size="sm">
                    <Plus className="mr-2 h-4 w-4" />
                    Connect Payment Gateway
                  </Button>
                </Link>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Notification Partner Integration Section */}
        <div>
          <div className="mb-4 flex items-center gap-3">
            <div className="rounded-lg bg-purple-50 p-2">
              <Bell className="h-5 w-5 text-purple-600" />
            </div>
            <div>
              <h2 className="text-lg font-semibold">Notification Partners</h2>
              <p className="text-sm text-muted-foreground">
                Email, SMS and messaging services
              </p>
            </div>
          </div>

          <Card>
            <CardContent className="p-6">
              <div className="flex flex-col items-center justify-center py-8 text-center">
                <Bell className="h-12 w-12 text-gray-300 mb-3" />
                <p className="text-sm font-medium text-gray-900">
                  No notification services connected
                </p>
                <p className="text-sm text-gray-500 mt-1 mb-4">
                  Connect SMTP, SendGrid, Twilio, MSG91 or WhatsApp Business
                </p>
                <Link href="/integrations">
                  <Button size="sm">
                    <Plus className="mr-2 h-4 w-4" />
                    Connect Service
                  </Button>
                </Link>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Courier Settings Modal */}
      {showCourierSettings && selectedCourierId && (
        <CourierSettingsModal
          isOpen={showCourierSettings}
          onClose={() => {
            setShowCourierSettings(false);
            setSelectedCourierId(null);
            setSelectedCourierName('');
          }}
          accountId={selectedCourierId}
          courierName={selectedCourierName}
          onUpdate={loadIntegrations}
        />
      )}
    </DashboardLayout>
  );
}
