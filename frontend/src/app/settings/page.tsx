'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { DashboardLayout } from '@/components/layout';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Button,
  Input,
  Select,
  Badge,
  SectionLoader,
} from '@/components/ui';
import {
  Settings,
  Building2,
  Bell,
  CreditCard,
  Link as LinkIcon,
  Shield,
  Palette,
  Save,
  Store,
  Mail,
  Phone,
  Globe,
  MapPin,
} from 'lucide-react';

const tabs = [
  { id: 'business', label: 'Business', icon: Building2 },
  { id: 'notifications', label: 'Notifications', icon: Bell },
  { id: 'integrations', label: 'Integrations', icon: LinkIcon },
  { id: 'billing', label: 'Billing', icon: CreditCard },
  { id: 'security', label: 'Security', icon: Shield },
  { id: 'appearance', label: 'Appearance', icon: Palette },
];

const businessSchema = z.object({
  businessName: z.string().min(1, 'Business name is required'),
  email: z.string().email('Invalid email'),
  phone: z.string().optional(),
  website: z.string().url().optional().or(z.literal('')),
  address: z.string().optional(),
  city: z.string().optional(),
  state: z.string().optional(),
  postalCode: z.string().optional(),
  country: z.string().default('IN'),
  gstNumber: z.string().optional(),
  panNumber: z.string().optional(),
});

type BusinessFormData = z.infer<typeof businessSchema>;

