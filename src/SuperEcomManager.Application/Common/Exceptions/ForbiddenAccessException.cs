namespace SuperEcomManager.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when access is forbidden.
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("Access to this resource is forbidden.")
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }

    public ForbiddenAccessException(string resource, string action)
        : base($"Access to {resource} for action '{action}' is forbidden.")
    {
    }
}
