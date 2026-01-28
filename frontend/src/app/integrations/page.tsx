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
  Mail,
  MessageSquare,
  CreditCard,
  CheckCircle2,
  AlertCircle,
  Settings,
  Plus,
  ExternalLink,
} from 'lucide-react';
import ShiprocketModal from '@/components/integrations/ShiprocketModal';
import CourierSettingsModal from '@/components/integrations/CourierSettingsModal';
import { courierService, CourierAccountDto } from '@/services/courier.service';
import { toast } from 'sonner';

// Integration categories
type IntegrationCategory = 'channels' | 'shipping' | 'notifications' | 'payments';

interface Integration {
  id: string;
  name: string;
  description: string;
  logo?: string;
  category: IntegrationCategory;
  isConnected: boolean;
  isActive?: boolean;
  lastSync?: string;
  config?: Record<string, any>;
}

// Sample integrations data (will be replaced with API calls)
const integrations: Integration[] = [
  // Sales Channels
  {
    id: 'shopify',
    name: 'Shopify',
    description: 'Connect your Shopify store to sync products and orders',
    category: 'channels',
    isConnected: false,
  },
  {
    id: 'amazon',
    name: 'Amazon',
    description: 'Manage Amazon seller central orders and inventory',
    category: 'channels',
    isConnected: false,
  },
  {
    id: 'flipkart',
    name: 'Flipkart',
    description: 'Connect Flipkart seller account for order management',
    category: 'channels',
    isConnected: false,
  },
  {
    id: 'meesho',
    name: 'Meesho',
    description: 'Sync Meesho supplier panel orders automatically',
    category: 'channels',
    isConnected: false,
  },
  {
    id: 'woocommerce',
    name: 'WooCommerce',
    description: 'Connect WordPress WooCommerce stores',
    category: 'channels',
    isConnected: false,
  },
  {
    id: 'custom',
    name: 'Custom Website',
    description: 'Connect any custom e-commerce website via API',
    category: 'channels',
    isConnected: false,
  },

  // Shipping Partners
  {
    id: 'shiprocket',
    name: 'Shiprocket',
    description: 'Automated shipping with 17+ courier partners',
    category: 'shipping',
    isConnected: false,
  },
  {
    id: 'bluedart',
    name: 'Blue Dart',
    description: 'Direct integration with Blue Dart Express',
    category: 'shipping',
    isConnected: false,
  },
  {
    id: 'delhivery',
    name: 'Delhivery',
    description: 'Connect with Delhivery for logistics',
    category: 'shipping',
    isConnected: false,
  },
  {
    id: 'dtdc',
    name: 'DTDC',
    description: 'DTDC courier services integration',
    category: 'shipping',
    isConnected: false,
  },
  {
    id: 'ekart',
    name: 'Ekart',
    description: 'Flipkart eKart Logistics',
    category: 'shipping',
    isConnected: false,
  },

  // Email & SMS
  {
    id: 'smtp',
    name: 'SMTP / Email',
    description: 'Configure SMTP for transactional emails',
    category: 'notifications',
    isConnected: false,
  },
  {
    id: 'sendgrid',
    name: 'SendGrid',
    description: 'SendGrid email delivery service',
    category: 'notifications',
    isConnected: false,
  },
  {
    id: 'twilio',
    name: 'Twilio SMS',
    description: 'Send SMS notifications via Twilio',
    category: 'notifications',
    isConnected: false,
  },
  {
    id: 'msg91',
    name: 'MSG91',
    description: 'Indian SMS gateway for notifications',
    category: 'notifications',
    isConnected: false,
  },
  {
    id: 'whatsapp-business',
    name: 'WhatsApp Business',
    description: 'WhatsApp Business API for messaging',
    category: 'notifications',
    isConnected: false,
  },

  // Payment Gateways
  {
    id: 'razorpay',
    name: 'Razorpay',
    description: 'Accept payments via Razorpay',
    category: 'payments',
    isConnected: false,
  },
  {
    id: 'paytm',
    name: 'Paytm',
    description: 'Paytm payment gateway integration',
    category: 'payments',
    isConnected: false,
  },
  {
    id: 'phonepe',
    name: 'PhonePe',
    description: 'PhonePe payment solutions',
    category: 'payments',
    isConnected: false,
  },
  {
    id: 'cashfree',
    name: 'Cashfree',
    description: 'Cashfree Payments integration',
    category: 'payments',
    isConnected: false,
  },
  {
    id: 'stripe',
    name: 'Stripe',
    description: 'International payments with Stripe',
    category: 'payments',
    isConnected: false,
  },
];

