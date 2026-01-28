'use client';

import { useState, useEffect } from 'react';
import { useRouter, useSearchParams, useParams } from 'next/navigation';
import Link from 'next/link';
import { DashboardLayout } from '@/components/layout';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Button,
  Input,
  Badge,
  SectionLoader,
  Modal,
} from '@/components/ui';
import { formatDateTime } from '@/lib/utils';
import { useChannels, useConnectShopify, useDisconnectChannel, useSyncChannel, useSyncInventory, useSyncProducts, useUpdateChannelSettings } from '@/hooks';
import type { Channel } from '@/services/channels.service';
import {
  Store,
  ArrowLeft,
  RefreshCw,
  Trash2,
  ExternalLink,
  CheckCircle,
  AlertCircle,
  Clock,
  Loader2,
  Settings,
  ShoppingCart,
  Package,
  Zap,
  Shield,
  ChevronDown,
  ChevronRight,
} from 'lucide-react';

export default function ChannelSettingsPage() {
  const router = useRouter();
  const params = useParams();
  const searchParams = useSearchParams();
  const channelId = params.id as string;
  const connected = searchParams.get('connected');
  const error = searchParams.get('error');

  // Settings state
  const [showDisconnectModal, setShowDisconnectModal] = useState(false);
  const [autoSyncOrders, setAutoSyncOrders] = useState(true);
  const [autoSyncInventory, setAutoSyncInventory] = useState(false);
  const [initialSyncDays, setInitialSyncDays] = useState<number | null>(7);
  const [inventorySyncDays, setInventorySyncDays] = useState<number | null>(7);
  const [productSyncDays, setProductSyncDays] = useState<number | null>(7);
  const [orderSyncLimit, setOrderSyncLimit] = useState<number | null>(100);
  const [inventorySyncLimit, setInventorySyncLimit] = useState<number | null>(500);
  const [productSyncLimit, setProductSyncLimit] = useState<number | null>(50);
  const [syncProductsEnabled, setSyncProductsEnabled] = useState(false);
  const [autoSyncProducts, setAutoSyncProducts] = useState(false);
  const [settingsChanged, setSettingsChanged] = useState(false);

  // Expand/collapse state for sync sections
  const [orderSyncExpanded, setOrderSyncExpanded] = useState(false);
  const [inventorySyncExpanded, setInventorySyncExpanded] = useState(false);
  const [productSyncExpanded, setProductSyncExpanded] = useState(false);

  const { data: channels, isLoading, refetch } = useChannels();
  const connectShopify = useConnectShopify();
  const disconnectChannel = useDisconnectChannel();
  const syncChannel = useSyncChannel();
  const syncInventory = useSyncInventory();
  const syncProducts = useSyncProducts();
  const updateSettings = useUpdateChannelSettings();

  // Find the specific channel by ID
  const channel = channels?.find(c => c.id === channelId);

  // Update local state when channel data loads
  // Note: We preserve null values for sync days/limits as null means "All time" / "Unlimited"
  useEffect(() => {
    if (channel) {
      setAutoSyncOrders(channel.autoSyncOrders ?? true);
      setAutoSyncInventory(channel.autoSyncInventory ?? false);
      // Preserve null values - null means "All time" for days and "Unlimited" for limits
      setInitialSyncDays(channel.initialSyncDays !== undefined ? channel.initialSyncDays : 7);
      setInventorySyncDays(channel.inventorySyncDays !== undefined ? channel.inventorySyncDays : 7);
      setProductSyncDays(channel.productSyncDays !== undefined ? channel.productSyncDays : 7);
      setOrderSyncLimit(channel.orderSyncLimit !== undefined ? channel.orderSyncLimit : 100);
      setInventorySyncLimit(channel.inventorySyncLimit !== undefined ? channel.inventorySyncLimit : 500);
      setProductSyncLimit(channel.productSyncLimit !== undefined ? channel.productSyncLimit : 50);
      setSyncProductsEnabled(channel.syncProductsEnabled ?? false);
      setAutoSyncProducts(channel.autoSyncProducts ?? false);
    }
  }, [channel]);

  // Refetch on mount and when connected param changes
  useEffect(() => {
    if (connected) {
      refetch();
    }
  }, [connected, refetch]);

  const handleConnect = async () => {
    if (!channel) return;

    try {
      const result = await connectShopify.mutateAsync(channel.id);
      window.location.href = result.authorizationUrl;
    } catch (err) {
      console.error('Failed to initiate Shopify connection:', err);
    }
  };

  const handleDisconnect = async () => {
    if (!channel) return;

    try {
      await disconnectChannel.mutateAsync(channel.id);
      setShowDisconnectModal(false);
      router.push('/channels');
    } catch (err) {
      console.error('Failed to disconnect channel:', err);
    }
  };

  const handleSync = async () => {
    if (!channel) return;

    try {
      await syncChannel.mutateAsync(channel.id);
    } catch (err) {
      console.error('Failed to sync orders:', err);
    }
  };

  const handleSyncInventory = async () => {
    if (!channel) return;

    try {
      const result = await syncInventory.mutateAsync(channel.id);
      console.log('Inventory sync completed:', result);
    } catch (err: unknown) {
      const error = err as { message?: string; statusCode?: number };
      console.error('Failed to sync inventory:', error.message || JSON.stringify(err));
    }
  };

  const handleSyncProducts = async () => {
    if (!channel) return;

    try {
      const result = await syncProducts.mutateAsync(channel.id);
      console.log('Product sync completed:', result);
    } catch (err: unknown) {
      const error = err as { message?: string; statusCode?: number };
      console.error('Failed to sync products:', error.message || JSON.stringify(err));
    }
  };

  const handleSaveSettings = async () => {
    if (!channel) return;

    try {
      await updateSettings.mutateAsync({
        id: channel.id,
        autoSyncOrders,
        autoSyncInventory,
        initialSyncDays,
        inventorySyncDays,
        productSyncDays,
        orderSyncLimit,
        inventorySyncLimit,
        productSyncLimit,
        syncProductsEnabled,
        autoSyncProducts,
      });
      setSettingsChanged(false);
    } catch (err) {
      console.error('Failed to update settings:', err);
    }
  };

  const handleSettingChange = (setting: 'orders' | 'inventory' | 'syncProducts' | 'autoSyncProducts', value: boolean) => {
    if (setting === 'orders') {
      setAutoSyncOrders(value);
    } else if (setting === 'inventory') {
      setAutoSyncInventory(value);
    } else if (setting === 'syncProducts') {
      setSyncProductsEnabled(value);
    } else if (setting === 'autoSyncProducts') {
      setAutoSyncProducts(value);
    }
    setSettingsChanged(true);
  };

  const handleInitialSyncDaysChange = (value: number | null) => {
    setInitialSyncDays(value);
    setSettingsChanged(true);
  };

  const handleInventorySyncDaysChange = (value: number | null) => {
    setInventorySyncDays(value);
    setSettingsChanged(true);
  };

  const handleOrderSyncLimitChange = (value: number | null) => {
    setOrderSyncLimit(value);
    setSettingsChanged(true);
  };

  const handleInventorySyncLimitChange = (value: number | null) => {
    setInventorySyncLimit(value);
    setSettingsChanged(true);
  };

  const handleProductSyncDaysChange = (value: number | null) => {
    setProductSyncDays(value);
    setSettingsChanged(true);
  };

  const handleProductSyncLimitChange = (value: number | null) => {
    setProductSyncLimit(value);
    setSettingsChanged(true);
  };

  if (isLoading) {
    return (
      <DashboardLayout title="Channel Settings">
        <SectionLoader />
      </DashboardLayout>
    );
  }

  if (!channel) {
    return (
      <DashboardLayout title="Channel Not Found">
        <div className="text-center py-12">
          <AlertCircle className="h-12 w-12 text-error mx-auto mb-4" />
          <h2 className="text-lg font-medium">Channel not found</h2>
          <p className="text-muted-foreground mt-2">The channel you're looking for doesn't exist.</p>
          <Link href="/channels">
            <Button className="mt-4">Back to Channels</Button>
          </Link>
        </div>
      </DashboardLayout>
    );
  }

  // Determine which view to show based on channel state
  const hasCredentials = channel.hasCredentials ?? false;
  const isConnected = channel.isConnected ?? false;

  return (
    <DashboardLayout title={`${channel.type} Settings`}>
      {/* Back Link */}
      <Link
        href="/channels"
        className="mb-4 inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to Channels
      </Link>

      {/* Success/Error Messages */}
      {connected && (
        <div className="mb-4 flex items-center gap-2 rounded-lg bg-success/10 p-4 text-success">
          <CheckCircle className="h-5 w-5" />
          <span>{channel.type} store connected successfully!</span>
        </div>
      )}
      {error && (
        <div className="mb-4 flex items-center gap-2 rounded-lg bg-error/10 p-4 text-error">
          <AlertCircle className="h-5 w-5" />
          <span>{decodeURIComponent(error)}</span>
        </div>
      )}
      {channel.lastError && !connected && (
        <div className="mb-4 flex items-center gap-2 rounded-lg bg-warning/10 p-4 text-warning">
          <AlertCircle className="h-5 w-5" />
          <span>Last connection error: {channel.lastError}</span>
        </div>
      )}

      {isConnected ? (
        // Connected State - Show full settings
        <ConnectedView
          channel={channel}
          autoSyncOrders={autoSyncOrders}
          autoSyncInventory={autoSyncInventory}
          initialSyncDays={initialSyncDays}
          inventorySyncDays={inventorySyncDays}
          productSyncDays={productSyncDays}
          orderSyncLimit={orderSyncLimit}
          inventorySyncLimit={inventorySyncLimit}
          productSyncLimit={productSyncLimit}
          syncProductsEnabled={syncProductsEnabled}
          autoSyncProducts={autoSyncProducts}
          settingsChanged={settingsChanged}
          orderSyncExpanded={orderSyncExpanded}
          inventorySyncExpanded={inventorySyncExpanded}
          productSyncExpanded={productSyncExpanded}
          onSettingChange={handleSettingChange}
          onInitialSyncDaysChange={handleInitialSyncDaysChange}
          onInventorySyncDaysChange={handleInventorySyncDaysChange}
          onProductSyncDaysChange={handleProductSyncDaysChange}
          onOrderSyncLimitChange={handleOrderSyncLimitChange}
          onInventorySyncLimitChange={handleInventorySyncLimitChange}
          onProductSyncLimitChange={handleProductSyncLimitChange}
          onOrderSyncExpandedChange={setOrderSyncExpanded}
          onInventorySyncExpandedChange={setInventorySyncExpanded}
          onProductSyncExpandedChange={setProductSyncExpanded}
          onSaveSettings={handleSaveSettings}
          onSync={handleSync}
          onSyncInventory={handleSyncInventory}
          onSyncProducts={handleSyncProducts}
          onDisconnect={() => setShowDisconnectModal(true)}
          isSaving={updateSettings.isPending}
          isSyncing={syncChannel.isPending}
          isSyncingInventory={syncInventory.isPending}
          isSyncingProducts={syncProducts.isPending}
        />
      ) : hasCredentials ? (
        // Has credentials but not connected - Show connect button
        <CredentialsSavedView
          channel={channel}
          onConnect={handleConnect}
          onDisconnect={() => setShowDisconnectModal(true)}
          isConnecting={connectShopify.isPending}
        />
      ) : (
        // No credentials - Redirect to type-specific setup page
        <NoCredentialsView channel={channel} />
      )}

      {/* Disconnect Confirmation Modal */}
      <Modal
        isOpen={showDisconnectModal}
        onClose={() => setShowDisconnectModal(false)}
        title={`Disconnect ${channel.type} Store`}
      >
        <div className="space-y-4">
          <div className="rounded-lg bg-error/10 p-4">
            <p className="text-sm text-error">
              <strong>Warning:</strong> This action will:
            </p>
            <ul className="mt-2 list-inside list-disc text-sm text-error">
              <li>Stop all automatic order syncing</li>
              <li>Remove all webhooks from your store</li>
              <li>Clear the stored access credentials</li>
            </ul>
          </div>
          <p className="text-muted-foreground">
            Your existing orders will not be deleted. You can reconnect your store at any time.
          </p>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setShowDisconnectModal(false)}>
              Cancel
            </Button>
            <Button
              variant="danger"
              onClick={handleDisconnect}
              disabled={disconnectChannel.isPending}
              leftIcon={disconnectChannel.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Trash2 className="h-4 w-4" />}
            >
              {disconnectChannel.isPending ? 'Disconnecting...' : 'Yes, Disconnect'}
            </Button>
          </div>
        </div>
      </Modal>
    </DashboardLayout>
  );
}

