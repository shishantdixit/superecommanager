import { cn, getInitials } from '@/lib/utils';

export interface AvatarProps {
  src?: string | null;
  name?: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  className?: string;
}

export function Avatar({ src, name = '', size = 'md', className }: AvatarProps) {
  const sizes = {
    sm: 'h-8 w-8 text-xs',
    md: 'h-10 w-10 text-sm',
    lg: 'h-12 w-12 text-base',
    xl: 'h-16 w-16 text-lg',
  };

  const initials = getInitials(name);

  if (src) {
    return (
      <img
        src={src}
        alt={name}
        className={cn(
          'rounded-full object-cover',
          sizes[size],
          className
        )}
      />
    );
  }

  return (
    <div
      className={cn(
        'flex items-center justify-center rounded-full bg-primary font-medium text-primary-foreground',
        sizes[size],
        className
      )}
    >
      {initials}
    </div>
  );
}

/**
 * Avatar with status indicator.
 */
export interface AvatarWithStatusProps extends AvatarProps {
  status?: 'online' | 'offline' | 'away' | 'busy';
}

export function AvatarWithStatus({
  status,
  ...props
}: AvatarWithStatusProps) {
  const statusColors = {
    online: 'bg-success',
    offline: 'bg-muted-foreground',
    away: 'bg-warning',
    busy: 'bg-error',
  };

  return (
    <div className="relative inline-block">
      <Avatar {...props} />
      {status && (
        <span
          className={cn(
            'absolute bottom-0 right-0 h-3 w-3 rounded-full border-2 border-background',
            statusColors[status]
          )}
        />
      )}
    </div>
  );
}

/**
 * Avatar group for showing multiple avatars.
 */
export interface AvatarGroupProps {
  avatars: Array<{ src?: string; name: string }>;
  max?: number;
  size?: AvatarProps['size'];
}

export function AvatarGroup({ avatars, max = 4, size = 'md' }: AvatarGroupProps) {
  const displayAvatars = avatars.slice(0, max);
  const remaining = avatars.length - max;

  return (
    <div className="flex -space-x-2">
      {displayAvatars.map((avatar, index) => (
        <Avatar
          key={index}
          src={avatar.src}
          name={avatar.name}
          size={size}
          className="ring-2 ring-background"
        />
      ))}
      {remaining > 0 && (
        <div
          className={cn(
            'flex items-center justify-center rounded-full bg-muted font-medium text-muted-foreground ring-2 ring-background',
            size === 'sm' && 'h-8 w-8 text-xs',
            size === 'md' && 'h-10 w-10 text-sm',
            size === 'lg' && 'h-12 w-12 text-base',
            size === 'xl' && 'h-16 w-16 text-lg'
          )}
        >
          +{remaining}
        </div>
      )}
    </div>
  );
}
