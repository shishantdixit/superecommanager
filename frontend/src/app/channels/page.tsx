'use client';

import { useState } from 'react';
import { useSearchParams } from 'next/navigation';
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
import { useChannels, useConnectShopify, useDisconnectChannel, useSyncChannel } from '@/hooks';
import type { Channel, ChannelType, ChannelSyncStatus } from '@/services/channels.service';
import {
  Store,
  Plus,
  RefreshCw,
  Trash2,
  ExternalLink,
  CheckCircle,
  AlertCircle,
  Clock,
  Loader2,
  ShoppingBag,
  Package,
} from 'lucide-react';

// Channel type configurations
const channelConfig: Record<ChannelType, { name: string; color: string; bgColor: string; logo?: string }> = {
  Shopify: { name: 'Shopify', color: 'text-green-700', bgColor: 'bg-green-100' },
  Amazon: { name: 'Amazon', color: 'text-orange-700', bgColor: 'bg-orange-100' },
  Flipkart: { name: 'Flipkart', color: 'text-yellow-700', bgColor: 'bg-yellow-100' },
  Meesho: { name: 'Meesho', color: 'text-pink-700', bgColor: 'bg-pink-100' },
  WooCommerce: { name: 'WooCommerce', color: 'text-purple-700', bgColor: 'bg-purple-100' },
  Custom: { name: 'Custom', color: 'text-gray-700', bgColor: 'bg-gray-100' },
};

