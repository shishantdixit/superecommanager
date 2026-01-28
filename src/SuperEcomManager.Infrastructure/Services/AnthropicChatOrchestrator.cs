using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Configuration for the Anthropic AI integration.
/// </summary>
public class AnthropicSettings
{
    public const string SectionName = "Anthropic";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-20250514";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.7;
}

/// <summary>
/// Orchestrates AI chat conversations using Anthropic's Claude API.
/// Supports tool execution for shipping, orders, and other operations.
/// </summary>
public class AnthropicChatOrchestrator : IChatOrchestrator
{
    private readonly HttpClient _httpClient;
    private readonly IChatService _chatService;
    private readonly IChatToolRegistry _toolRegistry;
    private readonly ICurrentTenantService _tenantService;
    private readonly AnthropicSettings _settings;
    private readonly ILogger<AnthropicChatOrchestrator> _logger;

    private const string ApiBaseUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    private const string SystemPrompt = @"You are a helpful AI assistant for an eCommerce management platform. You help users manage their orders, shipments, inventory, and more.

You have access to tools that allow you to:
- Track shipments and check delivery status
- Check courier serviceability and rates
- Look up order information
- Answer questions about shipping and logistics

When users ask about shipments or orders, use the available tools to get real-time information.
Always be helpful, concise, and accurate. If you don't have enough information to answer a question, ask for clarification.

When presenting information from tools:
- Format data clearly and readably
- Highlight important information like tracking numbers, delivery dates, and costs
- Provide actionable suggestions when appropriate

Keep your responses focused and professional. This is a business tool used by eCommerce sellers.";