export default function SettingsPage() {
  const [activeTab, setActiveTab] = useState('business');
  const [isSaving, setIsSaving] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isDirty },
  } = useForm<BusinessFormData>({
    resolver: zodResolver(businessSchema),
    defaultValues: {
      businessName: 'My Store',
      email: 'store@example.com',
      country: 'IN',
    },
  });

  const onSubmit = async (data: BusinessFormData) => {
    setIsSaving(true);
    try {
      // TODO: Save settings via API
      await new Promise((resolve) => setTimeout(resolve, 1000));
      console.log('Settings saved:', data);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <DashboardLayout title="Settings">
      <div className="grid gap-6 lg:grid-cols-4">
        {/* Sidebar */}
        <div className="lg:col-span-1">
          <Card>
            <CardContent className="p-2">
              <nav className="space-y-1">
                {tabs.map((tab) => {
                  const Icon = tab.icon;
                  return (
                    <button
                      key={tab.id}
                      onClick={() => setActiveTab(tab.id)}
                      className={`w-full flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${
                        activeTab === tab.id
                          ? 'bg-primary/10 text-primary'
                          : 'text-muted-foreground hover:bg-muted'
                      }`}
                    >
                      <Icon className="h-4 w-4" />
                      {tab.label}
                    </button>
                  );
                })}
              </nav>
            </CardContent>
          </Card>
        </div>

        {/* Content */}
        <div className="lg:col-span-3">
          {activeTab === 'business' && (
            <form onSubmit={handleSubmit(onSubmit)}>
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Building2 className="h-5 w-5" />
                    Business Information
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-6">
                  <div className="grid gap-4 md:grid-cols-2">
                    <div>
                      <label className="block text-sm font-medium mb-1">
                        Business Name <span className="text-error">*</span>
                      </label>
                      <Input
                        {...register('businessName')}
                        leftIcon={<Store className="h-4 w-4" />}
                        placeholder="Your business name"
                      />
                      {errors.businessName && (
                        <p className="text-sm text-error mt-1">{errors.businessName.message}</p>
                      )}
                    </div>
                    <div>
                      <label className="block text-sm font-medium mb-1">
                        Email <span className="text-error">*</span>
                      </label>
                      <Input
                        {...register('email')}
                        type="email"
                        leftIcon={<Mail className="h-4 w-4" />}
                        placeholder="business@example.com"
                      />
                      {errors.email && (
                        <p className="text-sm text-error mt-1">{errors.email.message}</p>
                      )}
                    </div>
                    <div>
                      <label className="block text-sm font-medium mb-1">Phone</label>
                      <Input
                        {...register('phone')}
                        leftIcon={<Phone className="h-4 w-4" />}
                        placeholder="+91 98765 43210"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium mb-1">Website</label>
                      <Input
                        {...register('website')}
                        leftIcon={<Globe className="h-4 w-4" />}
                        placeholder="https://yourstore.com"
                      />
                    </div>
                  </div>

                  <div className="border-t pt-6">
                    <h3 className="font-medium mb-4">Address</h3>
                    <div className="grid gap-4 md:grid-cols-2">
                      <div className="md:col-span-2">
                        <label className="block text-sm font-medium mb-1">Street Address</label>
                        <Input
                          {...register('address')}
                          leftIcon={<MapPin className="h-4 w-4" />}
                          placeholder="123 Main Street"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium mb-1">City</label>
                        <Input {...register('city')} placeholder="Mumbai" />
                      </div>
                      <div>
                        <label className="block text-sm font-medium mb-1">State</label>
                        <Input {...register('state')} placeholder="Maharashtra" />
                      </div>
                      <div>
                        <label className="block text-sm font-medium mb-1">Postal Code</label>
                        <Input {...register('postalCode')} placeholder="400001" />
                      </div>
                      <div>
                        <label className="block text-sm font-medium mb-1">Country</label>
                        <Select
                          {...register('country')}
                          options={[
                            { value: 'IN', label: 'India' },
                            { value: 'US', label: 'United States' },
                            { value: 'GB', label: 'United Kingdom' },
                          ]}
                        />
                      </div>
                    </div>
                  </div>

                  <div className="border-t pt-6">
                    <h3 className="font-medium mb-4">Tax Information</h3>
                    <div className="grid gap-4 md:grid-cols-2">
                      <div>
                        <label className="block text-sm font-medium mb-1">GST Number</label>
                        <Input {...register('gstNumber')} placeholder="22AAAAA0000A1Z5" />
                      </div>
                      <div>
                        <label className="block text-sm font-medium mb-1">PAN Number</label>
                        <Input {...register('panNumber')} placeholder="AAAAA0000A" />
                      </div>
                    </div>
                  </div>

                  <div className="flex justify-end pt-4 border-t">
                    <Button
                      type="submit"
                      isLoading={isSaving}
                      disabled={!isDirty}
                      leftIcon={<Save className="h-4 w-4" />}
                    >
                      Save Changes
                    </Button>
                  </div>
                </CardContent>
              </Card>
            </form>
          )}

          {activeTab === 'notifications' && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Bell className="h-5 w-5" />
                  Notification Settings
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-6">
                <NotificationToggle
                  title="Order Notifications"
                  description="Receive notifications for new orders"
                  defaultChecked={true}
                />
                <NotificationToggle
                  title="Shipment Updates"
                  description="Get notified when shipment status changes"
                  defaultChecked={true}
                />
                <NotificationToggle
                  title="NDR Alerts"
                  description="Immediate alerts for NDR cases"
                  defaultChecked={true}
                />
                <NotificationToggle
                  title="Low Stock Alerts"
                  description="Get notified when products are running low"
                  defaultChecked={true}
                />
                <NotificationToggle
                  title="Daily Summary"
                  description="Receive a daily summary of your business"
                  defaultChecked={false}
                />
                <NotificationToggle
                  title="Marketing Updates"
                  description="Tips and updates from SuperEcomManager"
                  defaultChecked={false}
                />
              </CardContent>
            </Card>
          )}

          {activeTab === 'integrations' && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <LinkIcon className="h-5 w-5" />
                  Integrations
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <IntegrationCard
                  name="Shopify"
                  description="Sync orders and inventory from your Shopify store"
                  connected={true}
                />
                <IntegrationCard
                  name="Amazon"
                  description="Connect your Amazon Seller Central account"
                  connected={false}
                />
                <IntegrationCard
                  name="Flipkart"
                  description="Integrate with Flipkart Seller Hub"
                  connected={false}
                />
                <IntegrationCard
                  name="Meesho"
                  description="Connect your Meesho supplier account"
                  connected={false}
                />
                <IntegrationCard
                  name="Shiprocket"
                  description="Manage shipments and tracking"
                  connected={true}
                />
              </CardContent>
            </Card>
          )}

          {activeTab === 'billing' && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <CreditCard className="h-5 w-5" />
                  Billing & Subscription
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="rounded-lg border p-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="font-medium">Current Plan</h3>
                      <p className="text-2xl font-bold text-primary mt-1">Professional</p>
                      <p className="text-sm text-muted-foreground">
                        ₹2,999/month, billed monthly
                      </p>
                    </div>
                    <Badge variant="success">Active</Badge>
                  </div>
                  <div className="mt-4 pt-4 border-t">
                    <p className="text-sm text-muted-foreground">
                      Next billing date: February 1, 2026
                    </p>
                  </div>
                </div>

                <div className="grid gap-4 md:grid-cols-3">
                  <div className="rounded-lg border p-4 text-center">
                    <p className="text-2xl font-bold">5,000</p>
                    <p className="text-sm text-muted-foreground">Orders/month</p>
                  </div>
                  <div className="rounded-lg border p-4 text-center">
                    <p className="text-2xl font-bold">10</p>
                    <p className="text-sm text-muted-foreground">Team Members</p>
                  </div>
                  <div className="rounded-lg border p-4 text-center">
                    <p className="text-2xl font-bold">5</p>
                    <p className="text-sm text-muted-foreground">Sales Channels</p>
                  </div>
                </div>

                <div className="flex gap-2">
                  <Button variant="outline">View Invoices</Button>
                  <Button variant="outline">Update Payment Method</Button>
                  <Button>Upgrade Plan</Button>
                </div>
              </CardContent>
            </Card>
          )}

          {activeTab === 'security' && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Shield className="h-5 w-5" />
                  Security Settings
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="rounded-lg border p-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="font-medium">Two-Factor Authentication</h3>
                      <p className="text-sm text-muted-foreground">
                        Add an extra layer of security to your account
                      </p>
                    </div>
                    <Badge variant="warning">Not Enabled</Badge>
                  </div>
                  <Button variant="outline" className="mt-3" size="sm">
                    Enable 2FA
                  </Button>
                </div>

                <div className="rounded-lg border p-4">
                  <h3 className="font-medium mb-2">Password</h3>
                  <p className="text-sm text-muted-foreground mb-3">
                    Last changed 30 days ago
                  </p>
                  <Button variant="outline" size="sm">
                    Change Password
                  </Button>
                </div>

                <div className="rounded-lg border p-4">
                  <h3 className="font-medium mb-2">Active Sessions</h3>
                  <p className="text-sm text-muted-foreground mb-3">
                    You are currently logged in on 2 devices
                  </p>
                  <Button variant="outline" size="sm">
                    Manage Sessions
                  </Button>
                </div>

                <div className="rounded-lg border p-4">
                  <h3 className="font-medium mb-2">API Keys</h3>
                  <p className="text-sm text-muted-foreground mb-3">
                    Manage API keys for third-party integrations
                  </p>
                  <Button variant="outline" size="sm">
                    Manage API Keys
                  </Button>
                </div>
              </CardContent>
            </Card>
          )}

          {activeTab === 'appearance' && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Palette className="h-5 w-5" />
                  Appearance
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-6">
                <div>
                  <h3 className="font-medium mb-3">Theme</h3>
                  <div className="grid grid-cols-3 gap-4">
                    <ThemeOption label="Light" selected={false} />
                    <ThemeOption label="Dark" selected={false} />
                    <ThemeOption label="System" selected={true} />
                  </div>
                </div>

                <div>
                  <h3 className="font-medium mb-3">Accent Color</h3>
                  <div className="flex gap-3">
                    <ColorOption color="bg-blue-500" selected={true} />
                    <ColorOption color="bg-purple-500" selected={false} />
                    <ColorOption color="bg-green-500" selected={false} />
                    <ColorOption color="bg-orange-500" selected={false} />
                    <ColorOption color="bg-pink-500" selected={false} />
                  </div>
                </div>

                <div>
                  <h3 className="font-medium mb-3">Sidebar</h3>
                  <div className="flex items-center gap-3">
                    <input
                      type="checkbox"
                      id="collapsedSidebar"
                      className="rounded border-input"
                      defaultChecked={false}
                    />
                    <label htmlFor="collapsedSidebar" className="text-sm">
                      Collapse sidebar by default
                    </label>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </DashboardLayout>
  );
}