export default function ChannelsPage() {
  const searchParams = useSearchParams();
  const connected = searchParams.get('connected');
  const error = searchParams.get('error');

  const [showConnectModal, setShowConnectModal] = useState(false);
  const [showDisconnectModal, setShowDisconnectModal] = useState<string | null>(null);
  const [shopifyDomain, setShopifyDomain] = useState('');

  const { data: channels, isLoading, error: loadError } = useChannels();
  const connectShopify = useConnectShopify();
  const disconnectChannel = useDisconnectChannel();
  const syncChannel = useSyncChannel();

  const handleConnectShopify = async () => {
    if (!shopifyDomain) return;

    try {
      const result = await connectShopify.mutateAsync({ shopDomain: shopifyDomain });
      // Redirect to Shopify OAuth
      window.location.href = result.authorizationUrl;
    } catch (err) {
      console.error('Failed to initiate Shopify connection:', err);
    }
  };

  const handleDisconnect = async (id: string) => {
    try {
      await disconnectChannel.mutateAsync(id);
      setShowDisconnectModal(null);
    } catch (err) {
      console.error('Failed to disconnect channel:', err);
    }
  };

  const handleSync = async (id: string) => {
    try {
      await syncChannel.mutateAsync(id);
    } catch (err) {
      console.error('Failed to sync channel:', err);
    }
  };

  // Count stats
  const activeChannels = channels?.filter(c => c.isActive).length ?? 0;
  const totalOrders = channels?.reduce((sum, c) => sum + c.totalOrders, 0) ?? 0;

  return (
    <DashboardLayout title="Sales Channels">
      {/* Success/Error Messages */}
      {connected && (
        <div className="mb-4 flex items-center gap-2 rounded-lg bg-success/10 p-4 text-success">
          <CheckCircle className="h-5 w-5" />
          <span>Channel connected successfully!</span>
        </div>
      )}
      {error && (
        <div className="mb-4 flex items-center gap-2 rounded-lg bg-error/10 p-4 text-error">
          <AlertCircle className="h-5 w-5" />
          <span>{decodeURIComponent(error)}</span>
        </div>
      )}

      {/* Stats Cards */}
      <div className="mb-6 grid gap-4 md:grid-cols-3">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Connected Channels</p>
                <p className="text-2xl font-bold">{activeChannels}</p>
              </div>
              <Store className="h-8 w-8 text-primary" />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Total Orders</p>
                <p className="text-2xl font-bold">{totalOrders.toLocaleString()}</p>
              </div>
              <Package className="h-8 w-8 text-info" />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Available Integrations</p>
                <p className="text-2xl font-bold">4</p>
              </div>
              <ShoppingBag className="h-8 w-8 text-success" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Connected Channels */}
      <Card className="mb-6">
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>Connected Channels</CardTitle>
          <Button
            size="sm"
            leftIcon={<Plus className="h-4 w-4" />}
            onClick={() => setShowConnectModal(true)}
          >
            Connect Channel
          </Button>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <SectionLoader />
          ) : loadError ? (
            <div className="py-12 text-center text-error">
              Failed to load channels. Please try again.
            </div>
          ) : !channels || channels.length === 0 ? (
            <div className="py-12 text-center">
              <Store className="mx-auto h-12 w-12 text-muted-foreground" />
              <p className="mt-4 text-lg font-medium">No channels connected</p>
              <p className="text-muted-foreground">
                Connect your first sales channel to start importing orders.
              </p>
              <Button
                className="mt-4"
                leftIcon={<Plus className="h-4 w-4" />}
                onClick={() => setShowConnectModal(true)}
              >
                Connect Channel
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              {channels.map((channel) => (
                <ChannelCard
                  key={channel.id}
                  channel={channel}
                  onSync={() => handleSync(channel.id)}
                  onDisconnect={() => setShowDisconnectModal(channel.id)}
                  isSyncing={syncChannel.isPending && syncChannel.variables === channel.id}
                />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Available Integrations */}
      <Card>
        <CardHeader>
          <CardTitle>Available Integrations</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <IntegrationCard
              name="Shopify"
              description="Connect your Shopify store"
              color="bg-green-100 text-green-700"
              isConnected={channels?.some(c => c.type === 'Shopify' && c.isActive)}
              onConnect={() => setShowConnectModal(true)}
            />
            <IntegrationCard
              name="Amazon"
              description="Sell on Amazon marketplace"
              color="bg-orange-100 text-orange-700"
              isConnected={channels?.some(c => c.type === 'Amazon' && c.isActive)}
              comingSoon
            />
            <IntegrationCard
              name="Flipkart"
              description="Connect Flipkart seller account"
              color="bg-yellow-100 text-yellow-700"
              isConnected={channels?.some(c => c.type === 'Flipkart' && c.isActive)}
              comingSoon
            />
            <IntegrationCard
              name="Meesho"
              description="Integrate with Meesho"
              color="bg-pink-100 text-pink-700"
              isConnected={channels?.some(c => c.type === 'Meesho' && c.isActive)}
              comingSoon
            />
          </div>
        </CardContent>
      </Card>

      {/* Connect Modal */}
      <Modal
        isOpen={showConnectModal}
        onClose={() => {
          setShowConnectModal(false);
          setShopifyDomain('');
        }}
        title="Connect Shopify Store"
      >
        <div className="space-y-4">
          <p className="text-muted-foreground">
            Enter your Shopify store domain to connect your store.
          </p>
          <Input
            label="Store Domain"
            placeholder="your-store.myshopify.com"
            value={shopifyDomain}
            onChange={(e) => setShopifyDomain(e.target.value)}
            helperText="Enter just the domain without https://"
          />
          <div className="flex justify-end gap-2">
            <Button
              variant="outline"
              onClick={() => {
                setShowConnectModal(false);
                setShopifyDomain('');
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleConnectShopify}
              disabled={!shopifyDomain || connectShopify.isPending}
              leftIcon={connectShopify.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : undefined}
            >
              {connectShopify.isPending ? 'Connecting...' : 'Connect'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Disconnect Confirmation Modal */}
      <Modal
        isOpen={!!showDisconnectModal}
        onClose={() => setShowDisconnectModal(null)}
        title="Disconnect Channel"
      >
        <div className="space-y-4">
          <p className="text-muted-foreground">
            Are you sure you want to disconnect this channel? This will stop order syncing
            but will not delete existing orders.
          </p>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setShowDisconnectModal(null)}>
              Cancel
            </Button>
            <Button
              variant="danger"
              onClick={() => showDisconnectModal && handleDisconnect(showDisconnectModal)}
              disabled={disconnectChannel.isPending}
              leftIcon={disconnectChannel.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : undefined}
            >
              {disconnectChannel.isPending ? 'Disconnecting...' : 'Disconnect'}
            </Button>
          </div>
        </div>
      </Modal>
    </DashboardLayout>
  );
}

function ChannelCard({
  channel,
  onSync,
  onDisconnect,
  isSyncing,
}: {
  channel: Channel;
  onSync: () => void;
  onDisconnect: () => void;
  isSyncing: boolean;
}) {
  const config = channelConfig[channel.type] || channelConfig.Custom;

  return (
    <div className="flex items-center justify-between rounded-lg border p-4">
      <div className="flex items-center gap-4">
        <div className={`flex h-12 w-12 items-center justify-center rounded-lg ${config.bgColor}`}>
          <Store className={`h-6 w-6 ${config.color}`} />
        </div>
        <div>
          <div className="flex items-center gap-2">
            <h3 className="font-medium">{channel.name}</h3>
            <Badge variant={channel.isActive ? 'success' : 'default'} size="sm">
              {channel.isActive ? 'Active' : 'Inactive'}
            </Badge>
            <SyncStatusBadge status={channel.syncStatus} />
          </div>
          <p className="text-sm text-muted-foreground">
            {channel.storeName || channel.storeUrl || config.name}
          </p>
          <div className="mt-1 flex items-center gap-4 text-xs text-muted-foreground">
            <span>{channel.totalOrders.toLocaleString()} orders</span>
            {channel.lastSyncAt && (
              <span>Last sync: {formatDateTime(channel.lastSyncAt)}</span>
            )}
          </div>
        </div>
      </div>
      <div className="flex items-center gap-2">
        {channel.storeUrl && (
          <a
            href={`https://${channel.storeUrl}`}
            target="_blank"
            rel="noopener noreferrer"
            className="rounded p-2 hover:bg-muted"
            title="Open Store"
          >
            <ExternalLink className="h-4 w-4 text-muted-foreground" />
          </a>
        )}
        <Button
          variant="outline"
          size="sm"
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
          {isSyncing ? 'Syncing...' : 'Sync'}
        </Button>
        <Button
          variant="outline"
          size="sm"
          onClick={onDisconnect}
          leftIcon={<Trash2 className="h-4 w-4" />}
          className="text-error hover:bg-error/10"
        >
          Disconnect
        </Button>
      </div>
    </div>
  );
}

function SyncStatusBadge({ status }: { status: ChannelSyncStatus }) {
  const config: Record<ChannelSyncStatus, { variant: 'success' | 'warning' | 'error' | 'info' | 'default'; icon: React.ReactNode }> = {
    NotStarted: { variant: 'default', icon: <Clock className="h-3 w-3" /> },
    InProgress: { variant: 'info', icon: <Loader2 className="h-3 w-3 animate-spin" /> },
    Completed: { variant: 'success', icon: <CheckCircle className="h-3 w-3" /> },
    Failed: { variant: 'error', icon: <AlertCircle className="h-3 w-3" /> },
  };

  const statusConfig = config[status];

  return (
    <Badge variant={statusConfig.variant} size="sm" className="gap-1">
      {statusConfig.icon}
      {status === 'NotStarted' ? 'Never Synced' : status}
    </Badge>
  );
}

function IntegrationCard({
  name,
  description,
  color,
  isConnected,
  comingSoon,
  onConnect,
}: {
  name: string;
  description: string;
  color: string;
  isConnected?: boolean;
  comingSoon?: boolean;
  onConnect?: () => void;
}) {
  return (
    <div className="rounded-lg border p-4">
      <div className="flex items-center gap-3">
        <div className={`flex h-10 w-10 items-center justify-center rounded-lg ${color}`}>
          <Store className="h-5 w-5" />
        </div>
        <div>
          <h4 className="font-medium">{name}</h4>
          <p className="text-xs text-muted-foreground">{description}</p>
        </div>
      </div>
      <div className="mt-4">
        {comingSoon ? (
          <Badge variant="default" size="sm">
            Coming Soon
          </Badge>
        ) : isConnected ? (
          <Badge variant="success" size="sm">
            <CheckCircle className="mr-1 h-3 w-3" />
            Connected
          </Badge>
        ) : (
          <Button size="sm" variant="outline" onClick={onConnect}>
            Connect
          </Button>
        )}
      </div>
    </div>
  );
}
