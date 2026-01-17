'use client';

import { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
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
import { useChannels, useConnectShopify, useDisconnectChannel, useSyncChannel, useUpdateChannelSettings, useSaveShopifyCredentials } from '@/hooks';
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
  Link as LinkIcon,
  ShoppingCart,
  Package,
  Zap,
  Shield,
  Key,
  Info,
} from 'lucide-react';

export default function ShopifySettingsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const connected = searchParams.get('connected');
  const error = searchParams.get('error');

  // Credentials form state
  const [apiKey, setApiKey] = useState('');
  const [apiSecret, setApiSecret] = useState('');
  const [shopifyDomain, setShopifyDomain] = useState('');

  // Settings state
  const [showDisconnectModal, setShowDisconnectModal] = useState(false);
  const [autoSyncOrders, setAutoSyncOrders] = useState(true);
  const [autoSyncInventory, setAutoSyncInventory] = useState(false);
  const [settingsChanged, setSettingsChanged] = useState(false);

  const { data: channels, isLoading, refetch } = useChannels();
  const saveCredentials = useSaveShopifyCredentials();
  const connectShopify = useConnectShopify();
  const disconnectChannel = useDisconnectChannel();
  const syncChannel = useSyncChannel();
  const updateSettings = useUpdateChannelSettings();

  // Find Shopify channel (any state)
  const shopifyChannel = channels?.find(c => c.type === 'Shopify');

  // Update local state when channel data loads
  useEffect(() => {
    if (shopifyChannel) {
      setAutoSyncOrders(shopifyChannel.autoSyncOrders ?? true);
      setAutoSyncInventory(shopifyChannel.autoSyncInventory ?? false);
    }
  }, [shopifyChannel]);

  // Refetch on mount and when connected param changes
  useEffect(() => {
    if (connected) {
      refetch();
    }
  }, [connected, refetch]);

  const handleSaveCredentials = async () => {
    if (!apiKey || !apiSecret || !shopifyDomain) return;

    try {
      await saveCredentials.mutateAsync({
        channelId: shopifyChannel?.id,
        apiKey,
        apiSecret,
        shopDomain: shopifyDomain,
      });
      // Clear form after save
      setApiKey('');
      setApiSecret('');
      setShopifyDomain('');
    } catch (err) {
      console.error('Failed to save credentials:', err);
    }
  };

  const handleConnect = async () => {
    if (!shopifyChannel) return;

    try {
      const result = await connectShopify.mutateAsync(shopifyChannel.id);
      window.location.href = result.authorizationUrl;
    } catch (err) {
      console.error('Failed to initiate Shopify connection:', err);
    }
  };

  const handleDisconnect = async () => {
    if (!shopifyChannel) return;

    try {
      await disconnectChannel.mutateAsync(shopifyChannel.id);
      setShowDisconnectModal(false);
      router.push('/channels');
    } catch (err) {
      console.error('Failed to disconnect Shopify:', err);
    }
  };

  const handleSync = async () => {
    if (!shopifyChannel) return;

    try {
      await syncChannel.mutateAsync(shopifyChannel.id);
    } catch (err) {
      console.error('Failed to sync:', err);
    }
  };

  const handleSaveSettings = async () => {
    if (!shopifyChannel) return;

    try {
      await updateSettings.mutateAsync({
        id: shopifyChannel.id,
        autoSyncOrders,
        autoSyncInventory,
      });
      setSettingsChanged(false);
    } catch (err) {
      console.error('Failed to update settings:', err);
    }
  };

  const handleSettingChange = (setting: 'orders' | 'inventory', value: boolean) => {
    if (setting === 'orders') {
      setAutoSyncOrders(value);
    } else {
      setAutoSyncInventory(value);
    }
    setSettingsChanged(true);
  };

  if (isLoading) {
    return (
      <DashboardLayout title="Shopify Settings">
        <SectionLoader />
      </DashboardLayout>
    );
  }

  // Determine which view to show
  const hasCredentials = shopifyChannel?.hasCredentials ?? false;
  const isConnected = shopifyChannel?.isConnected ?? false;

  return (
    <DashboardLayout title="Shopify Settings">
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
          <span>Shopify store connected successfully!</span>
        </div>
      )}
      {error && (
        <div className="mb-4 flex items-center gap-2 rounded-lg bg-error/10 p-4 text-error">
          <AlertCircle className="h-5 w-5" />
          <span>{decodeURIComponent(error)}</span>
        </div>
      )}
      {shopifyChannel?.lastError && !connected && (
        <div className="mb-4 flex items-center gap-2 rounded-lg bg-warning/10 p-4 text-warning">
          <AlertCircle className="h-5 w-5" />
          <span>Last connection error: {shopifyChannel.lastError}</span>
        </div>
      )}

      {isConnected && shopifyChannel ? (
        // Connected State - Show full settings
        <ConnectedView
          channel={shopifyChannel}
          autoSyncOrders={autoSyncOrders}
          autoSyncInventory={autoSyncInventory}
          settingsChanged={settingsChanged}
          onSettingChange={handleSettingChange}
          onSaveSettings={handleSaveSettings}
          onSync={handleSync}
          onDisconnect={() => setShowDisconnectModal(true)}
          isSaving={updateSettings.isPending}
          isSyncing={syncChannel.isPending}
        />
      ) : hasCredentials && shopifyChannel ? (
        // Has credentials but not connected - Show connect button
        <CredentialsSavedView
          channel={shopifyChannel}
          onConnect={handleConnect}
          onDisconnect={() => setShowDisconnectModal(true)}
          isConnecting={connectShopify.isPending}
        />
      ) : (
        // No credentials - Show credentials form
        <CredentialsFormView
          apiKey={apiKey}
          apiSecret={apiSecret}
          shopDomain={shopifyDomain}
          onApiKeyChange={setApiKey}
          onApiSecretChange={setApiSecret}
          onShopDomainChange={setShopifyDomain}
          onSave={handleSaveCredentials}
          isSaving={saveCredentials.isPending}
        />
      )}

      {/* Disconnect Confirmation Modal */}
      <Modal
        isOpen={showDisconnectModal}
        onClose={() => setShowDisconnectModal(false)}
        title="Disconnect Shopify Store"
      >
        <div className="space-y-4">
          <div className="rounded-lg bg-error/10 p-4">
            <p className="text-sm text-error">
              <strong>Warning:</strong> This action will:
            </p>
            <ul className="mt-2 list-inside list-disc text-sm text-error">
              <li>Stop all automatic order syncing</li>
              <li>Remove all webhooks from your Shopify store</li>
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

