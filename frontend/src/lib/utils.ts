import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

/**
 * Merge Tailwind CSS classes with clsx and tailwind-merge.
 * This utility handles class conflicts intelligently.
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * Format currency value.
 */
export function formatCurrency(
  amount: number,
  currency: string = 'INR',
  locale: string = 'en-IN'
): string {
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);
}

/**
 * Format date to localized string.
 */
export function formatDate(
  date: Date | string,
  options: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }
): string {
  const d = typeof date === 'string' ? new Date(date) : date;
  return d.toLocaleDateString('en-IN', options);
}

/**
 * Format date with time.
 */
export function formatDateTime(date: Date | string): string {
  const d = typeof date === 'string' ? new Date(date) : date;
  return d.toLocaleString('en-IN', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

/**
 * Truncate text to specified length.
 */
export function truncate(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text;
  return text.slice(0, maxLength - 3) + '...';
}

/**
 * Generate initials from name.
 */
export function getInitials(name: string): string {
  return name
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}

/**
 * Debounce function execution.
 */
export function debounce<T extends (...args: unknown[]) => unknown>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let timeout: NodeJS.Timeout;
  return (...args: Parameters<T>) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => func(...args), wait);
  };
}

/**
 * Sleep for specified milliseconds.
 */
export function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Check if value is empty (null, undefined, empty string, empty array, empty object).
 */
export function isEmpty(value: unknown): boolean {
  if (value === null || value === undefined) return true;
  if (typeof value === 'string') return value.trim() === '';
  if (Array.isArray(value)) return value.length === 0;
  if (typeof value === 'object') return Object.keys(value).length === 0;
  return false;
}

/**
 * Get order status color class.
 */
export function getOrderStatusColor(status: string): string {
  const statusColors: Record<string, string> = {
    pending: 'bg-warning/10 text-warning',
    confirmed: 'bg-info/10 text-info',
    processing: 'bg-info/10 text-info',
    shipped: 'bg-primary/10 text-primary',
    delivered: 'bg-success/10 text-success',
    cancelled: 'bg-error/10 text-error',
    returned: 'bg-muted text-muted-foreground',
    rto: 'bg-error/10 text-error',
  };
  return statusColors[status.toLowerCase()] || 'bg-muted text-muted-foreground';
}

/**
 * Get shipment status color class.
 */
export function getShipmentStatusColor(status: string): string {
  const statusColors: Record<string, string> = {
    created: 'bg-muted text-muted-foreground',
    manifested: 'bg-info/10 text-info',
    picked_up: 'bg-info/10 text-info',
    in_transit: 'bg-primary/10 text-primary',
    out_for_delivery: 'bg-primary/10 text-primary',
    delivered: 'bg-success/10 text-success',
    ndr: 'bg-warning/10 text-warning',
    rto_initiated: 'bg-error/10 text-error',
    rto_delivered: 'bg-error/10 text-error',
    cancelled: 'bg-muted text-muted-foreground',
  };
  return statusColors[status.toLowerCase()] || 'bg-muted text-muted-foreground';
}

/**
 * Get NDR status color class.
 */
export function getNdrStatusColor(status: string): string {
  const statusColors: Record<string, string> = {
    open: 'bg-error/10 text-error',
    in_progress: 'bg-warning/10 text-warning',
    reattempt_scheduled: 'bg-info/10 text-info',
    resolved: 'bg-success/10 text-success',
    rto_initiated: 'bg-error/10 text-error',
    closed: 'bg-muted text-muted-foreground',
  };
  return statusColors[status.toLowerCase()] || 'bg-muted text-muted-foreground';
}
