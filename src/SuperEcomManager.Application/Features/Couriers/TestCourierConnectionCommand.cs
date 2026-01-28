using MediatR;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Attributes;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Couriers;

/// <summary>
/// Command to test courier account connection by calling the courier API.
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
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}

public class TestCourierConnectionCommandHandler : IRequestHandler<TestCourierConnectionCommand, Result<CourierConnectionTestResult>>
{
    private readonly ICourierService _courierService;
    private readonly ILogger<TestCourierConnectionCommandHandler> _logger;

    public TestCourierConnectionCommandHandler(
        ICourierService courierService,
        ILogger<TestCourierConnectionCommandHandler> logger)
    {
        _courierService = courierService;
        _logger = logger;
    }

    public async Task<Result<CourierConnectionTestResult>> Handle(
        TestCourierConnectionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var connectionResult = await _courierService.TestConnectionAsync(
                request.AccountId, cancellationToken);

            var result = new CourierConnectionTestResult
            {
                IsConnected = connectionResult.IsConnected,
                Message = connectionResult.Message,
                AccountName = connectionResult.AccountName,
                TestedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Connection test for account {AccountId}: {IsConnected} - {Message}",
                request.AccountId, result.IsConnected, result.Message);

            return Result<CourierConnectionTestResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for account {AccountId}", request.AccountId);
            return Result<CourierConnectionTestResult>.Failure($"Connection test failed: {ex.Message}");
        }
    }
}