// Credentials Form View - Step 1
function CredentialsFormView({
  apiKey,
  apiSecret,
  shopDomain,
  onApiKeyChange,
  onApiSecretChange,
  onShopDomainChange,
  onSave,
  isSaving,
}: {
  apiKey: string;
  apiSecret: string;
  shopDomain: string;
  onApiKeyChange: (value: string) => void;
  onApiSecretChange: (value: string) => void;
  onShopDomainChange: (value: string) => void;
  onSave: () => void;
  isSaving: boolean;
}) {
  const isFormValid = apiKey && apiSecret && shopDomain;

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-green-100">
              <Store className="h-6 w-6 text-green-700" />
            </div>
            <div>
              <CardTitle>Connect Your Shopify Store</CardTitle>
              <p className="text-sm text-muted-foreground">
                Step 1: Enter your Shopify app credentials
              </p>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* Info Box */}
          <div className="flex gap-3 rounded-lg border border-info/30 bg-info/10 p-4">
            <Info className="h-5 w-5 text-info shrink-0 mt-0.5" />
            <div className="text-sm">
              <p className="font-medium text-info">Create a Shopify App first</p>
              <p className="text-muted-foreground mt-1">
                You need to create a custom app in your Shopify store to get API credentials.
                Go to <strong>Settings → Apps and sales channels → Develop apps</strong> in your Shopify admin.
              </p>
            </div>
          </div>

          {/* Credentials Form */}
          <div className="space-y-4">
            <Input
              label="Shop Domain"
              placeholder="your-store.myshopify.com"
              value={shopDomain}
              onChange={(e) => onShopDomainChange(e.target.value)}
              helperText="Your Shopify store domain (e.g., my-store.myshopify.com)"
              leftIcon={<LinkIcon className="h-4 w-4" />}
            />
            <Input
              label="API Key (Client ID)"
              placeholder="Enter your Shopify API Key"
              value={apiKey}
              onChange={(e) => onApiKeyChange(e.target.value)}
              helperText="Found in your Shopify app's API credentials section"
              leftIcon={<Key className="h-4 w-4" />}
            />
            <Input
              label="API Secret Key (Client Secret)"
              type="password"
              placeholder="Enter your Shopify API Secret"
              value={apiSecret}
              onChange={(e) => onApiSecretChange(e.target.value)}
              helperText="Found in your Shopify app's API credentials section"
              leftIcon={<Key className="h-4 w-4" />}
            />
            <Button
              onClick={onSave}
              disabled={!isFormValid || isSaving}
              leftIcon={
                isSaving ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Key className="h-4 w-4" />
                )
              }
              className="w-full md:w-auto"
            >
              {isSaving ? 'Saving...' : 'Save Credentials'}
            </Button>
          </div>

          {/* Setup Instructions */}
          <div className="rounded-lg border p-4">
            <h4 className="mb-3 font-medium">How to get your Shopify API credentials:</h4>
            <ol className="space-y-2 text-sm text-muted-foreground">
              <li className="flex items-start gap-2">
                <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-primary text-xs text-primary-foreground">1</span>
                <span>Log in to your Shopify admin panel</span>
              </li>
              <li className="flex items-start gap-2">
                <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-primary text-xs text-primary-foreground">2</span>
                <span>Go to <strong>Settings → Apps and sales channels</strong></span>
              </li>
              <li className="flex items-start gap-2">
                <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-primary text-xs text-primary-foreground">3</span>
                <span>Click <strong>Develop apps</strong> and then <strong>Create an app</strong></span>
              </li>
              <li className="flex items-start gap-2">
                <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-primary text-xs text-primary-foreground">4</span>
                <span>Configure the app scopes (read_orders, write_orders, read_products, etc.)</span>
              </li>
              <li className="flex items-start gap-2">
                <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-primary text-xs text-primary-foreground">5</span>
                <span>Install the app to your store and copy the <strong>API Key</strong> and <strong>API Secret Key</strong></span>
              </li>
            </ol>
          </div>

          {/* Features List */}
          <div className="rounded-lg bg-muted/50 p-4">
            <h4 className="mb-3 font-medium">What you get with Shopify integration:</h4>
            <div className="grid gap-3 md:grid-cols-2">
              <div className="flex items-start gap-2">
                <CheckCircle className="mt-0.5 h-4 w-4 text-success" />
                <div>
                  <p className="text-sm font-medium">Automatic Order Import</p>
                  <p className="text-xs text-muted-foreground">
                    Orders sync automatically via webhooks
                  </p>
                </div>
              </div>
              <div className="flex items-start gap-2">
                <CheckCircle className="mt-0.5 h-4 w-4 text-success" />
                <div>
                  <p className="text-sm font-medium">Inventory Sync</p>
                  <p className="text-xs text-muted-foreground">
                    Keep stock levels in sync across platforms
                  </p>
                </div>
              </div>
              <div className="flex items-start gap-2">
                <CheckCircle className="mt-0.5 h-4 w-4 text-success" />
                <div>
                  <p className="text-sm font-medium">Shipment Updates</p>
                  <p className="text-xs text-muted-foreground">
                    Push tracking info back to Shopify
                  </p>
                </div>
              </div>
              <div className="flex items-start gap-2">
                <CheckCircle className="mt-0.5 h-4 w-4 text-success" />
                <div>
                  <p className="text-sm font-medium">Real-time Webhooks</p>
                  <p className="text-xs text-muted-foreground">
                    Instant notifications for order changes
                  </p>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
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
                Step 2: Connect to Shopify to complete the setup
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
                  Your API credentials have been saved. Click the button below to connect to Shopify
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
              {isConnecting ? 'Connecting...' : 'Connect to Shopify'}
            </Button>
            <Button variant="outline" onClick={onDisconnect}>
              <Trash2 className="h-4 w-4 mr-2" />
              Remove Credentials
            </Button>
          </div>

          <div className="rounded-lg bg-muted/50 p-4">
            <h4 className="mb-2 font-medium">What happens next?</h4>
            <ul className="space-y-1 text-sm text-muted-foreground">
              <li>• You&apos;ll be redirected to Shopify to log in</li>
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
  settingsChanged,
  onSettingChange,
  onSaveSettings,
  onSync,
  onDisconnect,
  isSaving,
  isSyncing,
}: {
  channel: Channel;
  autoSyncOrders: boolean;
  autoSyncInventory: boolean;
  settingsChanged: boolean;
  onSettingChange: (setting: 'orders' | 'inventory', value: boolean) => void;
  onSaveSettings: () => void;
  onSync: () => void;
  onDisconnect: () => void;
  isSaving: boolean;
  isSyncing: boolean;
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
              Open Shopify Admin
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
          <div className="flex items-center justify-between rounded-lg border p-4">
            <div className="flex items-center gap-3">
              <ShoppingCart className="h-5 w-5 text-muted-foreground" />
              <div>
                <p className="font-medium">Auto-sync Orders</p>
                <p className="text-sm text-muted-foreground">
                  Automatically import new orders from Shopify via webhooks
                </p>
              </div>
            </div>
            <label className="relative inline-flex cursor-pointer items-center">
              <input
                type="checkbox"
                className="peer sr-only"
                checked={autoSyncOrders}
                onChange={(e) => onSettingChange('orders', e.target.checked)}
              />
              <div className="peer h-6 w-11 rounded-full bg-gray-200 after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-gray-300 after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full peer-checked:after:border-white peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary/20"></div>
            </label>
          </div>

          <div className="flex items-center justify-between rounded-lg border p-4">
            <div className="flex items-center gap-3">
              <Package className="h-5 w-5 text-muted-foreground" />
              <div>
                <p className="font-medium">Auto-sync Inventory</p>
                <p className="text-sm text-muted-foreground">
                  Push inventory updates back to Shopify when stock changes
                </p>
              </div>
            </div>
            <label className="relative inline-flex cursor-pointer items-center">
              <input
                type="checkbox"
                className="peer sr-only"
                checked={autoSyncInventory}
                onChange={(e) => onSettingChange('inventory', e.target.checked)}
              />
              <div className="peer h-6 w-11 rounded-full bg-gray-200 after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-gray-300 after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full peer-checked:after:border-white peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary/20"></div>
            </label>
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
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium">Sync Orders Now</p>
              <p className="text-sm text-muted-foreground">
                Manually trigger a sync to import all orders from Shopify.
                This may take a few minutes depending on the number of orders.
              </p>
            </div>
            <Button
              variant="outline"
              onClick={onSync}
              disabled={isSyncing}
              leftIcon={
                isSyncing ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <RefreshCw className="h-4 w-4" />
                )
              }
            >
              {isSyncing ? 'Syncing...' : 'Sync Now'}
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
              Webhooks are automatically configured when you connect your Shopify store.
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
                This will disconnect your Shopify store and stop all syncing.
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
