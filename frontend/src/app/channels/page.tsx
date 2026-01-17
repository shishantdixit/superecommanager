'use client';

import { useState } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { DashboardLayout } from '@/components/layout';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Button,
  Badge,
  SectionLoader,
  Modal,
} from '@/components/ui';
import { formatDateTime } from '@/lib/utils';
import { useChannels, useDisconnectChannel, useSyncChannel } from '@/hooks';
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
  Settings,
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
  const router = useRouter();
  const searchParams = useSearchParams();
  const connected = searchParams.get('connected');
  const error = searchParams.get('error');

  const [showConnectModal, setShowConnectModal] = useState(false);
  const [showDisconnectModal, setShowDisconnectModal] = useState<string | null>(null);

  const { data: channels, isLoading, error: loadError } = useChannels();
  const disconnectChannel = useDisconnectChannel();
  const syncChannel = useSyncChannel();

  // Handle connecting to Shopify - redirect to settings page
  const handleConnectShopify = () => {
    router.push('/channels/shopify');
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
              isConnected={channels?.some(c => c.type === 'Shopify' && c.isConnected)}
              hasCredentials={channels?.some(c => c.type === 'Shopify' && c.hasCredentials)}
              onConnect={handleConnectShopify}
              settingsUrl={
                channels?.find(c => c.type === 'Shopify')?.id
                  ? `/channels/${channels.find(c => c.type === 'Shopify')!.id}`
                  : '/channels/shopify'
              }
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

      {/* Connect Channel Modal */}
      <Modal
        isOpen={showConnectModal}
        onClose={() => setShowConnectModal(false)}
        title="Connect Sales Channel"
      >
        <div className="space-y-4">
          <p className="text-muted-foreground">
            Select a sales channel to connect.
          </p>
          <div className="grid gap-3">
            <button
              onClick={() => {
                setShowConnectModal(false);
                handleConnectShopify();
              }}
              className="flex items-center gap-3 rounded-lg border p-4 text-left hover:bg-muted transition-colors"
            >
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-green-100">
                <Store className="h-5 w-5 text-green-700" />
              </div>
              <div>
                <h4 className="font-medium">Shopify</h4>
                <p className="text-xs text-muted-foreground">Connect your Shopify store</p>
              </div>
            </button>
            <button
              disabled
              className="flex items-center gap-3 rounded-lg border p-4 text-left opacity-50 cursor-not-allowed"
            >
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-orange-100">
                <Store className="h-5 w-5 text-orange-700" />
              </div>
              <div>
                <h4 className="font-medium">Amazon</h4>
                <p className="text-xs text-muted-foreground">Coming Soon</p>
              </div>
            </button>
            <button
              disabled
              className="flex items-center gap-3 rounded-lg border p-4 text-left opacity-50 cursor-not-allowed"
            >
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-yellow-100">
                <Store className="h-5 w-5 text-yellow-700" />
              </div>
              <div>
                <h4 className="font-medium">Flipkart</h4>
                <p className="text-xs text-muted-foreground">Coming Soon</p>
              </div>
            </button>
          </div>
          <div className="flex justify-end">
            <Button variant="outline" onClick={() => setShowConnectModal(false)}>
              Cancel
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
  const isConnected = channel.isConnected ?? channel.isActive;

  return (
    <div className="flex items-center justify-between rounded-lg border p-4">
      <div className="flex items-center gap-4">
        <div className={`flex h-12 w-12 items-center justify-center rounded-lg ${config.bgColor}`}>
          <Store className={`h-6 w-6 ${config.color}`} />
        </div>
        <div>
          <div className="flex items-center gap-2">
            <h3 className="font-medium">{channel.name}</h3>
            {isConnected ? (
              <Badge variant="success" size="sm">Connected</Badge>
            ) : channel.hasCredentials ? (
              <Badge variant="warning" size="sm">Setup Required</Badge>
            ) : (
              <Badge variant="default" size="sm">Not Connected</Badge>
            )}
            {isConnected && <SyncStatusBadge status={channel.syncStatus} />}
          </div>
          <p className="text-sm text-muted-foreground">
            {channel.storeName || channel.storeUrl || config.name}
          </p>
          <div className="mt-1 flex items-center gap-4 text-xs text-muted-foreground">
            <span>{channel.totalOrders.toLocaleString()} orders</span>
            {channel.lastSyncAt && (
              <span>Last sync: {formatDateTime(channel.lastSyncAt)}</span>
            )}
            {channel.lastError && (
              <span className="text-error">Error: {channel.lastError}</span>
            )}
          </div>
        </div>
      </div>
      <div className="flex items-center gap-2">
        {channel.storeUrl && (
          <a
            href={channel.storeUrl.startsWith('http') ? channel.storeUrl : `https://${channel.storeUrl}`}
            target="_blank"
            rel="noopener noreferrer"
            className="rounded p-2 hover:bg-muted"
            title="Open Store"
          >
            <ExternalLink className="h-4 w-4 text-muted-foreground" />
          </a>
        )}
        {isConnected && (
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
        )}
        <Link href={`/channels/${channel.id}`}>
          <Button
            variant="outline"
            size="sm"
            leftIcon={<Settings className="h-4 w-4" />}
          >
            {isConnected ? 'Settings' : 'Complete Setup'}
          </Button>
        </Link>
        <Button
          variant="outline"
          size="sm"
          onClick={onDisconnect}
          leftIcon={<Trash2 className="h-4 w-4" />}
          className="text-error hover:bg-error/10"
        >
          Remove
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
  hasCredentials,
  comingSoon,
  onConnect,
  settingsUrl,
}: {
  name: string;
  description: string;
  color: string;
  isConnected?: boolean;
  hasCredentials?: boolean;
  comingSoon?: boolean;
  onConnect?: () => void;
  settingsUrl?: string;
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
      <div className="mt-4 flex items-center gap-2">
        {comingSoon ? (
          <Badge variant="default" size="sm">
            Coming Soon
          </Badge>
        ) : isConnected ? (
          <>
            <Badge variant="success" size="sm">
              <CheckCircle className="mr-1 h-3 w-3" />
              Connected
            </Badge>
            {settingsUrl && (
              <Link href={settingsUrl}>
                <Button size="sm" variant="ghost" className="h-6 px-2">
                  <Settings className="h-3 w-3" />
                </Button>
              </Link>
            )}
          </>
        ) : hasCredentials ? (
          <>
            <Badge variant="warning" size="sm">
              <Clock className="mr-1 h-3 w-3" />
              Setup Required
            </Badge>
            {settingsUrl && (
              <Link href={settingsUrl}>
                <Button size="sm" variant="outline">
                  Complete Setup
                </Button>
              </Link>
            )}
          </>
        ) : (
          <Link href={settingsUrl || '#'}>
            <Button size="sm" variant="outline" onClick={!settingsUrl ? onConnect : undefined}>
              Connect
            </Button>
          </Link>
        )}
      </div>
    </div>
  );
}
