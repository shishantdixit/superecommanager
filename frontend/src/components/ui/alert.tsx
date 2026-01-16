import { cn } from '@/lib/utils';
import { AlertCircle, CheckCircle, Info, XCircle, X } from 'lucide-react';
import { type HTMLAttributes, type ReactNode } from 'react';

export interface AlertProps extends HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'success' | 'warning' | 'error' | 'info';
  title?: string;
  onClose?: () => void;
  children: ReactNode;
}

export function Alert({
  className,
  variant = 'default',
  title,
  onClose,
  children,
  ...props
}: AlertProps) {
  const variants = {
    default: 'bg-muted border-border text-foreground',
    success: 'bg-success/10 border-success/20 text-success',
    warning: 'bg-warning/10 border-warning/20 text-warning',
    error: 'bg-error/10 border-error/20 text-error',
    info: 'bg-info/10 border-info/20 text-info',
  };

  const icons = {
    default: Info,
    success: CheckCircle,
    warning: AlertCircle,
    error: XCircle,
    info: Info,
  };

  const Icon = icons[variant];

  return (
    <div
      role="alert"
      className={cn(
        'relative flex gap-3 rounded-lg border p-4',
        variants[variant],
        className
      )}
      {...props}
    >
      <Icon className="h-5 w-5 shrink-0" />
      <div className="flex-1">
        {title && <h5 className="mb-1 font-medium">{title}</h5>}
        <div className="text-sm opacity-90">{children}</div>
      </div>
      {onClose && (
        <button
          onClick={onClose}
          className="absolute right-2 top-2 rounded p-1 opacity-70 hover:opacity-100 transition-opacity"
        >
          <X className="h-4 w-4" />
        </button>
      )}
    </div>
  );
}

/**
 * Inline alert for form validation errors.
 */
export interface FormErrorProps {
  message?: string;
  className?: string;
}

export function FormError({ message, className }: FormErrorProps) {
  if (!message) return null;

  return (
    <p className={cn('flex items-center gap-1.5 text-sm text-error', className)}>
      <AlertCircle className="h-4 w-4" />
      {message}
    </p>
  );
}
