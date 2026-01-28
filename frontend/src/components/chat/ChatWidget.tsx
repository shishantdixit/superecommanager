'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import { cn } from '@/lib/utils';
import { Button, Input } from '@/components/ui';
import {
  MessageSquare,
  X,
  Send,
  Loader2,
  Trash2,
  ChevronLeft,
  History,
  Sparkles,
  Bot,
  User,
} from 'lucide-react';
import {
  chatService,
  type ChatMessageDto,
  type ChatConversationSummary,
} from '@/services/chat.service';
import { toast } from 'sonner';

interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  createdAt: Date;
}

export function ChatWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [showHistory, setShowHistory] = useState(false);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [inputValue, setInputValue] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [conversations, setConversations] = useState<ChatConversationSummary[]>([]);
  const [loadingHistory, setLoadingHistory] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Scroll to bottom when messages change
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Focus input when chat opens
  useEffect(() => {
    if (isOpen && !showHistory) {
      inputRef.current?.focus();
    }
  }, [isOpen, showHistory]);

  // Load conversation history
  const loadConversations = useCallback(async () => {
    setLoadingHistory(true);
    try {
      const data = await chatService.getConversations();
      setConversations(data);
    } catch (error) {
      console.error('Failed to load conversations:', error);
    } finally {
      setLoadingHistory(false);
    }
  }, []);

  // Load a specific conversation
  const loadConversation = useCallback(async (id: string) => {
    setIsLoading(true);
    try {
      const data = await chatService.getConversation(id);
      setConversationId(id);
      setMessages(
        data.messages
          .filter((m) => m.role === 'User' || m.role === 'Assistant')
          .map((m) => ({
            id: m.id,
            role: m.role === 'User' ? 'user' : 'assistant',
            content: m.content,
            createdAt: new Date(m.createdAt),
          }))
      );
      setShowHistory(false);
    } catch (error) {
      console.error('Failed to load conversation:', error);
      toast.error('Failed to load conversation');
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Send message
  const sendMessage = useCallback(async () => {
    if (!inputValue.trim() || isLoading) return;

    const userMessage: Message = {
      id: `temp-${Date.now()}`,
      role: 'user',
      content: inputValue.trim(),
      createdAt: new Date(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setInputValue('');
    setIsLoading(true);

    try {
      const response = await chatService.sendMessage({
        conversationId: conversationId || undefined,
        message: userMessage.content,
      });

      // Update conversation ID if this is a new conversation
      if (!conversationId) {
        setConversationId(response.conversationId);
      }

      // Add assistant message
      const assistantMessage: Message = {
        id: `assistant-${Date.now()}`,
        role: 'assistant',
        content: response.message,
        createdAt: new Date(),
      };

      setMessages((prev) => [...prev, assistantMessage]);
    } catch (error: any) {
      console.error('Failed to send message:', error);
      toast.error(error?.message || 'Failed to send message');
      // Remove the user message on error
      setMessages((prev) => prev.filter((m) => m.id !== userMessage.id));
      setInputValue(userMessage.content);
    } finally {
      setIsLoading(false);
    }
  }, [inputValue, isLoading, conversationId]);

  // Start new conversation
  const startNewConversation = useCallback(() => {
    setConversationId(null);
    setMessages([]);
    setShowHistory(false);
    inputRef.current?.focus();
  }, []);

  // Delete conversation
  const deleteConversation = useCallback(async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await chatService.deleteConversation(id);
      setConversations((prev) => prev.filter((c) => c.id !== id));
      if (conversationId === id) {
        startNewConversation();
      }
      toast.success('Conversation deleted');
    } catch (error) {
      console.error('Failed to delete conversation:', error);
      toast.error('Failed to delete conversation');
    }
  }, [conversationId, startNewConversation]);

  // Handle key press
  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  };

  // Toggle history view
  const toggleHistory = () => {
    if (!showHistory) {
      loadConversations();
    }
    setShowHistory(!showHistory);
  };

  return (
    <>
      {/* Chat button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className={cn(
          'fixed bottom-6 right-6 z-50 flex h-14 w-14 items-center justify-center rounded-full shadow-lg transition-all',
          'bg-primary text-primary-foreground hover:bg-primary-hover',
          isOpen && 'scale-0 opacity-0'
        )}
        aria-label="Open chat assistant"
      >
        <MessageSquare className="h-6 w-6" />
      </button>

      {/* Chat window */}
      <div
        className={cn(
          'fixed bottom-6 right-6 z-50 flex flex-col overflow-hidden rounded-xl bg-card shadow-2xl border border-border transition-all duration-300',
          'w-[380px] h-[600px]',
          isOpen ? 'scale-100 opacity-100' : 'scale-95 opacity-0 pointer-events-none'
        )}
      >
        {/* Header */}
        <div className="flex items-center justify-between border-b border-border bg-primary px-4 py-3">
          <div className="flex items-center gap-2">
            {showHistory && (
              <button
                onClick={() => setShowHistory(false)}
                className="text-primary-foreground/80 hover:text-primary-foreground"
              >
                <ChevronLeft className="h-5 w-5" />
              </button>
            )}
            <Bot className="h-5 w-5 text-primary-foreground" />
            <span className="font-semibold text-primary-foreground">
              {showHistory ? 'Chat History' : 'AI Assistant'}
            </span>
          </div>
          <div className="flex items-center gap-2">
            {!showHistory && (
              <>
                <button
                  onClick={startNewConversation}
                  className="rounded p-1 text-primary-foreground/80 hover:bg-white/10 hover:text-primary-foreground"
                  title="New conversation"
                >
                  <Sparkles className="h-4 w-4" />
                </button>
                <button
                  onClick={toggleHistory}
                  className="rounded p-1 text-primary-foreground/80 hover:bg-white/10 hover:text-primary-foreground"
                  title="Chat history"
                >
                  <History className="h-4 w-4" />
                </button>
              </>
            )}
            <button
              onClick={() => setIsOpen(false)}
              className="rounded p-1 text-primary-foreground/80 hover:bg-white/10 hover:text-primary-foreground"
            >
              <X className="h-5 w-5" />
            </button>
          </div>
        </div>

        {/* Content */}
        {showHistory ? (
          /* History view */
          <div className="flex-1 overflow-auto p-4">
            {loadingHistory ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              </div>
            ) : conversations.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-8 text-center">
                <History className="h-12 w-12 text-muted-foreground/50 mb-3" />
                <p className="text-muted-foreground">No conversations yet</p>
                <button
                  onClick={startNewConversation}
                  className="mt-4 text-primary hover:underline text-sm"
                >
                  Start a new conversation
                </button>
              </div>
            ) : (
              <div className="space-y-2">
                {conversations.map((conv) => (
                  <div
                    key={conv.id}
                    onClick={() => loadConversation(conv.id)}
                    className={cn(
                      'flex items-center justify-between rounded-lg border border-border p-3 cursor-pointer transition-colors',
                      'hover:bg-muted',
                      conversationId === conv.id && 'bg-primary/5 border-primary'
                    )}
                  >
                    <div className="flex-1 min-w-0">
                      <p className="font-medium text-sm truncate">{conv.title}</p>
                      <p className="text-xs text-muted-foreground">
                        {conv.messageCount} messages · {new Date(conv.createdAt).toLocaleDateString()}
                      </p>
                    </div>
                    <button
                      onClick={(e) => deleteConversation(conv.id, e)}
                      className="ml-2 p-1 text-muted-foreground hover:text-error rounded"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        ) : (
          /* Chat view */
          <>
            {/* Messages */}
            <div className="flex-1 overflow-auto p-4 space-y-4">
              {messages.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-full text-center">
                  <div className="rounded-full bg-primary/10 p-4 mb-4">
                    <Bot className="h-8 w-8 text-primary" />
                  </div>
                  <h3 className="font-semibold mb-2">How can I help you?</h3>
                  <p className="text-sm text-muted-foreground max-w-[280px]">
                    Ask me about shipments, orders, courier rates, or any shipping-related questions.
                  </p>
                  <div className="mt-4 space-y-2 w-full max-w-[280px]">
                    {[
                      'Track my latest shipment',
                      'Check delivery rates to Mumbai',
                      'Show shipment statistics',
                    ].map((suggestion) => (
                      <button
                        key={suggestion}
                        onClick={() => {
                          setInputValue(suggestion);
                          inputRef.current?.focus();
                        }}
                        className="w-full text-left text-sm px-3 py-2 rounded-lg border border-border hover:bg-muted transition-colors"
                      >
                        {suggestion}
                      </button>
                    ))}
                  </div>
                </div>
              ) : (
                messages.map((message) => (
                  <div
                    key={message.id}
                    className={cn(
                      'flex gap-3',
                      message.role === 'user' ? 'justify-end' : 'justify-start'
                    )}
                  >
                    {message.role === 'assistant' && (
                      <div className="flex-shrink-0 w-7 h-7 rounded-full bg-primary/10 flex items-center justify-center">
                        <Bot className="h-4 w-4 text-primary" />
                      </div>
                    )}
                    <div
                      className={cn(
                        'max-w-[85%] rounded-lg px-4 py-2',
                        message.role === 'user'
                          ? 'bg-primary text-primary-foreground'
                          : 'bg-muted'
                      )}
                    >
                      <div
                        className={cn(
                          'text-sm whitespace-pre-wrap',
                          message.role === 'assistant' && 'prose prose-sm dark:prose-invert max-w-none'
                        )}
                        dangerouslySetInnerHTML={
                          message.role === 'assistant'
                            ? { __html: formatMarkdown(message.content) }
                            : undefined
                        }
                      >
                        {message.role === 'user' ? message.content : undefined}
                      </div>
                    </div>
                    {message.role === 'user' && (
                      <div className="flex-shrink-0 w-7 h-7 rounded-full bg-primary flex items-center justify-center">
                        <User className="h-4 w-4 text-primary-foreground" />
                      </div>
                    )}
                  </div>
                ))
              )}
              {isLoading && (
                <div className="flex gap-3 justify-start">
                  <div className="flex-shrink-0 w-7 h-7 rounded-full bg-primary/10 flex items-center justify-center">
                    <Bot className="h-4 w-4 text-primary" />
                  </div>
                  <div className="bg-muted rounded-lg px-4 py-3">
                    <div className="flex gap-1">
                      <span className="w-2 h-2 bg-muted-foreground/50 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
                      <span className="w-2 h-2 bg-muted-foreground/50 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
                      <span className="w-2 h-2 bg-muted-foreground/50 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
                    </div>
                  </div>
                </div>
              )}
              <div ref={messagesEndRef} />
            </div>

            {/* Input */}
            <div className="border-t border-border p-4">
              <div className="flex gap-2">
                <input
                  ref={inputRef}
                  type="text"
                  value={inputValue}
                  onChange={(e) => setInputValue(e.target.value)}
                  onKeyPress={handleKeyPress}
                  placeholder="Type your message..."
                  disabled={isLoading}
                  className={cn(
                    'flex-1 rounded-lg border border-border bg-background px-4 py-2 text-sm',
                    'focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent',
                    'disabled:opacity-50 disabled:cursor-not-allowed'
                  )}
                />
                <button
                  onClick={sendMessage}
                  disabled={!inputValue.trim() || isLoading}
                  className={cn(
                    'flex items-center justify-center rounded-lg bg-primary px-4 py-2 text-primary-foreground',
                    'hover:bg-primary-hover transition-colors',
                    'disabled:opacity-50 disabled:cursor-not-allowed'
                  )}
                >
                  {isLoading ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <Send className="h-4 w-4" />
                  )}
                </button>
              </div>
              <p className="mt-2 text-xs text-muted-foreground text-center">
                AI-powered assistant for shipping and order management
              </p>
            </div>
          </>
        )}
      </div>
    </>
  );
}

/**
 * Simple markdown formatting for assistant messages.
 */
function formatMarkdown(text: string): string {
  return text
    // Bold
    .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
    // Headers
    .replace(/^### (.+)$/gm, '<h4 class="font-semibold mt-2">$1</h4>')
    .replace(/^## (.+)$/gm, '<h3 class="font-semibold mt-2 text-base">$1</h3>')
    // Lists
    .replace(/^- (.+)$/gm, '<li class="ml-4">$1</li>')
    // Line breaks
    .replace(/\n/g, '<br/>');
}
