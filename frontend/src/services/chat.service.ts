import { get, post, del, patch } from '@/lib/api-client';
import type { ApiResponse } from '@/types/api';

/**
 * Chat conversation summary for listing.
 */
export interface ChatConversationSummary {
  id: string;
  title: string;
  messageCount: number;
  lastMessageAt?: string;
  createdAt: string;
}

/**
 * Chat message DTO.
 */
export interface ChatMessageDto {
  id: string;
  role: 'User' | 'Assistant' | 'System' | 'Tool';
  content: string;
  createdAt: string;
  toolName?: string;
  toolCalls?: string;
}

/**
 * Chat conversation detail with messages.
 */
export interface ChatConversationDetail {
  id: string;
  title: string;
  messageCount: number;
  totalTokensUsed: number;
  createdAt: string;
  lastMessageAt?: string;
  messages: ChatMessageDto[];
}

/**
 * Send message request.
 */
export interface SendMessageRequest {
  conversationId?: string;
  message: string;
}

/**
 * Chat message response.
 */
export interface ChatMessageResponse {
  conversationId: string;
  message: string;
  tokensUsed: number;
}

/**
 * Chat tool info.
 */
export interface ChatToolInfo {
  name: string;
  description: string;
  category: string;
}

/**
 * Chat service for interacting with the AI assistant.
 */
export const chatService = {
  /**
   * Send a message to the AI assistant.
   */
  sendMessage: async (request: SendMessageRequest): Promise<ChatMessageResponse> => {
    const response = await post<ApiResponse<ChatMessageResponse>, SendMessageRequest>(
      '/chat/message',
      request
    );
    return response.data;
  },

  /**
   * Get conversation history for the current user.
   */
  getConversations: async (limit = 20): Promise<ChatConversationSummary[]> => {
    const response = await get<ApiResponse<ChatConversationSummary[]>>(
      `/chat/conversations?limit=${limit}`
    );
    return response.data;
  },

  /**
   * Get conversation details with messages.
   */
  getConversation: async (conversationId: string): Promise<ChatConversationDetail> => {
    const response = await get<ApiResponse<ChatConversationDetail>>(
      `/chat/conversations/${conversationId}`
    );
    return response.data;
  },

  /**
   * Delete a conversation.
   */
  deleteConversation: async (conversationId: string): Promise<void> => {
    await del<ApiResponse<boolean>>(`/chat/conversations/${conversationId}`);
  },

  /**
   * Update conversation title.
   */
  updateConversationTitle: async (conversationId: string, title: string): Promise<void> => {
    await patch<ApiResponse<boolean>, { title: string }>(
      `/chat/conversations/${conversationId}/title`,
      { title }
    );
  },

  /**
   * Get available tools for the chat assistant.
   */
  getAvailableTools: async (): Promise<ChatToolInfo[]> => {
    const response = await get<ApiResponse<ChatToolInfo[]>>('/chat/tools');
    return response.data;
  },
};