    public AnthropicChatOrchestrator(
        IHttpClientFactory httpClientFactory,
        IChatService chatService,
        IChatToolRegistry toolRegistry,
        ICurrentTenantService tenantService,
        IOptions<AnthropicSettings> settings,
        ILogger<AnthropicChatOrchestrator> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Anthropic");
        _chatService = chatService;
        _toolRegistry = toolRegistry;
        _tenantService = tenantService;
        _settings = settings.Value;
        _logger = logger;

        // Configure HttpClient headers
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);
    }

    public async Task<ChatResponse> ProcessMessageAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get or create conversation
            var conversation = await _chatService.GetOrCreateConversationAsync(
                request.ConversationId,
                request.UserId,
                cancellationToken);

            // Add user message to conversation
            await _chatService.AddMessageAsync(
                conversation.Id,
                ChatMessageRole.User.ToString(),
                request.Message,
                cancellationToken: cancellationToken);

            // Build messages for API call
            var messages = BuildMessagesFromConversation(conversation);

            // Get available tools
            var tools = BuildToolDefinitions();

            // Make API call
            var response = await CallAnthropicAsync(messages, tools, cancellationToken);

            // Process response and handle tool calls
            var chatResponse = await ProcessApiResponseAsync(
                conversation,
                response,
                request.UserId,
                messages,
                tools,
                cancellationToken);

            return chatResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            throw;
        }
    }

    public Task<List<ChatTool>> GetAvailableToolsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tools = _toolRegistry.GetAllTools().ToList();
        return Task.FromResult(tools);
    }

    private List<ApiMessage> BuildMessagesFromConversation(
        Domain.Entities.Chat.ChatConversation conversation)
    {
        var messages = new List<ApiMessage>();

        foreach (var msg in conversation.Messages.OrderBy(m => m.Sequence))
        {
            // Skip system and tool messages for now - handled separately
            if (msg.Role == ChatMessageRole.System || msg.Role == ChatMessageRole.Tool)
                continue;

            var role = msg.Role == ChatMessageRole.User ? "user" : "assistant";
            messages.Add(new ApiMessage
            {
                Role = role,
                Content = msg.Content
            });
        }

        return messages;
    }

    private List<ApiTool> BuildToolDefinitions()
    {
        var tools = _toolRegistry.GetAllTools();
        var apiTools = new List<ApiTool>();

        foreach (var tool in tools)
        {
            try
            {
                var inputSchema = JsonSerializer.Deserialize<JsonElement>(tool.InputSchema);

                apiTools.Add(new ApiTool
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    InputSchema = inputSchema
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse tool schema for {ToolName}", tool.Name);
            }
        }

        return apiTools;
    }

    private async Task<ApiResponse> CallAnthropicAsync(
        List<ApiMessage> messages,
        List<ApiTool> tools,
        CancellationToken cancellationToken)
    {
        var request = new ApiRequest
        {
            Model = _settings.Model,
            MaxTokens = _settings.MaxTokens,
            System = SystemPrompt,
            Messages = messages,
            Tools = tools.Count > 0 ? tools : null,
            Temperature = _settings.Temperature
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(request, jsonOptions);
        _logger.LogDebug("Anthropic request: {Request}", json);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(ApiBaseUrl, content, cancellationToken);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("Anthropic response: {Response}", responseJson);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Anthropic API error: {StatusCode} - {Response}",
                response.StatusCode, responseJson);
            throw new Exception($"Anthropic API error: {response.StatusCode} - {responseJson}");
        }

        return JsonSerializer.Deserialize<ApiResponse>(responseJson, jsonOptions)
            ?? throw new Exception("Failed to parse Anthropic response");
    }

    private async Task<ChatResponse> ProcessApiResponseAsync(
        Domain.Entities.Chat.ChatConversation conversation,
        ApiResponse response,
        Guid userId,
        List<ApiMessage> messages,
        List<ApiTool> tools,
        CancellationToken cancellationToken)
    {
        var toolCalls = new List<ChatToolCall>();
        var textContent = string.Empty;

        // Process response content
        if (response.Content != null)
        {
            foreach (var contentItem in response.Content)
            {
                if (contentItem.Type == "text")
                {
                    textContent += contentItem.Text ?? string.Empty;
                }
                else if (contentItem.Type == "tool_use")
                {
                    toolCalls.Add(new ChatToolCall
                    {
                        Id = contentItem.Id ?? string.Empty,
                        Name = contentItem.Name ?? string.Empty,
                        Arguments = contentItem.Input.HasValue
                            ? contentItem.Input.Value.GetRawText()
                            : "{}"
                    });
                }
            }
        }

        // If there are tool calls and stop reason is tool_use, execute them and continue
        if (toolCalls.Count > 0 && response.StopReason == "tool_use")
        {
            return await HandleToolCallsAsync(
                conversation,
                textContent,
                toolCalls,
                response,
                userId,
                messages,
                tools,
                cancellationToken);
        }

        // No tool calls - save assistant response and return
        if (!string.IsNullOrEmpty(textContent))
        {
            await _chatService.AddMessageAsync(
                conversation.Id,
                ChatMessageRole.Assistant.ToString(),
                textContent,
                cancellationToken: cancellationToken);
        }

        // Record token usage
        var tokensUsed = (response.Usage?.InputTokens ?? 0) + (response.Usage?.OutputTokens ?? 0);
        await _chatService.RecordTokenUsageAsync(conversation.Id, tokensUsed, cancellationToken);

        return new ChatResponse
        {
            ConversationId = conversation.Id,
            Message = textContent,
            TokensUsed = tokensUsed
        };
    }

    private async Task<ChatResponse> HandleToolCallsAsync(
        Domain.Entities.Chat.ChatConversation conversation,
        string partialText,
        List<ChatToolCall> toolCalls,
        ApiResponse originalResponse,
        Guid userId,
        List<ApiMessage> messages,
        List<ApiTool> tools,
        CancellationToken cancellationToken)
    {
        // Save assistant message with tool calls
        var assistantMessage = await _chatService.AddMessageAsync(
            conversation.Id,
            ChatMessageRole.Assistant.ToString(),
            partialText,
            cancellationToken: cancellationToken);

        assistantMessage.SetToolCalls(JsonSerializer.Serialize(toolCalls));

        // Execute tools
        var tenantId = _tenantService.HasTenant ? _tenantService.TenantId : Guid.Empty;
        var context = new ToolExecutionContext
        {
            TenantId = tenantId,
            UserId = userId,
            ConversationId = conversation.Id
        };

        var toolResults = new List<ChatToolResult>();
        foreach (var toolCall in toolCalls)
        {
            var result = await _toolRegistry.ExecuteToolAsync(toolCall, context, cancellationToken);
            toolResults.Add(result);

            // Save tool result as message
            await _chatService.AddMessageAsync(
                conversation.Id,
                ChatMessageRole.Tool.ToString(),
                result.Content,
                result.ToolCallId,
                result.ToolName,
                cancellationToken);
        }

        // Build follow-up messages with tool results
        var updatedMessages = BuildMessagesWithToolResults(messages, originalResponse, toolResults);

        // Get follow-up response from AI
        var followUpResponse = await CallAnthropicAsync(updatedMessages, tools, cancellationToken);

        // Process follow-up response (may recurse if more tool calls)
        return await ProcessApiResponseAsync(
            conversation,
            followUpResponse,
            userId,
            updatedMessages,
            tools,
            cancellationToken);
    }

    private List<ApiMessage> BuildMessagesWithToolResults(
        List<ApiMessage> messages,
        ApiResponse assistantResponse,
        List<ChatToolResult> toolResults)
    {
        var result = new List<ApiMessage>(messages);

        // Add the assistant message with tool use (as content array)
        result.Add(new ApiMessage
        {
            Role = "assistant",
            ContentArray = assistantResponse.Content
        });

        // Add tool results as user message
        var toolResultContent = toolResults.Select(r => new ApiContentItem
        {
            Type = "tool_result",
            ToolUseId = r.ToolCallId,
            Content = r.Content
        }).ToList();

        result.Add(new ApiMessage
        {
            Role = "user",
            ContentArray = toolResultContent
        });

        return result;
    }
}

#region Anthropic API Models

internal class ApiRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("messages")]
    public List<ApiMessage> Messages { get; set; } = new();

    [JsonPropertyName("tools")]
    public List<ApiTool>? Tools { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
}

internal class ApiMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    // For simple text content
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }

    // For complex content (tool use, tool results)
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ApiContentItem>? ContentArray { get; set; }

    // Custom serialization to handle both content types
    public bool ShouldSerializeContent() => Content != null && ContentArray == null;
    public bool ShouldSerializeContentArray() => ContentArray != null;
}

internal class ApiContentItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("input")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Input { get; set; }

    [JsonPropertyName("tool_use_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolUseId { get; set; }

    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }
}

internal class ApiTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("input_schema")]
    public JsonElement InputSchema { get; set; }
}

internal class ApiResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public List<ApiContentItem>? Content { get; set; }

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("usage")]
    public ApiUsage? Usage { get; set; }
}

internal class ApiUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
}

#endregion