// No Credentials View - Redirect to setup
function NoCredentialsView({ channel }: { channel: Channel }) {
  const router = useRouter();

  useEffect(() => {
    // Redirect to type-specific setup page
    if (channel.type === 'Shopify') {
      router.push('/channels/shopify');
    }
  }, [channel.type, router]);

  return (
    <Card>
      <CardContent className="py-12 text-center">
        <AlertCircle className="h-12 w-12 text-warning mx-auto mb-4" />
        <h3 className="text-lg font-medium">Setup Required</h3>
        <p className="text-muted-foreground mt-2">
          This channel needs to be configured before it can be used.
        </p>
        {channel.type === 'Shopify' && (
          <Link href="/channels/shopify">
            <Button className="mt-4">Configure Shopify</Button>
          </Link>
        )}
      </CardContent>
    </Card>
  );
}

// Credentials Saved View - Step 2: Initiate OAuth
function CredentialsSavedView({
  channel,
  onConnect,
  onDisconnect,
  isConnecting,
}: {
  channel: Channel;
  onConnect: () => void;
  onDisconnect: () => void;
  isConnecting: boolean;
}) {
  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-yellow-100">
              <Store className="h-6 w-6 text-yellow-700" />
            </div>
            <div>
              <CardTitle className="flex items-center gap-2">
                {channel.storeName || channel.name}
                <Badge variant="warning" size="sm">Credentials Saved</Badge>
              </CardTitle>
              <p className="text-sm text-muted-foreground">
                Complete the OAuth connection to start syncing orders
              </p>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="rounded-lg border border-warning/30 bg-warning/10 p-4">
            <div className="flex items-start gap-3">
              <AlertCircle className="h-5 w-5 text-warning shrink-0 mt-0.5" />
              <div>
                <p className="font-medium text-warning">OAuth Connection Required</p>
                <p className="text-sm text-muted-foreground mt-1">
                  Your API credentials have been saved. Click the button below to connect to {channel.type}
                  and authorize access to your store.
                </p>
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-4 sm:flex-row">
            <Button
              onClick={onConnect}
              disabled={isConnecting}
              leftIcon={
                isConnecting ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <ExternalLink className="h-4 w-4" />
                )
              }
              size="lg"
            >
              {isConnecting ? 'Connecting...' : `Connect to ${channel.type}`}
            </Button>
            <Button variant="outline" onClick={onDisconnect}>
              <Trash2 className="h-4 w-4 mr-2" />
              Remove Credentials
            </Button>
          </div>

          <div className="rounded-lg bg-muted/50 p-4">
            <h4 className="mb-2 font-medium">What happens next?</h4>
            <ul className="space-y-1 text-sm text-muted-foreground">
              <li>• You&apos;ll be redirected to {channel.type} to log in</li>
              <li>• Review and approve the requested permissions</li>
              <li>• You&apos;ll be redirected back here once connected</li>
            </ul>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// Connected View - Full settings
