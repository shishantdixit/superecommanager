'use client';

import { useEffect, useState } from 'react';
import {
  Settings,
  Globe,
  Mail,
  Shield,
  CreditCard,
  Bell,
  Puzzle,
  ToggleLeft,
  Save,
  Loader2,
} from 'lucide-react';
import { PlatformAdminLayout } from '@/components/platform-admin/layout';
import { getPlatformSettings, updatePlatformSetting } from '@/services/platform-admin.service';
import type { PlatformSetting, PlatformSettingCategory } from '@/types/api';
import { cn } from '@/lib/utils';

const categories: { key: PlatformSettingCategory; label: string; icon: React.ReactNode; description: string }[] = [
  { key: 'General', label: 'General', icon: <Globe className="h-5 w-5" />, description: 'Basic platform configuration' },
  { key: 'Email', label: 'Email', icon: <Mail className="h-5 w-5" />, description: 'Email server and templates' },
  { key: 'Security', label: 'Security', icon: <Shield className="h-5 w-5" />, description: 'Security and authentication' },
  { key: 'Payment', label: 'Payment', icon: <CreditCard className="h-5 w-5" />, description: 'Payment gateway settings' },
  { key: 'Notification', label: 'Notifications', icon: <Bell className="h-5 w-5" />, description: 'Notification preferences' },
  { key: 'Integration', label: 'Integrations', icon: <Puzzle className="h-5 w-5" />, description: 'Third-party integrations' },
  { key: 'Feature', label: 'Features', icon: <ToggleLeft className="h-5 w-5" />, description: 'Feature flags and toggles' },
];

