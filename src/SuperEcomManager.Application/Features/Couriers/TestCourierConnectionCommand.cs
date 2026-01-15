using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Command to test courier account connection.
/// </summary>
[RequirePermission("couriers.view")]
[RequireFeature("courier_management")]
public record TestCourierConnectionCommand : IRequest<Result<CourierConnectionTestResult>>, ITenantRequest
{
    public Guid AccountId { get; init; }
}

public class CourierConnectionTestResult
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public string? AccountName { get; set; }
    public Dictionary<string, string>? AccountInfo { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}

public class TestCourierConnectionCommandHandler : IRequestHandler<TestCourierConnectionCommand, Result<CourierConnectionTestResult>>
{
    private readonly ITenantDbContext _dbContext;
    private readonly ILogger<TestCourierConnectionCommandHandler> _logger;

    // Delegate for testing connection (injected from Integrations layer)
    public Func<Guid, CancellationToken, Task<CourierConnectionTestResult>>? TestConnection { get; set; }

    public TestCourierConnectionCommandHandler(
        ITenantDbContext dbContext,
        ILogger<TestCourierConnectionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<CourierConnectionTestResult>> Handle(
        TestCourierConnectionCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _dbContext.CourierAccounts
            .FirstOrDefaultAsync(c => c.Id == request.AccountId, cancellationToken);

        if (account == null)
        {
            return Result<CourierConnectionTestResult>.Failure("Courier account not found");
        }

        if (string.IsNullOrEmpty(account.ApiKey) && string.IsNullOrEmpty(account.AccessToken))
        {
            return Result<CourierConnectionTestResult>.Success(new CourierConnectionTestResult
            {
                IsConnected = false,
                Message = "No credentials configured"
            });
        }

        try
        {
            CourierConnectionTestResult result;

            if (TestConnection != null)
            {
                result = await TestConnection(account.Id, cancellationToken);
            }
            else
            {
                // Simulate connection test when adapter is not available
                result = new CourierConnectionTestResult
                {
                    IsConnected = true,
                    Message = "Connection test simulated (adapter not configured)",
                    AccountName = account.Name
                };
            }

            // Update account connection status
            if (result.IsConnected)
            {
                account.MarkConnected();
            }
            else
            {
                account.MarkDisconnected(result.Message);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Connection test for account {AccountId}: {IsConnected}",
                account.Id, result.IsConnected);

            return Result<CourierConnectionTestResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for account {AccountId}", account.Id);

            account.MarkDisconnected(ex.Message);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result<CourierConnectionTestResult>.Failure($"Connection test failed: {ex.Message}");
        }
    }
}
