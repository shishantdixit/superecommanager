import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { channelsService, ConnectShopifyRequest } from '@/services/channels.service';

export const channelKeys = {
  all: ['channels'] as const,
  lists: () => [...channelKeys.all, 'list'] as const,
  list: () => [...channelKeys.lists()] as const,
  details: () => [...channelKeys.all, 'detail'] as const,
  detail: (id: string) => [...channelKeys.details(), id] as const,
};

/**
 * Hook to fetch all sales channels.
 */
export function useChannels() {
  return useQuery({
    queryKey: channelKeys.list(),
    queryFn: () => channelsService.getChannels(),
  });
}

/**
 * Hook to fetch a single channel by ID.
 */
export function useChannel(id: string) {
  return useQuery({
    queryKey: channelKeys.detail(id),
    queryFn: () => channelsService.getChannelById(id),
    enabled: !!id,
  });
}

/**
 * Hook to initiate Shopify OAuth connection.
 */
export function useConnectShopify() {
  return useMutation({
    mutationFn: (data: ConnectShopifyRequest) => channelsService.connectShopify(data),
  });
}

/**
 * Hook to disconnect a channel.
 */
export function useDisconnectChannel() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => channelsService.disconnectChannel(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: channelKeys.lists() });
    },
  });
}

/**
 * Hook to trigger manual sync for a channel.
 */
export function useSyncChannel() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => channelsService.syncChannel(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: channelKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: channelKeys.lists() });
    },
  });
}
