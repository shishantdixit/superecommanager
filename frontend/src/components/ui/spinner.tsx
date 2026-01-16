import { cn } from '@/lib/utils';

export interface SpinnerProps {
  size?: 'sm' | 'md' | 'lg' | 'xl';
  className?: string;
}

export function Spinner({ size = 'md', className }: SpinnerProps) {
  const sizes = {
    sm: 'h-4 w-4 border-2',
    md: 'h-6 w-6 border-2',
    lg: 'h-8 w-8 border-3',
    xl: 'h-12 w-12 border-4',
  };

  return (
    <div
      className={cn(
        'animate-spin rounded-full border-primary border-t-transparent',
        sizes[size],
        className
      )}
    />
  );
}

/**
 * Full page loading spinner.
 */
export function PageLoader() {
  return (
    <div className="flex h-screen items-center justify-center">
      <Spinner size="xl" />
    </div>
  );
}

/**
 * Section loading spinner.
 */
export function SectionLoader({ className }: { className?: string }) {
  return (
    <div className={cn('flex items-center justify-center py-12', className)}>
      <Spinner size="lg" />
    </div>
  );
}

/**
 * Inline loading indicator.
 */
export function InlineLoader({ text = 'Loading...' }: { text?: string }) {
  return (
    <div className="flex items-center gap-2 text-muted-foreground">
      <Spinner size="sm" />
      <span className="text-sm">{text}</span>
    </div>
  );
}