function ConnectedView({
  channel,
  autoSyncOrders,
  autoSyncInventory,
  initialSyncDays,
  inventorySyncDays,
  productSyncDays,
  orderSyncLimit,
  inventorySyncLimit,
  productSyncLimit,
  syncProductsEnabled,
  autoSyncProducts,
  settingsChanged,
  orderSyncExpanded,
  inventorySyncExpanded,
  productSyncExpanded,
  onSettingChange,
  onInitialSyncDaysChange,
  onInventorySyncDaysChange,
  onProductSyncDaysChange,
  onOrderSyncLimitChange,
  onInventorySyncLimitChange,
  onProductSyncLimitChange,
  onOrderSyncExpandedChange,
  onInventorySyncExpandedChange,
  onProductSyncExpandedChange,
  onSaveSettings,
  onSync,
  onSyncInventory,
  onSyncProducts,
  onDisconnect,
  isSaving,
  isSyncing,
  isSyncingInventory,
  isSyncingProducts,
}: {
  channel: Channel;
  autoSyncOrders: boolean;
  autoSyncInventory: boolean;
  initialSyncDays: number | null;
  inventorySyncDays: number | null;
  productSyncDays: number | null;
  orderSyncLimit: number | null;
  inventorySyncLimit: number | null;
  productSyncLimit: number | null;
  syncProductsEnabled: boolean;
  autoSyncProducts: boolean;
  settingsChanged: boolean;
  orderSyncExpanded: boolean;
  inventorySyncExpanded: boolean;
  productSyncExpanded: boolean;
  onSettingChange: (setting: 'orders' | 'inventory' | 'syncProducts' | 'autoSyncProducts', value: boolean) => void;
  onInitialSyncDaysChange: (value: number | null) => void;
  onInventorySyncDaysChange: (value: number | null) => void;
  onProductSyncDaysChange: (value: number | null) => void;
  onOrderSyncLimitChange: (value: number | null) => void;
  onInventorySyncLimitChange: (value: number | null) => void;
  onProductSyncLimitChange: (value: number | null) => void;
  onOrderSyncExpandedChange: (value: boolean) => void;
  onInventorySyncExpandedChange: (value: boolean) => void;
  onProductSyncExpandedChange: (value: boolean) => void;
  onSaveSettings: () => void;
  onSync: () => void;
  onSyncInventory: () => void;
  onSyncProducts: () => void;
  onDisconnect: () => void;
  isSaving: boolean;
  isSyncing: boolean;
  isSyncingInventory: boolean;
  isSyncingProducts: boolean;
}) {
  return (
    <div className="space-y-6">
      {/* Connection Status Card */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-green-100">
              <Store className="h-6 w-6 text-green-700" />
            </div>
            <div>
              <CardTitle className="flex items-center gap-2">
                {channel.storeName || channel.name}
                <Badge variant="success" size="sm">Connected</Badge>
              </CardTitle>
              <p className="text-sm text-muted-foreground">
                {channel.storeUrl}
              </p>
            </div>
          </div>
          {channel.storeUrl && (
            <a
              href={`https://${channel.storeUrl.replace('https://', '')}/admin`}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-2 text-sm text-primary hover:underline"
            >
              <ExternalLink className="h-4 w-4" />
              Open Admin
            </a>
          )}
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-4">
            <div className="flex items-center gap-3 rounded-lg border p-3">
              <ShoppingCart className="h-5 w-5 text-muted-foreground" />
              <div>
                <p className="text-sm text-muted-foreground">Total Orders</p>
                <p className="text-lg font-semibold">{channel.totalOrders.toLocaleString()}</p>
              </div>
            </div>
            <div className="flex items-center gap-3 rounded-lg border p-3">
              <Clock className="h-5 w-5 text-muted-foreground" />
              <div>
                <p className="text-sm text-muted-foreground">Last Sync</p>
                <p className="text-lg font-semibold">
                  {channel.lastSyncAt ? formatDateTime(channel.lastSyncAt) : 'Never'}
                </p>
              </div>
            </div>
            <div className="flex items-center gap-3 rounded-lg border p-3">
              <SyncStatusIcon status={channel.syncStatus} />
              <div>
                <p className="text-sm text-muted-foreground">Sync Status</p>
                <p className="text-lg font-semibold capitalize">
                  {channel.syncStatus === 'NotStarted' ? 'Not Started' : channel.syncStatus}
                </p>
              </div>
            </div>
            <div className="flex items-center gap-3 rounded-lg border p-3">
              <Package className="h-5 w-5 text-muted-foreground" />
              <div>
                <p className="text-sm text-muted-foreground">Connected Since</p>
                <p className="text-lg font-semibold">{formatDateTime(channel.createdAt)}</p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Sync Settings Card */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Settings className="h-5 w-5" />
            Sync Settings
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* ===== ORDER SYNC SECTION ===== */}
          <div className="rounded-lg border">
            <div
              className="flex items-center justify-between p-4 cursor-pointer hover:bg-muted/50 transition-colors"
              onClick={() => onOrderSyncExpandedChange(!orderSyncExpanded)}
            >
              <div className="flex items-center gap-3">
                {orderSyncExpanded ? (
                  <ChevronDown className="h-5 w-5 text-muted-foreground" />
                ) : (
                  <ChevronRight className="h-5 w-5 text-muted-foreground" />
                )}
                <ShoppingCart className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="font-medium">Order Sync</p>
                  <p className="text-sm text-muted-foreground">
                    Configure order syncing from {channel.type}
                  </p>
                </div>
              </div>
              <label className="relative inline-flex cursor-pointer items-center" onClick={(e) => e.stopPropagation()}>
                <input
                  type="checkbox"
                  className="peer sr-only"
                  checked={autoSyncOrders}
                  onChange={(e) => onSettingChange('orders', e.target.checked)}
                />
                <div className="peer h-6 w-11 rounded-full bg-gray-200 after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-gray-300 after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full peer-checked:after:border-white peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary/20"></div>
              </label>
            </div>

            {orderSyncExpanded && (
              <div className="border-t p-4 space-y-4 bg-muted/20">
                {/* Order Sync Days */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <Clock className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="font-medium">Sync Days</p>
                      <p className="text-sm text-muted-foreground">
                        Only sync orders created in the last N days
                      </p>
                    </div>
                  </div>
                  <select
                    className="rounded-md border border-input bg-background px-3 py-2 text-sm"
                    value={initialSyncDays === null ? 'all' : initialSyncDays.toString()}
                    onChange={(e) => {
                      const value = e.target.value;
                      onInitialSyncDaysChange(value === 'all' ? null : parseInt(value, 10));
                    }}
                  >
                    <option value="1">Last 1 day</option>
                    <option value="3">Last 3 days</option>
                    <option value="7">Last 7 days</option>
                    <option value="14">Last 14 days</option>
                    <option value="30">Last 30 days</option>
                    <option value="all">All time</option>
                  </select>
                </div>

                {/* Order Sync Limit */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <ShoppingCart className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="font-medium">Sync Limit</p>
                      <p className="text-sm text-muted-foreground">
                        Maximum orders to sync per batch
                      </p>
                    </div>
                  </div>
                  <select
                    className="rounded-md border border-input bg-background px-3 py-2 text-sm"
                    value={orderSyncLimit === null ? 'unlimited' : orderSyncLimit.toString()}
                    onChange={(e) => {
                      const value = e.target.value;
                      onOrderSyncLimitChange(value === 'unlimited' ? null : parseInt(value, 10));
                    }}
                  >
                    <option value="50">50 orders</option>
                    <option value="100">100 orders</option>
                    <option value="200">200 orders</option>
                    <option value="500">500 orders</option>
                    <option value="1000">1000 orders</option>
                    <option value="unlimited">Unlimited</option>
                  </select>
                </div>
              </div>
            )}
          </div>

          {/* ===== INVENTORY SYNC SECTION ===== */}
          <div className="rounded-lg border">
            <div
              className="flex items-center justify-between p-4 cursor-pointer hover:bg-muted/50 transition-colors"
              onClick={() => onInventorySyncExpandedChange(!inventorySyncExpanded)}
            >
              <div className="flex items-center gap-3">
                {inventorySyncExpanded ? (
                  <ChevronDown className="h-5 w-5 text-muted-foreground" />
                ) : (
                  <ChevronRight className="h-5 w-5 text-muted-foreground" />
                )}
                <Package className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="font-medium">Inventory Sync</p>
                  <p className="text-sm text-muted-foreground">
                    Configure inventory syncing from {channel.type}
                  </p>
                </div>
              </div>
              <label className="relative inline-flex cursor-pointer items-center" onClick={(e) => e.stopPropagation()}>
                <input
                  type="checkbox"
                  className="peer sr-only"
                  checked={autoSyncInventory}
                  onChange={(e) => onSettingChange('inventory', e.target.checked)}
                />
                <div className="peer h-6 w-11 rounded-full bg-gray-200 after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-gray-300 after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full peer-checked:after:border-white peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary/20"></div>
              </label>
            </div>

            {inventorySyncExpanded && (
              <div className="border-t p-4 space-y-4 bg-muted/20">
                {/* Inventory Sync Days */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <Clock className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="font-medium">Sync Days</p>
                      <p className="text-sm text-muted-foreground">
                        Only sync inventory updated in the last N days
                      </p>
                    </div>
                  </div>
                  <select
                    className="rounded-md border border-input bg-background px-3 py-2 text-sm"
                    value={inventorySyncDays === null ? 'all' : inventorySyncDays.toString()}
                    onChange={(e) => {
                      const value = e.target.value;
                      onInventorySyncDaysChange(value === 'all' ? null : parseInt(value, 10));
                    }}
                  >
                    <option value="1">Last 1 day</option>
                    <option value="3">Last 3 days</option>
                    <option value="7">Last 7 days</option>
                    <option value="14">Last 14 days</option>
                    <option value="30">Last 30 days</option>
                    <option value="all">All time</option>
                  </select>
                </div>

                {/* Inventory Sync Limit */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <Package className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="font-medium">Sync Limit</p>
                      <p className="text-sm text-muted-foreground">
                        Maximum inventory items to sync per batch
                      </p>
                    </div>
                  </div>
                  <select
                    className="rounded-md border border-input bg-background px-3 py-2 text-sm"
                    value={inventorySyncLimit === null ? 'unlimited' : inventorySyncLimit.toString()}
                    onChange={(e) => {
                      const value = e.target.value;
                      onInventorySyncLimitChange(value === 'unlimited' ? null : parseInt(value, 10));
                    }}
                  >
                    <option value="100">100 items</option>
                    <option value="250">250 items</option>
                    <option value="500">500 items</option>
                    <option value="1000">1000 items</option>
                    <option value="2000">2000 items</option>
                    <option value="unlimited">Unlimited</option>
                  </select>
                </div>
              </div>
            )}
          </div>

          {/* ===== PRODUCT SYNC SECTION ===== */}
          <div className="rounded-lg border">
            <div
              className="flex items-center justify-between p-4 cursor-pointer hover:bg-muted/50 transition-colors"
              onClick={() => onProductSyncExpandedChange(!productSyncExpanded)}
            >
              <div className="flex items-center gap-3">
                {productSyncExpanded ? (
                  <ChevronDown className="h-5 w-5 text-muted-foreground" />
                ) : (
                  <ChevronRight className="h-5 w-5 text-muted-foreground" />
                )}
                <Store className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="font-medium">Product Sync</p>
                  <p className="text-sm text-muted-foreground">
                    Configure product syncing from {channel.type}
                  </p>
                </div>
              </div>
              <label className="relative inline-flex cursor-pointer items-center" onClick={(e) => e.stopPropagation()}>
                <input
                  type="checkbox"
                  className="peer sr-only"
                  checked={syncProductsEnabled}
                  onChange={(e) => onSettingChange('syncProducts', e.target.checked)}
                />
                <div className="peer h-6 w-11 rounded-full bg-gray-200 after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-gray-300 after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full peer-checked:after:border-white peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary/20"></div>
              </label>
            </div>

            {productSyncExpanded && (
              <div className="border-t p-4 space-y-4 bg-muted/20">
                {/* Auto-sync Products */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <Zap className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="font-medium">Auto-sync Products</p>
                      <p className="text-sm text-muted-foreground">
                        Automatically import new products via webhooks
                      </p>
                    </div>
                  </div>
                  <label className="relative inline-flex cursor-pointer items-center">
                    <input
                      type="checkbox"
                      className="peer sr-only"
                      checked={autoSyncProducts}
                      onChange={(e) => onSettingChange('autoSyncProducts', e.target.checked)}
                    />
                    <div className="peer h-6 w-11 rounded-full bg-gray-200 after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-gray-300 after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full peer-checked:after:border-white peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary/20"></div>
                  </label>
                </div>

                {/* Product Sync Days */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <Clock className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="font-medium">Sync Days</p>
                      <p className="text-sm text-muted-foreground">
                        Only sync products updated in the last N days
                      </p>
                    </div>
                  </div>
                  <select
                    className="rounded-md border border-input bg-background px-3 py-2 text-sm"
                    value={productSyncDays === null ? 'all' : productSyncDays.toString()}
                    onChange={(e) => {
                      const value = e.target.value;
                      onProductSyncDaysChange(value === 'all' ? null : parseInt(value, 10));
                    }}
                  >
                    <option value="1">Last 1 day</option>
                    <option value="3">Last 3 days</option>
                    <option value="7">Last 7 days</option>
                    <option value="14">Last 14 days</option>
                    <option value="30">Last 30 days</option>
                    <option value="all">All time</option>
                  </select>
                </div>

                {/* Product Sync Limit */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <Store className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="font-medium">Sync Limit</p>
                      <p className="text-sm text-muted-foreground">
                        Maximum products to sync per batch
                      </p>
                    </div>
                  </div>
                  <select
                    className="rounded-md border border-input bg-background px-3 py-2 text-sm"
                    value={productSyncLimit === null ? 'unlimited' : productSyncLimit.toString()}
                    onChange={(e) => {
                      const value = e.target.value;
                      onProductSyncLimitChange(value === 'unlimited' ? null : parseInt(value, 10));
                    }}
                  >
                    <option value="10">10 products</option>
                    <option value="25">25 products</option>
                    <option value="50">50 products</option>
                    <option value="100">100 products</option>
                    <option value="250">250 products</option>
                    <option value="500">500 products</option>
                    <option value="unlimited">Unlimited</option>
                  </select>
                </div>
              </div>
            )}
          </div>

          {settingsChanged && (
            <div className="flex justify-end">
              <Button
                onClick={onSaveSettings}
                disabled={isSaving}
                leftIcon={isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : undefined}
              >
                {isSaving ? 'Saving...' : 'Save Settings'}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Manual Sync Card */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <RefreshCw className="h-5 w-5" />
            Manual Sync
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between rounded-lg border p-4">
            <div className="flex items-center gap-3">
              <ShoppingCart className="h-5 w-5 text-muted-foreground" />
              <div>
                <p className="font-medium">Sync Orders</p>
                <p className="text-sm text-muted-foreground">
                  Import orders from {channel.type}
                  {channel.lastSyncAt && (
                    <span className="block text-xs">
                      Last synced: {formatDateTime(channel.lastSyncAt)}
                    </span>
                  )}
                </p>
              </div>
            </div>
            <Button
              variant="outline"
              onClick={onSync}
              disabled={isSyncing || isSyncingInventory}
              leftIcon={
                isSyncing ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <RefreshCw className="h-4 w-4" />
                )
              }
            >
              {isSyncing ? 'Syncing...' : 'Sync Orders'}
            </Button>
          </div>

          <div className="flex items-center justify-between rounded-lg border p-4">
            <div className="flex items-center gap-3">
              <Package className="h-5 w-5 text-muted-foreground" />
              <div>
                <p className="font-medium">Sync Inventory</p>
                <p className="text-sm text-muted-foreground">
                  Pull inventory levels from {channel.type}
                  {channel.lastInventorySyncAt && (
                    <span className="block text-xs">
                      Last synced: {formatDateTime(channel.lastInventorySyncAt)}
                    </span>
                  )}
                </p>
              </div>
            </div>
            <Button
              variant="outline"
              onClick={onSyncInventory}
              disabled={isSyncing || isSyncingInventory || isSyncingProducts}
              leftIcon={
                isSyncingInventory ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Package className="h-4 w-4" />
                )
              }
            >
              {isSyncingInventory ? 'Syncing...' : 'Sync Inventory'}
            </Button>
          </div>

          <div className="flex items-center justify-between rounded-lg border p-4">
            <div className="flex items-center gap-3">
              <Store className="h-5 w-5 text-muted-foreground" />
              <div>
                <p className="font-medium">Sync Products</p>
                <p className="text-sm text-muted-foreground">
                  Import products from {channel.type} and create inventory records
                  {channel.lastProductSyncAt && (
                    <span className="block text-xs">
                      Last synced: {formatDateTime(channel.lastProductSyncAt)}
                    </span>
                  )}
                </p>
              </div>
            </div>
            <Button
              variant="outline"
              onClick={onSyncProducts}
              disabled={isSyncing || isSyncingInventory || isSyncingProducts || !syncProductsEnabled}
              leftIcon={
                isSyncingProducts ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Store className="h-4 w-4" />
                )
              }
              title={!syncProductsEnabled ? 'Enable product sync in settings first' : undefined}
            >
              {isSyncingProducts ? 'Syncing...' : 'Sync Products'}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Webhooks Info Card */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Zap className="h-5 w-5" />
            Webhooks
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="rounded-lg bg-muted/50 p-4">
            <p className="text-sm text-muted-foreground">
              Webhooks are automatically configured when you connect your store.
              They enable real-time order updates without manual syncing.
            </p>
            <div className="mt-3 space-y-2">
              <div className="flex items-center gap-2 text-sm">
                <CheckCircle className="h-4 w-4 text-success" />
                <span>Order creation webhook</span>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <CheckCircle className="h-4 w-4 text-success" />
                <span>Order update webhook</span>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <CheckCircle className="h-4 w-4 text-success" />
                <span>Order fulfillment webhook</span>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <CheckCircle className="h-4 w-4 text-success" />
                <span>Order cancellation webhook</span>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Danger Zone */}
      <Card className="border-error/50">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-error">
            <Shield className="h-5 w-5" />
            Danger Zone
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium">Disconnect Store</p>
              <p className="text-sm text-muted-foreground">
                This will disconnect your store and stop all syncing.
                Existing orders will not be deleted.
              </p>
            </div>
            <Button
              variant="danger"
              onClick={onDisconnect}
              leftIcon={<Trash2 className="h-4 w-4" />}
            >
              Disconnect
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function SyncStatusIcon({ status }: { status: string }) {
  switch (status) {
    case 'Completed':
      return <CheckCircle className="h-5 w-5 text-success" />;
    case 'InProgress':
      return <Loader2 className="h-5 w-5 animate-spin text-info" />;
    case 'Failed':
      return <AlertCircle className="h-5 w-5 text-error" />;
    default:
      return <Clock className="h-5 w-5 text-muted-foreground" />;
  }
}
