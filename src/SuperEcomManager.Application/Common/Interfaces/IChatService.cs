using SuperEcomManager.Domain.Entities.Chat;

namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Conversation summary for listing.
/// </summary>
public record ChatConversationSummary
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public int MessageCount { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Message DTO for API responses.
/// </summary>
public record ChatMessageDto
{
    public Guid Id { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string? ToolName { get; init; }
    public string? ToolCalls { get; init; }
}

/// <summary>
/// Conversation detail with messages.
/// </summary>
public record ChatConversationDetail
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public int MessageCount { get; init; }
    public int TotalTokensUsed { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public List<ChatMessageDto> Messages { get; init; } = new();
}

/// <summary>
/// Service for managing chat conversations and messages.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Gets or creates a conversation.
    /// </summary>
    Task<ChatConversation> GetOrCreateConversationAsync(
        Guid? conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a conversation by ID.
    /// </summary>
    Task<ChatConversation?> GetConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets conversation detail with messages.
    /// </summary>
    Task<ChatConversationDetail?> GetConversationDetailAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user's conversation history (list).
    /// </summary>
    Task<List<ChatConversationSummary>> GetUserConversationsAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a message to a conversation.
    /// </summary>
    Task<ChatMessage> AddMessageAsync(
        Guid conversationId,
        string role,
        string content,
        string? toolCallId = null,
        string? toolName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records token usage for a conversation.
    /// </summary>
    Task RecordTokenUsageAsync(
        Guid conversationId,
        int tokensUsed,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conversation (soft delete).
    /// </summary>
    Task DeleteConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates conversation title.
    /// </summary>
    Task UpdateConversationTitleAsync(
        Guid conversationId,
        Guid userId,
        string newTitle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old conversations (for background job).
    /// </summary>
    Task CleanupOldConversationsAsync(
        int retentionDays = 30,
        CancellationToken cancellationToken = default);
}
