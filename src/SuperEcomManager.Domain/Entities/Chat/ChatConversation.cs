using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.Chat;

/// <summary>
/// Represents a chat conversation session between a user and the AI assistant.
/// Conversations are tenant-isolated and support history persistence.
/// </summary>
public class ChatConversation : AuditableEntity
{
    /// <summary>
    /// User-friendly title for the conversation (auto-generated from first message).
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// The user who owns this conversation.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Current status of the conversation.
    /// </summary>
    public ChatConversationStatus Status { get; private set; }

    /// <summary>
    /// Total number of messages in this conversation.
    /// </summary>
    public int MessageCount { get; private set; }

    /// <summary>
    /// Total tokens used in this conversation (for billing/tracking).
    /// </summary>
    public int TotalTokensUsed { get; private set; }

    /// <summary>
    /// Timestamp of the last message in this conversation.
    /// </summary>
    public DateTime? LastMessageAt { get; private set; }

    /// <summary>
    /// Metadata stored as JSON (e.g., context, preferences).
    /// </summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// Collection of messages in this conversation.
    /// </summary>
    private readonly List<ChatMessage> _messages = new();
    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

    private ChatConversation() { } // EF Core constructor

    /// <summary>
    /// Creates a new chat conversation.
    /// </summary>
    public static ChatConversation Create(Guid userId, string? initialTitle = null)
    {
        return new ChatConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = initialTitle ?? "New Conversation",
            Status = ChatConversationStatus.Active,
            MessageCount = 0,
            TotalTokensUsed = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Adds a message to this conversation.
    /// </summary>
    public ChatMessage AddMessage(ChatMessageRole role, string content, string? toolCallId = null, string? toolName = null)
    {
        var message = ChatMessage.Create(Id, role, content, MessageCount, toolCallId, toolName);
        _messages.Add(message);
        MessageCount++;
        LastMessageAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Auto-generate title from first user message if still default
        if (Title == "New Conversation" && role == ChatMessageRole.User && !string.IsNullOrWhiteSpace(content))
        {
            UpdateTitle(GenerateTitleFromContent(content));
        }

        return message;
    }

    /// <summary>
    /// Updates the conversation title.
    /// </summary>
    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        Title = title.Length > 100 ? title[..100] : title;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records token usage for billing/tracking.
    /// </summary>
    public void AddTokenUsage(int tokens)
    {
        TotalTokensUsed += tokens;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets metadata for the conversation.
    /// </summary>
    public void SetMetadata(string? metadata)
    {
        Metadata = metadata;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Archives the conversation.
    /// </summary>
    public void Archive()
    {
        Status = ChatConversationStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the conversation as deleted.
    /// </summary>
    public void Delete()
    {
        Status = ChatConversationStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates an archived conversation.
    /// </summary>
    public void Reactivate()
    {
        if (Status == ChatConversationStatus.Deleted)
            throw new InvalidOperationException("Cannot reactivate a deleted conversation");

        Status = ChatConversationStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Generates a title from the message content.
    /// </summary>
    private static string GenerateTitleFromContent(string content)
    {
        // Take first sentence or first 50 characters
        var firstSentence = content.Split(new[] { '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.Trim() ?? content;

        if (firstSentence.Length > 50)
            return firstSentence[..47] + "...";

        return firstSentence;
    }
}
