namespace SuperEcomManager.Domain.Enums;

/// <summary>
/// Role of a message sender in a chat conversation.
/// </summary>
public enum ChatMessageRole
{
    /// <summary>User message (tenant employee)</summary>
    User = 0,

    /// <summary>AI assistant message</summary>
    Assistant = 1,

    /// <summary>System message (context, instructions)</summary>
    System = 2,

    /// <summary>Tool execution result</summary>
    Tool = 3
}

/// <summary>
/// Status of a chat conversation.
/// </summary>
public enum ChatConversationStatus
{
    /// <summary>Conversation is active</summary>
    Active = 0,

    /// <summary>Conversation has been archived</summary>
    Archived = 1,

    /// <summary>Conversation was deleted by user</summary>
    Deleted = 2
}

/// <summary>
/// Category of chat tools for organization.
/// </summary>
public enum ChatToolCategory
{
    /// <summary>Shipping and courier related tools</summary>
    Shipping = 0,

    /// <summary>Order management tools</summary>
    Orders = 1,

    /// <summary>Inventory management tools</summary>
    Inventory = 2,

    /// <summary>Finance and reporting tools</summary>
    Finance = 3,

    /// <summary>General assistant capabilities</summary>
    General = 4
}