export default function SettingsPage() {
  const [settings, setSettings] = useState<PlatformSetting[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [activeCategory, setActiveCategory] = useState<PlatformSettingCategory>('General');
  const [editedValues, setEditedValues] = useState<Record<string, string>>({});
  const [savingKeys, setSavingKeys] = useState<string[]>([]);

  useEffect(() => {
    fetchSettings();
  }, []);

  const fetchSettings = async () => {
    try {
      setIsLoading(true);
      const data = await getPlatformSettings();
      setSettings(data);
    } catch (error) {
      console.error('Failed to fetch settings:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const categorySettings = settings.filter((s) => s.category === activeCategory);

  const handleValueChange = (key: string, value: string) => {
    setEditedValues((prev) => ({ ...prev, [key]: value }));
  };

  const handleSave = async (setting: PlatformSetting) => {
    const newValue = editedValues[setting.key];
    if (newValue === undefined || newValue === setting.value) return;

    setSavingKeys((prev) => [...prev, setting.key]);
    try {
      await updatePlatformSetting(setting.key, newValue);
      setSettings((prev) =>
        prev.map((s) => (s.key === setting.key ? { ...s, value: newValue } : s))
      );
      setEditedValues((prev) => {
        const { [setting.key]: _, ...rest } = prev;
        return rest;
      });
    } catch (error) {
      console.error('Failed to save setting:', error);
    } finally {
      setSavingKeys((prev) => prev.filter((k) => k !== setting.key));
    }
  };

  const getValue = (setting: PlatformSetting) => {
    return editedValues[setting.key] ?? setting.value;
  };

  const hasChanges = (setting: PlatformSetting) => {
    return editedValues[setting.key] !== undefined && editedValues[setting.key] !== setting.value;
  };

  return (
    <PlatformAdminLayout title="Platform Settings">
      <div className="flex gap-6">
        {/* Sidebar */}
        <div className="w-64 shrink-0">
          <div className="sticky top-6 rounded-lg border border-slate-200 bg-white">
            <div className="border-b border-slate-200 px-4 py-3">
              <h3 className="font-medium text-slate-900">Categories</h3>
            </div>
            <nav className="p-2">
              {categories.map((category) => (
                <button
                  key={category.key}
                  onClick={() => setActiveCategory(category.key)}
                  className={cn(
                    'flex w-full items-center gap-3 rounded-lg px-3 py-2 text-left text-sm transition-colors',
                    activeCategory === category.key
                      ? 'bg-indigo-50 text-indigo-700'
                      : 'text-slate-600 hover:bg-slate-50'
                  )}
                >
                  <span
                    className={cn(
                      activeCategory === category.key ? 'text-indigo-600' : 'text-slate-400'
                    )}
                  >
                    {category.icon}
                  </span>
                  {category.label}
                </button>
              ))}
            </nav>
          </div>
        </div>

        {/* Content */}
        <div className="flex-1">
          <div className="rounded-lg border border-slate-200 bg-white">
            {/* Category Header */}
            <div className="border-b border-slate-200 px-6 py-4">
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-indigo-100 text-indigo-600">
                  {categories.find((c) => c.key === activeCategory)?.icon}
                </div>
                <div>
                  <h2 className="font-semibold text-slate-900">
                    {categories.find((c) => c.key === activeCategory)?.label} Settings
                  </h2>
                  <p className="text-sm text-slate-500">
                    {categories.find((c) => c.key === activeCategory)?.description}
                  </p>
                </div>
              </div>
            </div>

            {/* Settings List */}
            <div className="divide-y divide-slate-100">
              {isLoading ? (
                <div className="flex h-64 items-center justify-center">
                  <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-500 border-t-transparent" />
                </div>
              ) : categorySettings.length === 0 ? (
                <div className="flex h-64 flex-col items-center justify-center">
                  <Settings className="h-12 w-12 text-slate-300" />
                  <p className="mt-4 text-slate-500">No settings in this category</p>
                  <p className="mt-1 text-sm text-slate-400">
                    Settings will appear here once configured
                  </p>
                </div>
              ) : (
                categorySettings.map((setting) => (
                  <div key={setting.id} className="px-6 py-4">
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex-1">
                        <label className="block font-medium text-slate-900">{setting.key}</label>
                        {setting.description && (
                          <p className="mt-1 text-sm text-slate-500">{setting.description}</p>
                        )}
                        {setting.isEncrypted && (
                          <span className="mt-1 inline-block rounded bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-700">
                            Encrypted
                          </span>
                        )}
                      </div>
                      <div className="flex items-center gap-2">
                        {setting.isEncrypted ? (
                          <input
                            type="password"
                            value={getValue(setting)}
                            onChange={(e) => handleValueChange(setting.key, e.target.value)}
                            className="w-64 rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                            placeholder="••••••••"
                          />
                        ) : setting.value === 'true' || setting.value === 'false' ? (
                          <button
                            onClick={() =>
                              handleValueChange(
                                setting.key,
                                getValue(setting) === 'true' ? 'false' : 'true'
                              )
                            }
                            className={cn(
                              'relative h-6 w-11 rounded-full transition-colors',
                              getValue(setting) === 'true' ? 'bg-indigo-600' : 'bg-slate-200'
                            )}
                          >
                            <span
                              className={cn(
                                'absolute top-0.5 h-5 w-5 rounded-full bg-white shadow transition-all',
                                getValue(setting) === 'true' ? 'left-5' : 'left-0.5'
                              )}
                            />
                          </button>
                        ) : (
                          <input
                            type="text"
                            value={getValue(setting)}
                            onChange={(e) => handleValueChange(setting.key, e.target.value)}
                            className="w-64 rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                          />
                        )}
                        {hasChanges(setting) && (
                          <button
                            onClick={() => handleSave(setting)}
                            disabled={savingKeys.includes(setting.key)}
                            className="flex items-center gap-1 rounded-lg bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
                          >
                            {savingKeys.includes(setting.key) ? (
                              <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                              <Save className="h-4 w-4" />
                            )}
                            Save
                          </button>
                        )}
                      </div>
                    </div>
                    <p className="mt-2 text-xs text-slate-400">
                      Last updated: {new Date(setting.updatedAt).toLocaleString()}
                      {setting.updatedBy && ` by ${setting.updatedBy}`}
                    </p>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      </div>
    </PlatformAdminLayout>
  );
}