function NotificationToggle({
  title,
  description,
  defaultChecked,
}: {
  title: string;
  description: string;
  defaultChecked: boolean;
}) {
  const [checked, setChecked] = useState(defaultChecked);

  return (
    <div className="flex items-center justify-between py-3 border-b last:border-0">
      <div>
        <p className="font-medium">{title}</p>
        <p className="text-sm text-muted-foreground">{description}</p>
      </div>
      <button
        onClick={() => setChecked(!checked)}
        className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
          checked ? 'bg-primary' : 'bg-muted'
        }`}
      >
        <span
          className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
            checked ? 'translate-x-6' : 'translate-x-1'
          }`}
        />
      </button>
    </div>
  );
}

function IntegrationCard({
  name,
  description,
  connected,
}: {
  name: string;
  description: string;
  connected: boolean;
}) {
  return (
    <div className="flex items-center justify-between rounded-lg border p-4">
      <div>
        <h3 className="font-medium">{name}</h3>
        <p className="text-sm text-muted-foreground">{description}</p>
      </div>
      {connected ? (
        <div className="flex items-center gap-2">
          <Badge variant="success">Connected</Badge>
          <Button variant="outline" size="sm">
            Settings
          </Button>
        </div>
      ) : (
        <Button size="sm">Connect</Button>
      )}
    </div>
  );
}

function ThemeOption({ label, selected }: { label: string; selected: boolean }) {
  return (
    <button
      className={`rounded-lg border-2 p-4 text-center transition-colors ${
        selected ? 'border-primary bg-primary/5' : 'border-input hover:border-primary/50'
      }`}
    >
      <span className="font-medium">{label}</span>
    </button>
  );
}

function ColorOption({ color, selected }: { color: string; selected: boolean }) {
  return (
    <button
      className={`h-8 w-8 rounded-full ${color} ${
        selected ? 'ring-2 ring-offset-2 ring-primary' : ''
      }`}
    />
  );
}
