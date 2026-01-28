using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Registry for managing and executing chat tools from various providers.
/// </summary>
public class ChatToolRegistry : IChatToolRegistry
{
    private readonly List<IChatToolProvider> _providers = new();
    private readonly ILogger<ChatToolRegistry> _logger;

    public ChatToolRegistry(ILogger<ChatToolRegistry> logger)
    {
        _logger = logger;
    }

    public void RegisterProvider(IChatToolProvider provider)
    {
        _providers.Add(provider);
        _providers.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        _logger.LogInformation(
            "Registered chat tool provider: {Category} with {ToolCount} tools",
            provider.Category,
            provider.GetTools().Count);
    }

    public IReadOnlyList<ChatTool> GetAllTools()
    {
        return _providers
            .SelectMany(p => p.GetTools())
            .ToList()
            .AsReadOnly();
    }

    public IChatToolProvider? FindProvider(string toolName)
    {
        return _providers.FirstOrDefault(p => p.CanHandle(toolName));
    }

    public async Task<ChatToolResult> ExecuteToolAsync(
        ChatToolCall toolCall,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var provider = FindProvider(toolCall.Name);

        if (provider == null)
        {
            _logger.LogWarning("No provider found for tool: {ToolName}", toolCall.Name);
            return new ChatToolResult
            {
                ToolCallId = toolCall.Id,
                ToolName = toolCall.Name,
                IsSuccess = false,
                Error = $"Unknown tool: {toolCall.Name}",
                Content = $"Error: Tool '{toolCall.Name}' is not available."
            };
        }

        try
        {
            _logger.LogInformation(
                "Executing tool {ToolName} (provider: {Provider})",
                toolCall.Name,
                provider.Category);

            var result = await provider.ExecuteToolAsync(toolCall, context, cancellationToken);

            _logger.LogInformation(
                "Tool {ToolName} executed successfully: {Success}",
                toolCall.Name,
                result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolCall.Name);

            return new ChatToolResult
            {
                ToolCallId = toolCall.Id,
                ToolName = toolCall.Name,
                IsSuccess = false,
                Error = ex.Message,
                Content = $"Error executing tool: {ex.Message}"
            };
        }
    }
}
