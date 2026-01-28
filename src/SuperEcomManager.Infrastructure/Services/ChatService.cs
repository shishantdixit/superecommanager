using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Entities.Chat;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Service for managing chat conversations and messages.
/// </summary>
public class ChatService : IChatService
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        ITenantDbContext dbContext,
        ILogger<ChatService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ChatConversation> GetOrCreateConversationAsync(
        Guid? conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (conversationId.HasValue)
        {
            var existing = await _dbContext.ChatConversations
                .Include(c => c.Messages.OrderBy(m => m.Sequence))
                .FirstOrDefaultAsync(
                    c => c.Id == conversationId.Value && c.UserId == userId && c.Status == ChatConversationStatus.Active,
                    cancellationToken);

            if (existing != null)
                return existing;
        }

        // Create new conversation
        var conversation = ChatConversation.Create(userId);
        _dbContext.ChatConversations.Add(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new chat conversation {ConversationId} for user {UserId}",
            conversation.Id, userId);

        return conversation;
    }

    public async Task<ChatConversation?> GetConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatConversations
            .Include(c => c.Messages.OrderBy(m => m.Sequence))
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
    }

    public async Task<ChatConversationDetail?> GetConversationDetailAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.ChatConversations
            .Include(c => c.Messages.OrderBy(m => m.Sequence))
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && c.UserId == userId,
                cancellationToken);

        if (conversation == null)
            return null;

        return new ChatConversationDetail
        {
            Id = conversation.Id,
            Title = conversation.Title,
            MessageCount = conversation.MessageCount,
            TotalTokensUsed = conversation.TotalTokensUsed,
            CreatedAt = conversation.CreatedAt,
            LastMessageAt = conversation.LastMessageAt,
            Messages = conversation.Messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role.ToString(),
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                ToolName = m.ToolName,
                ToolCalls = m.ToolCalls
            }).ToList()
        };
    }

    public async Task<List<ChatConversationSummary>> GetUserConversationsAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatConversations
            .Where(c => c.UserId == userId && c.Status == ChatConversationStatus.Active)
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .Take(limit)
            .Select(c => new ChatConversationSummary
            {
                Id = c.Id,
                Title = c.Title,
                MessageCount = c.MessageCount,
                LastMessageAt = c.LastMessageAt,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatMessage> AddMessageAsync(
        Guid conversationId,
        string role,
        string content,
        string? toolCallId = null,
        string? toolName = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.ChatConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
            throw new InvalidOperationException($"Conversation {conversationId} not found");

        var messageRole = Enum.Parse<ChatMessageRole>(role, ignoreCase: true);
        var message = conversation.AddMessage(messageRole, content, toolCallId, toolName);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return message;
    }

    public async Task RecordTokenUsageAsync(
        Guid conversationId,
        int tokensUsed,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation != null)
        {
            conversation.AddTokenUsage(tokensUsed);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.ChatConversations
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && c.UserId == userId,
                cancellationToken);

        if (conversation != null)
        {
            conversation.Delete();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted chat conversation {ConversationId}", conversationId);
        }
    }

    public async Task UpdateConversationTitleAsync(
        Guid conversationId,
        Guid userId,
        string newTitle,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.ChatConversations
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && c.UserId == userId,
                cancellationToken);

        if (conversation != null)
        {
            conversation.UpdateTitle(newTitle);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CleanupOldConversationsAsync(
        int retentionDays = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var oldConversations = await _dbContext.ChatConversations
            .Where(c => c.Status == ChatConversationStatus.Active &&
                       (c.LastMessageAt ?? c.CreatedAt) < cutoffDate)
            .ToListAsync(cancellationToken);

        foreach (var conversation in oldConversations)
        {
            conversation.Archive();
        }

        if (oldConversations.Any())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Archived {Count} old conversations", oldConversations.Count);
        }
    }
}
