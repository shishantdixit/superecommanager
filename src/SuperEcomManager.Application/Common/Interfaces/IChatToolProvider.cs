namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Context for tool execution containing tenant and user information.
/// </summary>
public record ToolExecutionContext
{
    /// <summary>
    /// The tenant ID for the current context.
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    /// The user ID executing the tool.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// The conversation ID.
    /// </summary>
    public Guid ConversationId { get; init; }

    /// <summary>
    /// Additional context data (JSON).
    /// </summary>
    public string? AdditionalContext { get; init; }
}

/// <summary>
/// Interface for providing tools to the chat assistant.
/// Implement this to add new tool categories (shipping, orders, inventory, etc.).
/// </summary>
public interface IChatToolProvider
{
    /// <summary>
    /// The category name for this tool provider (e.g., "Shipping", "Orders").
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Priority for tool providers (lower = higher priority).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets all tools provided by this provider.
    /// </summary>
    IReadOnlyList<ChatTool> GetTools();

    /// <summary>
    /// Checks if this provider can handle the specified tool.
    /// </summary>
    bool CanHandle(string toolName);

    /// <summary>
    /// Executes a tool call and returns the result.
    /// </summary>
    Task<ChatToolResult> ExecuteToolAsync(
        ChatToolCall toolCall,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Registry for managing chat tool providers.
/// </summary>
public interface IChatToolRegistry
{
    /// <summary>
    /// Registers a tool provider.
    /// </summary>
    void RegisterProvider(IChatToolProvider provider);

    /// <summary>
    /// Gets all registered tools.
    /// </summary>
    IReadOnlyList<ChatTool> GetAllTools();

    /// <summary>
    /// Finds a provider that can handle the specified tool.
    /// </summary>
    IChatToolProvider? FindProvider(string toolName);

    /// <summary>
    /// Executes a tool using the appropriate provider.
    /// </summary>
    Task<ChatToolResult> ExecuteToolAsync(
        ChatToolCall toolCall,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default);
}
