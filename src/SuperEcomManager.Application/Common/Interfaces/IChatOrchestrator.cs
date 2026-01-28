namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Represents a tool that can be called by the AI assistant.
/// </summary>
public record ChatTool
{
    /// <summary>
    /// Unique identifier for the tool.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of what the tool does (shown to AI).
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// JSON schema for the tool's input parameters.
    /// </summary>
    public string InputSchema { get; init; } = "{}";

    /// <summary>
    /// Category of the tool for organization.
    /// </summary>
    public string Category { get; init; } = "General";
}

/// <summary>
/// Represents a tool call requested by the AI.
/// </summary>
public record ChatToolCall
{
    /// <summary>
    /// Unique ID for this specific tool call.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Name of the tool to execute.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Arguments to pass to the tool (JSON).
    /// </summary>
    public string Arguments { get; init; } = "{}";
}

/// <summary>
/// Represents the result of executing a tool.
/// </summary>
public record ChatToolResult
{
    /// <summary>
    /// ID of the tool call this result corresponds to.
    /// </summary>
    public string ToolCallId { get; init; } = string.Empty;

    /// <summary>
    /// Name of the tool that was executed.
    /// </summary>
    public string ToolName { get; init; } = string.Empty;

    /// <summary>
    /// The result content (text/JSON).
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Whether the tool execution was successful.
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Error message if the tool execution failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Request to send a message to the chat assistant.
/// </summary>
public record ChatRequest
{
    /// <summary>
    /// The conversation ID (null for new conversation).
    /// </summary>
    public Guid? ConversationId { get; init; }

    /// <summary>
    /// The user's message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// User ID making the request.
    /// </summary>
    public Guid UserId { get; init; }
}

/// <summary>
/// Response from the chat assistant.
/// </summary>
public record ChatResponse
{
    /// <summary>
    /// The conversation ID.
    /// </summary>
    public Guid ConversationId { get; init; }

    /// <summary>
    /// The assistant's response message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Tool calls requested by the assistant (if any).
    /// </summary>
    public List<ChatToolCall> ToolCalls { get; init; } = new();

    /// <summary>
    /// Whether the response requires tool execution.
    /// </summary>
    public bool RequiresToolExecution => ToolCalls.Count > 0;

    /// <summary>
    /// Results of tool executions (populated after tools are run).
    /// </summary>
    public List<ChatToolResult> ToolResults { get; init; } = new();

    /// <summary>
    /// Token usage for this request.
    /// </summary>
    public int TokensUsed { get; init; }
}

/// <summary>
/// Orchestrates AI chat conversations with tool execution support.
/// </summary>
public interface IChatOrchestrator
{
    /// <summary>
    /// Processes a chat message and returns the assistant's response.
    /// Handles tool execution automatically when needed.
    /// </summary>
    Task<ChatResponse> ProcessMessageAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available tools for the current tenant/user context.
    /// </summary>
    Task<List<ChatTool>> GetAvailableToolsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
