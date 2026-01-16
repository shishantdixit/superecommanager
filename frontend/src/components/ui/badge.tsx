import { cn } from '@/lib/utils';
import { type HTMLAttributes, type ReactNode } from 'react';

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: 'default' | 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
  size?: 'sm' | 'md' | 'lg';
  children: ReactNode;
}

export function Badge({
  className,
  variant = 'default',
  size = 'md',
  children,
  ...props
}: BadgeProps) {
  const variants = {
    default: 'bg-muted text-muted-foreground',
    primary: 'bg-primary/10 text-primary',
    secondary: 'bg-muted text-foreground',
    success: 'bg-success/10 text-success',
    warning: 'bg-warning/10 text-warning',
    error: 'bg-error/10 text-error',
    info: 'bg-info/10 text-info',
  };

  const sizes = {
    sm: 'px-1.5 py-0.5 text-xs',
    md: 'px-2 py-0.5 text-xs',
    lg: 'px-2.5 py-1 text-sm',
  };

  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full font-medium',
        variants[variant],
        sizes[size],
        className
      )}
      {...props}
    >
      {children}
    </span>
  );
}

/**
 * Status badge with dot indicator.
 */
export interface StatusBadgeProps {
  status: string;
  label?: string;
  className?: string;
}

export function StatusBadge({ status, label, className }: StatusBadgeProps) {
  const statusConfig: Record<string, { variant: BadgeProps['variant']; dot: string }> = {
    // Order statuses
    pending: { variant: 'warning', dot: 'bg-warning' },
    confirmed: { variant: 'info', dot: 'bg-info' },
    processing: { variant: 'info', dot: 'bg-info' },
    shipped: { variant: 'primary', dot: 'bg-primary' },
    delivered: { variant: 'success', dot: 'bg-success' },
    cancelled: { variant: 'error', dot: 'bg-error' },
    returned: { variant: 'default', dot: 'bg-muted-foreground' },
    rto: { variant: 'error', dot: 'bg-error' },
    // Shipment statuses
    created: { variant: 'default', dot: 'bg-muted-foreground' },
    manifested: { variant: 'info', dot: 'bg-info' },
    picked_up: { variant: 'info', dot: 'bg-info' },
    in_transit: { variant: 'primary', dot: 'bg-primary' },
    out_for_delivery: { variant: 'primary', dot: 'bg-primary' },
    ndr: { variant: 'warning', dot: 'bg-warning' },
    rto_initiated: { variant: 'error', dot: 'bg-error' },
    rto_delivered: { variant: 'error', dot: 'bg-error' },
    // NDR statuses
    open: { variant: 'error', dot: 'bg-error' },
    in_progress: { variant: 'warning', dot: 'bg-warning' },
    reattempt_scheduled: { variant: 'info', dot: 'bg-info' },
    resolved: { variant: 'success', dot: 'bg-success' },
    closed: { variant: 'default', dot: 'bg-muted-foreground' },
    // Payment statuses
    paid: { variant: 'success', dot: 'bg-success' },
    failed: { variant: 'error', dot: 'bg-error' },
    refunded: { variant: 'warning', dot: 'bg-warning' },
    cod: { variant: 'info', dot: 'bg-info' },
    // Generic
    active: { variant: 'success', dot: 'bg-success' },
    inactive: { variant: 'default', dot: 'bg-muted-foreground' },
    suspended: { variant: 'error', dot: 'bg-error' },
  };

  const normalizedStatus = status.toLowerCase().replace(/ /g, '_');
  const config = statusConfig[normalizedStatus] || { variant: 'default', dot: 'bg-muted-foreground' };

  return (
    <Badge variant={config.variant} className={cn('gap-1.5', className)}>
      <span className={cn('h-1.5 w-1.5 rounded-full', config.dot)} />
      {label || status}
    </Badge>
  );
}