const categories = [
  {
    id: 'channels' as const,
    name: 'Sales Channels',
    description: 'Connect your online stores and marketplaces',
    icon: ShoppingCart,
    color: 'text-blue-600',
    bgColor: 'bg-blue-50',
  },
  {
    id: 'shipping' as const,
    name: 'Shipping Partners',
    description: 'Integrate with courier and logistics providers',
    icon: Truck,
    color: 'text-green-600',
    bgColor: 'bg-green-50',
  },
  {
    id: 'notifications' as const,
    name: 'Email & SMS',
    description: 'Configure email and SMS notification services',
    icon: Mail,
    color: 'text-purple-600',
    bgColor: 'bg-purple-50',
  },
  {
    id: 'payments' as const,
    name: 'Payment Gateways',
    description: 'Set up payment processing services',
    icon: CreditCard,
    color: 'text-orange-600',
    bgColor: 'bg-orange-50',
  },
];

export default function IntegrationsPage() {
  const [selectedCategory, setSelectedCategory] = useState<IntegrationCategory>('channels');
  const [showConnectModal, setShowConnectModal] = useState(false);
  const [showSettingsModal, setShowSettingsModal] = useState(false);
  const [selectedIntegration, setSelectedIntegration] = useState<Integration | null>(null);
  const [selectedAccountId, setSelectedAccountId] = useState<string | null>(null);
  const [courierAccounts, setCourierAccounts] = useState<CourierAccountDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  // Fetch courier accounts on mount
  useEffect(() => {
    loadCourierAccounts();
  }, []);

  const loadCourierAccounts = async () => {
    try {
      setIsLoading(true);
      const accounts = await courierService.getCourierAccounts();
      setCourierAccounts(accounts);
    } catch (error) {
      console.error('Failed to load courier accounts:', error);
      toast.error('Failed to load courier accounts');
    } finally {
      setIsLoading(false);
    }
  };

  // Update integrations list with actual courier account status
  const updatedIntegrations = integrations.map((integration) => {
    if (integration.category === 'shipping') {
      const account = courierAccounts.find(
        (acc) => acc.courierType.toLowerCase() === integration.id.toLowerCase()
      );
      return {
        ...integration,
        isConnected: !!account && account.isConnected,
        isActive: account?.isActive,
        lastSync: account?.lastConnectedAt,
      };
    }
    return integration;
  });

  const filteredIntegrations = updatedIntegrations.filter((i) => i.category === selectedCategory);
  const connectedCount = updatedIntegrations.filter((i) => i.isConnected).length;

  const handleConnect = (integration: Integration) => {
    setSelectedIntegration(integration);
    // For courier integrations, we'll use specific modals
    if (integration.category === 'shipping' && integration.id === 'shiprocket') {
      // Shiprocket modal will be shown via state check below
      setShowConnectModal(true);
    } else {
      // For other integrations, show placeholder modal
      setShowConnectModal(true);
    }
  };

  const handleDisconnect = async (integration: Integration) => {
    if (integration.category === 'shipping') {
      const account = courierAccounts.find(
        (acc) => acc.courierType.toLowerCase() === integration.id.toLowerCase()
      );
      if (account) {
        try {
          await courierService.deleteCourierAccount(account.id);
          toast.success(`${integration.name} disconnected successfully`);
          loadCourierAccounts();
        } catch (error) {
          toast.error(`Failed to disconnect ${integration.name}`);
        }
      }
    }
  };

  const handleConfigure = (integration: Integration) => {
    if (integration.category === 'shipping') {
      const account = courierAccounts.find(
        (acc) => acc.courierType.toLowerCase() === integration.id.toLowerCase()
      );
      if (account) {
        setSelectedIntegration(integration);
        setSelectedAccountId(account.id);
        setShowSettingsModal(true);
      }
    } else {
      // TODO: Open configuration modal for other integration types
      console.log('Configure:', integration.name);
      toast.info('Configuration coming soon');
    }
  };

  const handleConnectionSuccess = () => {
    loadCourierAccounts();
  };

  return (
    <DashboardLayout title="Integrations">
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold">Integrations</h1>
            <p className="text-muted-foreground">
              Connect your business tools and services
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Badge variant="default">{connectedCount} Connected</Badge>
          </div>
        </div>

        {/* Category Tabs */}
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
          {categories.map((category) => {
            const Icon = category.icon;
            const count = integrations.filter(
              (i) => i.category === category.id && i.isConnected
            ).length;
            const total = integrations.filter((i) => i.category === category.id).length;

            return (
              <Card
                key={category.id}
                className={`cursor-pointer transition-all hover:shadow-md ${
                  selectedCategory === category.id ? 'ring-2 ring-primary' : ''
                }`}
                onClick={() => setSelectedCategory(category.id)}
              >
                <CardContent className="p-4">
                  <div className="flex items-start gap-3">
                    <div className={`rounded-lg p-2 ${category.bgColor}`}>
                      <Icon className={`h-5 w-5 ${category.color}`} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <h3 className="font-semibold">{category.name}</h3>
                      <p className="text-xs text-muted-foreground mt-1">
                        {count}/{total} connected
                      </p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>

        {/* Integrations List */}
        <div>
          <div className="mb-4">
            <h2 className="text-lg font-semibold">
              {categories.find((c) => c.id === selectedCategory)?.name}
            </h2>
            <p className="text-sm text-muted-foreground">
              {categories.find((c) => c.id === selectedCategory)?.description}
            </p>
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
            {filteredIntegrations.map((integration) => (
              <Card key={integration.id} className="hover:shadow-md transition-shadow">
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <div className="flex items-center gap-3">
                      <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-muted">
                        <ShoppingCart className="h-6 w-6 text-muted-foreground" />
                      </div>
                      <div>
                        <CardTitle className="text-base">{integration.name}</CardTitle>
                        {integration.isConnected && (
                          <Badge variant="success" className="mt-1">
                            <CheckCircle2 className="mr-1 h-3 w-3" />
                            Connected
                          </Badge>
                        )}
                      </div>
                    </div>
                  </div>
                </CardHeader>
                <CardContent className="space-y-4">
                  <p className="text-sm text-muted-foreground">
                    {integration.description}
                  </p>

                  {integration.isConnected ? (
                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleConfigure(integration)}
                        className="flex-1"
                      >
                        <Settings className="mr-2 h-4 w-4" />
                        Configure
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleDisconnect(integration)}
                        className="text-error hover:bg-error/10"
                      >
                        Disconnect
                      </Button>
                    </div>
                  ) : (
                    <Button
                      variant="primary"
                      size="sm"
                      onClick={() => handleConnect(integration)}
                      className="w-full"
                    >
                      <Plus className="mr-2 h-4 w-4" />
                      Connect
                    </Button>
                  )}

                  {integration.lastSync && (
                    <p className="text-xs text-muted-foreground">
                      Last synced: {integration.lastSync}
                    </p>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
        </div>

        {/* Empty State */}
        {filteredIntegrations.length === 0 && (
          <Card>
            <CardContent className="flex flex-col items-center justify-center py-12">
              <AlertCircle className="h-12 w-12 text-muted-foreground mb-4" />
              <p className="text-lg font-medium">No integrations available</p>
              <p className="text-sm text-muted-foreground">
                Check back later for more integrations
              </p>
            </CardContent>
          </Card>
        )}
      </div>

      {/* Connection Modals */}
      {showConnectModal && selectedIntegration && (
        <>
          {selectedIntegration.id === 'shiprocket' ? (
            <ShiprocketModal
              isOpen={showConnectModal}
              onClose={() => setShowConnectModal(false)}
              onSuccess={handleConnectionSuccess}
            />
          ) : (
            <div className="fixed inset-0 z-50 flex items-center justify-center">
              <div
                className="fixed inset-0 bg-black/50"
                onClick={() => setShowConnectModal(false)}
              />
              <div className="relative z-50 w-full max-w-md rounded-lg bg-background p-6 shadow-lg">
                <h3 className="text-lg font-semibold mb-4">
                  Connect {selectedIntegration.name}
                </h3>
                <p className="text-sm text-muted-foreground mb-6">
                  Connection modal for {selectedIntegration.name} will be implemented soon.
                </p>
                <div className="flex justify-end gap-2">
                  <Button variant="outline" onClick={() => setShowConnectModal(false)}>
                    Cancel
                  </Button>
                  <Button variant="primary" disabled>Connect</Button>
                </div>
              </div>
            </div>
          )}
        </>
      )}

      {/* Courier Settings Modal */}
      {showSettingsModal && selectedIntegration && selectedAccountId && (
        <CourierSettingsModal
          isOpen={showSettingsModal}
          onClose={() => {
            setShowSettingsModal(false);
            setSelectedAccountId(null);
            setSelectedIntegration(null);
          }}
          accountId={selectedAccountId}
          courierName={selectedIntegration.name}
          onUpdate={handleConnectionSuccess}
        />
      )}
    </DashboardLayout>
  );
}
