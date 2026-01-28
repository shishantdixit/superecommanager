using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Controller for AI chat assistant operations.
/// </summary>
[Authorize]
public class ChatController : ApiControllerBase
{
    private readonly IChatOrchestrator _chatOrchestrator;
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatOrchestrator chatOrchestrator,
        IChatService chatService,
        ILogger<ChatController> logger)
    {
        _chatOrchestrator = chatOrchestrator;
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the AI assistant.
    /// </summary>
    [HttpPost("message")]
    [ProducesResponseType(typeof(ApiResponse<ChatMessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChatMessageResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ChatMessageResponse>>> SendMessage(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!CurrentUserId.HasValue)
        {
            return BadRequestResponse<ChatMessageResponse>("User not authenticated");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequestResponse<ChatMessageResponse>("Message cannot be empty");
        }

        try
        {
            var chatRequest = new ChatRequest
            {
                ConversationId = request.ConversationId,
                Message = request.Message,
                UserId = CurrentUserId.Value
            };

            var response = await _chatOrchestrator.ProcessMessageAsync(chatRequest, cancellationToken);

            var result = new ChatMessageResponse
            {
                ConversationId = response.ConversationId,
                Message = response.Message,
                TokensUsed = response.TokensUsed
            };

            return OkResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return BadRequestResponse<ChatMessageResponse>($"Failed to process message: {ex.Message}");
        }
    }

    /// <summary>
    /// Get conversation history for the current user.
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(ApiResponse<List<ChatConversationSummary>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ChatConversationSummary>>>> GetConversations(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (!CurrentUserId.HasValue)
        {
            return BadRequestResponse<List<ChatConversationSummary>>("User not authenticated");
        }

        var conversations = await _chatService.GetUserConversationsAsync(
            CurrentUserId.Value,
            limit,
            cancellationToken);

        return OkResponse(conversations);
    }

    /// <summary>
    /// Get conversation details with messages.
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ChatConversationDetail>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChatConversationDetail>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ChatConversationDetail>>> GetConversation(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        if (!CurrentUserId.HasValue)
        {
            return BadRequestResponse<ChatConversationDetail>("User not authenticated");
        }

        var conversation = await _chatService.GetConversationDetailAsync(
            conversationId,
            CurrentUserId.Value,
            cancellationToken);

        if (conversation == null)
        {
            return NotFoundResponse<ChatConversationDetail>("Conversation not found");
        }

        return OkResponse(conversation);
    }

    /// <summary>
    /// Delete a conversation.
    /// </summary>
    [HttpDelete("conversations/{conversationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteConversation(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        if (!CurrentUserId.HasValue)
        {
            return BadRequestResponse<bool>("User not authenticated");
        }

        await _chatService.DeleteConversationAsync(
            conversationId,
            CurrentUserId.Value,
            cancellationToken);

        return OkResponse(true, "Conversation deleted successfully");
    }

    /// <summary>
    /// Update conversation title.
    /// </summary>
    [HttpPatch("conversations/{conversationId:guid}/title")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateConversationTitle(
        Guid conversationId,
        [FromBody] UpdateTitleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!CurrentUserId.HasValue)
        {
            return BadRequestResponse<bool>("User not authenticated");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequestResponse<bool>("Title cannot be empty");
        }

        await _chatService.UpdateConversationTitleAsync(
            conversationId,
            CurrentUserId.Value,
            request.Title,
            cancellationToken);

        return OkResponse(true, "Title updated successfully");
    }

    /// <summary>
    /// Get available tools for the chat assistant.
    /// </summary>
    [HttpGet("tools")]
    [ProducesResponseType(typeof(ApiResponse<List<ChatToolInfo>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ChatToolInfo>>>> GetAvailableTools(
        CancellationToken cancellationToken = default)
    {
        if (!CurrentUserId.HasValue)
        {
            return BadRequestResponse<List<ChatToolInfo>>("User not authenticated");
        }

        var tools = await _chatOrchestrator.GetAvailableToolsAsync(CurrentUserId.Value, cancellationToken);

        var toolInfos = tools.Select(t => new ChatToolInfo
        {
            Name = t.Name,
            Description = t.Description,
            Category = t.Category
        }).ToList();

        return OkResponse(toolInfos);
    }
}

#region Request/Response Models

/// <summary>
/// Request to send a message to the AI assistant.
/// </summary>
public record SendMessageRequest
{
    /// <summary>
    /// Optional conversation ID. If null, a new conversation will be created.
    /// </summary>
    public Guid? ConversationId { get; init; }

    /// <summary>
    /// The user's message.
    /// </summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Response from the AI assistant.
/// </summary>
public record ChatMessageResponse
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
    /// Tokens used for this request.
    /// </summary>
    public int TokensUsed { get; init; }
}

/// <summary>
/// Request to update conversation title.
/// </summary>
public record UpdateTitleRequest
{
    /// <summary>
    /// The new title for the conversation.
    /// </summary>
    public string Title { get; init; } = string.Empty;
}

/// <summary>
/// Information about an available tool.
/// </summary>
public record ChatToolInfo
{
    /// <summary>
    /// The tool name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of what the tool does.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Tool category.
    /// </summary>
    public string Category { get; init; } = string.Empty;
}

#endregion
