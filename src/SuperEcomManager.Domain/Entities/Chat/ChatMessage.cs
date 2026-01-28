using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.Chat;

/// <summary>
/// Represents a single message in a chat conversation.
/// </summary>
public class ChatMessage : BaseEntity
{
    /// <summary>
    /// The conversation this message belongs to.
    /// </summary>
    public Guid ConversationId { get; private set; }

    /// <summary>
    /// The role of the message sender.
    /// </summary>
    public ChatMessageRole Role { get; private set; }

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Sequence number for ordering messages within a conversation.
    /// </summary>
    public int Sequence { get; private set; }

    /// <summary>
    /// Timestamp when the message was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Number of tokens used for this message (input or output).
    /// </summary>
    public int? TokenCount { get; private set; }

    /// <summary>
    /// For tool messages: the ID of the tool call this is responding to.
    /// </summary>
    public string? ToolCallId { get; private set; }

    /// <summary>
    /// For tool messages: the name of the tool that was called.
    /// </summary>
    public string? ToolName { get; private set; }

    /// <summary>
    /// For assistant messages: tool calls requested (stored as JSON).
    /// </summary>
    public string? ToolCalls { get; private set; }

    /// <summary>
    /// Additional metadata stored as JSON.
    /// </summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// Navigation property to the conversation.
    /// </summary>
    public ChatConversation? Conversation { get; private set; }

    private ChatMessage() { } // EF Core constructor

    /// <summary>
    /// Creates a new chat message.
    /// </summary>
    internal static ChatMessage Create(
        Guid conversationId,
        ChatMessageRole role,
        string content,
        int sequence,
        string? toolCallId = null,
        string? toolName = null)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = role,
            Content = content,
            Sequence = sequence,
            ToolCallId = toolCallId,
            ToolName = toolName,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Sets the token count for this message.
    /// </summary>
    public void SetTokenCount(int tokenCount)
    {
        TokenCount = tokenCount;
    }

    /// <summary>
    /// Sets tool calls for an assistant message.
    /// </summary>
    public void SetToolCalls(string? toolCallsJson)
    {
        ToolCalls = toolCallsJson;
    }

    /// <summary>
    /// Sets metadata for this message.
    /// </summary>
    public void SetMetadata(string? metadata)
    {
        Metadata = metadata;
    }
}
