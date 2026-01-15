# NDR Workflow & Data Model

## Table of Contents
1. [Overview](#overview)
2. [NDR Lifecycle](#ndr-lifecycle)
3. [Data Model](#data-model)
4. [Workflow States](#workflow-states)
5. [Action Types](#action-types)
6. [Assignment System](#assignment-system)
7. [Analytics & Reporting](#analytics--reporting)
8. [Automation Rules](#automation-rules)

---

## Overview

Non-Delivery Report (NDR) management is a critical feature for eCommerce operations. When a delivery attempt fails, an NDR is generated, and the system helps resolve it through customer follow-ups.

### Key Features
- **Centralized NDR inbox** - All NDRs from all couriers in one place
- **Employee assignment** - Assign NDRs to team members for follow-up
- **Multi-channel follow-up** - Call, WhatsApp, SMS, Email
- **Action tracking** - Complete audit trail of all actions
- **Reattempt scheduling** - Request new delivery attempts
- **Analytics** - Performance tracking by employee, courier, reason

---

## NDR Lifecycle

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              NDR LIFECYCLE                                           │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   ┌────────────┐                                                                    │
│   │  Courier   │──── NDR webhook received                                           │
│   │  Webhook   │                                                                    │
│   └─────┬──────┘                                                                    │
│         │                                                                            │
│         ▼                                                                            │
│   ┌────────────┐                                                                    │
│   │    OPEN    │──── NDR created, awaiting assignment                               │
│   └─────┬──────┘                                                                    │
│         │                                                                            │
│         │ (Auto or manual assignment)                                               │
│         ▼                                                                            │
│   ┌────────────┐                                                                    │
│   │IN_PROGRESS │──── Assigned to employee, follow-up in progress                    │
│   └─────┬──────┘                                                                    │
│         │                                                                            │
│         │ (Call/WhatsApp/SMS/Email actions)                                         │
│         │                                                                            │
│         ├──────────────────────────────────────────────────────────────────┐        │
│         │                                                                  │        │
│         ▼                                                                  ▼        │
│   ┌─────────────────┐                                             ┌──────────────┐ │
│   │    REATTEMPT    │                                             │     RTO      │ │
│   │    SCHEDULED    │                                             │   INITIATED  │ │
│   └────────┬────────┘                                             └──────┬───────┘ │
│            │                                                             │         │
│            │ (Courier picks up)                                          │         │
│            │                                                             │         │
│            ├────────────────────────┬────────────────────┐              │         │
│            ▼                        ▼                    ▼              ▼         │
│   ┌──────────────┐         ┌──────────────┐    ┌──────────────┐  ┌───────────┐   │
│   │  DELIVERED   │         │  NDR AGAIN   │    │  CANCELLED   │  │    RTO    │   │
│   │  (Resolved)  │         │  (Re-open)   │    │  (Resolved)  │  │ DELIVERED │   │
│   └──────────────┘         └──────────────┘    └──────────────┘  └───────────┘   │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Data Model

### NDR Record Entity

```csharp
// Domain/Entities/NDR/NdrRecord.cs
public class NdrRecord : AuditableEntity
{
    public string NdrNumber { get; private set; } = string.Empty;

    // References
    public Guid ShipmentId { get; private set; }
    public Shipment Shipment { get; private set; } = null!;

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;

    // NDR Details
    public NdrReasonCode ReasonCode { get; private set; }
    public string? ReasonDescription { get; private set; }
    public DateTime NdrDate { get; private set; }
    public int AttemptCount { get; private set; } = 1;

    // Status
    public NdrStatus Status { get; private set; } = NdrStatus.Open;
    public NdrResolution? Resolution { get; private set; }

    // Assignment
    public Guid? AssignedToId { get; private set; }
    public User? AssignedTo { get; private set; }
    public DateTime? AssignedAt { get; private set; }

    // Cached Customer Info
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;
    public Address CustomerAddress { get; private set; } = null!;

    // Priority & SLA
    public NdrPriority Priority { get; private set; } = NdrPriority.Medium;
    public DateTime? DueDate { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    // Collections
    private readonly List<NdrAction> _actions = new();
    public IReadOnlyCollection<NdrAction> Actions => _actions.AsReadOnly();

    private readonly List<NdrRemark> _remarks = new();
    public IReadOnlyCollection<NdrRemark> Remarks => _remarks.AsReadOnly();

    public Dictionary<string, object> Metadata { get; private set; } = new();

    // Factory method
    public static NdrRecord Create(
        Shipment shipment,
        NdrReasonCode reasonCode,
        string? reasonDescription,
        DateTime ndrDate)
    {
        var ndr = new NdrRecord
        {
            Id = Guid.NewGuid(),
            NdrNumber = GenerateNdrNumber(),
            ShipmentId = shipment.Id,
            OrderId = shipment.OrderId,
            ReasonCode = reasonCode,
            ReasonDescription = reasonDescription,
            NdrDate = ndrDate,
            AttemptCount = 1,
            Status = NdrStatus.Open,
            CustomerName = shipment.Order.CustomerName,
            CustomerPhone = shipment.Order.CustomerPhone ?? string.Empty,
            CustomerAddress = shipment.DeliveryAddress,
            Priority = DeterminePriority(reasonCode, shipment.Order),
            DueDate = CalculateDueDate(reasonCode)
        };

        ndr.AddDomainEvent(new NdrCreatedEvent(ndr.Id, ndr.ShipmentId));
        return ndr;
    }

    public void AssignTo(Guid userId)
    {
        AssignedToId = userId;
        AssignedAt = DateTime.UtcNow;

        if (Status == NdrStatus.Open)
            Status = NdrStatus.InProgress;

        AddDomainEvent(new NdrAssignedEvent(Id, userId));
    }

    public void AddAction(NdrAction action)
    {
        _actions.Add(action);

        // Update status based on action outcome
        if (action.Outcome == NdrOutcome.WillAccept ||
            action.Outcome == NdrOutcome.Reschedule)
        {
            if (action.ReattemptDate.HasValue)
            {
                Status = NdrStatus.ReattemptScheduled;
            }
        }
        else if (action.Outcome == NdrOutcome.Refuse)
        {
            // May initiate RTO
        }

        AddDomainEvent(new NdrActionAddedEvent(Id, action.Id));
    }

    public void AddRemark(string remark, Guid createdBy, bool isInternal = true)
    {
        _remarks.Add(new NdrRemark(Id, remark, createdBy, isInternal));
    }

    public void ScheduleReattempt(DateTime reattemptDate, Address? newAddress = null, string? newPhone = null)
    {
        Status = NdrStatus.ReattemptScheduled;

        if (newAddress != null)
            CustomerAddress = newAddress;

        if (!string.IsNullOrEmpty(newPhone))
            CustomerPhone = newPhone;

        Metadata["reattempt_date"] = reattemptDate;
        Metadata["reattempt_requested_at"] = DateTime.UtcNow;

        AddDomainEvent(new NdrReattemptScheduledEvent(Id, reattemptDate));
    }

    public void Resolve(NdrResolution resolution)
    {
        Status = NdrStatus.Resolved;
        Resolution = resolution;
        ResolvedAt = DateTime.UtcNow;

        AddDomainEvent(new NdrResolvedEvent(Id, resolution));
    }

    public void InitiateRto()
    {
        Status = NdrStatus.RtoInitiated;
        AddDomainEvent(new NdrRtoInitiatedEvent(Id));
    }

    public void IncrementAttempt(NdrReasonCode newReasonCode, string? newReasonDescription)
    {
        AttemptCount++;
        ReasonCode = newReasonCode;
        ReasonDescription = newReasonDescription;
        NdrDate = DateTime.UtcNow;

        if (Status == NdrStatus.ReattemptScheduled)
        {
            Status = NdrStatus.InProgress;
        }
    }

    private static NdrPriority DeterminePriority(NdrReasonCode reason, Order order)
    {
        // High value orders = high priority
        if (order.TotalAmount.Amount > 5000)
            return NdrPriority.High;

        // COD orders = higher priority (revenue at risk)
        if (order.PaymentMethod == PaymentMethod.Cod)
            return NdrPriority.High;

        // Customer-related issues = medium
        if (reason == NdrReasonCode.CustomerUnavailable ||
            reason == NdrReasonCode.WrongAddress)
            return NdrPriority.Medium;

        // Refused = urgent (need quick resolution)
        if (reason == NdrReasonCode.Refused)
            return NdrPriority.Urgent;

        return NdrPriority.Medium;
    }

    private static DateTime CalculateDueDate(NdrReasonCode reason)
    {
        // Different SLAs based on reason
        var hours = reason switch
        {
            NdrReasonCode.Refused => 24,        // Urgent
            NdrReasonCode.CodNotReady => 48,    // Customer needs time
            _ => 36                              // Default 36 hours
        };

        return DateTime.UtcNow.AddHours(hours);
    }

    private static string GenerateNdrNumber()
    {
        return $"NDR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}
```

### NDR Action Entity

```csharp
// Domain/Entities/NDR/NdrAction.cs
public class NdrAction : BaseEntity
{
    public Guid NdrId { get; private set; }
    public NdrRecord Ndr { get; private set; } = null!;

    public NdrActionType ActionType { get; private set; }

    // Call details
    public CallStatus? CallStatus { get; private set; }
    public int? CallDurationSeconds { get; private set; }
    public string? CallRecordingUrl { get; private set; }

    // Message details
    public string? MessageContent { get; private set; }
    public MessageStatus? MessageStatus { get; private set; }
    public string? MessageId { get; private set; }

    // Outcome
    public NdrOutcome? Outcome { get; private set; }
    public string? OutcomeNotes { get; private set; }

    // Reattempt details
    public DateTime? ReattemptDate { get; private set; }
    public Address? NewAddress { get; private set; }
    public string? NewPhone { get; private set; }

    public Guid PerformedById { get; private set; }
    public User PerformedBy { get; private set; } = null!;
    public DateTime PerformedAt { get; private set; }

    public Dictionary<string, object> Metadata { get; private set; } = new();

    private NdrAction() { }

    public static NdrAction CreateCallAction(
        Guid ndrId,
        Guid performedBy,
        CallStatus callStatus,
        int? durationSeconds,
        NdrOutcome? outcome,
        string? notes)
    {
        return new NdrAction
        {
            Id = Guid.NewGuid(),
            NdrId = ndrId,
            ActionType = NdrActionType.Call,
            CallStatus = callStatus,
            CallDurationSeconds = durationSeconds,
            Outcome = outcome,
            OutcomeNotes = notes,
            PerformedById = performedBy,
            PerformedAt = DateTime.UtcNow
        };
    }

    public static NdrAction CreateWhatsAppAction(
        Guid ndrId,
        Guid performedBy,
        string messageContent,
        NdrOutcome? outcome,
        string? notes)
    {
        return new NdrAction
        {
            Id = Guid.NewGuid(),
            NdrId = ndrId,
            ActionType = NdrActionType.WhatsApp,
            MessageContent = messageContent,
            MessageStatus = MessageStatus.Sent,
            Outcome = outcome,
            OutcomeNotes = notes,
            PerformedById = performedBy,
            PerformedAt = DateTime.UtcNow
        };
    }

    public static NdrAction CreateSmsAction(
        Guid ndrId,
        Guid performedBy,
        string messageContent)
    {
        return new NdrAction
        {
            Id = Guid.NewGuid(),
            NdrId = ndrId,
            ActionType = NdrActionType.Sms,
            MessageContent = messageContent,
            MessageStatus = MessageStatus.Sent,
            PerformedById = performedBy,
            PerformedAt = DateTime.UtcNow
        };
    }

    public static NdrAction CreateReattemptRequest(
        Guid ndrId,
        Guid performedBy,
        DateTime reattemptDate,
        Address? newAddress,
        string? newPhone,
        string? notes)
    {
        return new NdrAction
        {
            Id = Guid.NewGuid(),
            NdrId = ndrId,
            ActionType = NdrActionType.ReattemptRequest,
            ReattemptDate = reattemptDate,
            NewAddress = newAddress,
            NewPhone = newPhone,
            Outcome = NdrOutcome.Reschedule,
            OutcomeNotes = notes,
            PerformedById = performedBy,
            PerformedAt = DateTime.UtcNow
        };
    }
}
```

### Enums

```csharp
// Domain/Enums/NdrEnums.cs

public enum NdrStatus
{
    Open,
    InProgress,
    ReattemptScheduled,
    Resolved,
    RtoInitiated
}

public enum NdrResolution
{
    Delivered,
    Rto,
    Cancelled
}

public enum NdrPriority
{
    Low,
    Medium,
    High,
    Urgent
}

public enum NdrReasonCode
{
    CustomerUnavailable,
    WrongAddress,
    IncompleteAddress,
    Refused,
    CodNotReady,
    OutOfDeliveryArea,
    CustomerRequestedDelay,
    DoorLocked,
    SecurityRestriction,
    WeatherIssue,
    OperationalIssue,
    Other
}

public enum NdrActionType
{
    Call,
    WhatsApp,
    Sms,
    Email,
    ReattemptRequest
}

public enum NdrOutcome
{
    WillAccept,
    WrongAddress,
    Reschedule,
    Refuse,
    NotReachable,
    NoResponse,
    Other
}

public enum CallStatus
{
    Connected,
    NotAnswered,
    Busy,
    SwitchedOff,
    InvalidNumber,
    Disconnected
}

public enum MessageStatus
{
    Pending,
    Sent,
    Delivered,
    Read,
    Failed
}
```

---

## Workflow States

### State Transitions

| Current State | Allowed Transitions | Trigger |
|---------------|---------------------|---------|
| Open | InProgress | Assignment |
| Open | RtoInitiated | Auto RTO after X attempts |
| InProgress | ReattemptScheduled | Reattempt requested |
| InProgress | Resolved | Delivered confirmation |
| InProgress | RtoInitiated | Customer refuses / Max attempts |
| ReattemptScheduled | InProgress | New NDR received |
| ReattemptScheduled | Resolved | Delivered on reattempt |
| RtoInitiated | Resolved | RTO completed |

### State Machine Implementation

```csharp
// Domain/Entities/NDR/NdrStateMachine.cs
public class NdrStateMachine
{
    private static readonly Dictionary<NdrStatus, NdrStatus[]> _allowedTransitions = new()
    {
        [NdrStatus.Open] = new[] { NdrStatus.InProgress, NdrStatus.RtoInitiated },
        [NdrStatus.InProgress] = new[] { NdrStatus.ReattemptScheduled, NdrStatus.Resolved, NdrStatus.RtoInitiated },
        [NdrStatus.ReattemptScheduled] = new[] { NdrStatus.InProgress, NdrStatus.Resolved, NdrStatus.RtoInitiated },
        [NdrStatus.RtoInitiated] = new[] { NdrStatus.Resolved },
        [NdrStatus.Resolved] = Array.Empty<NdrStatus>()
    };

    public static bool CanTransition(NdrStatus current, NdrStatus target)
    {
        if (current == target) return true;

        return _allowedTransitions.TryGetValue(current, out var allowed)
            && allowed.Contains(target);
    }

    public static void ValidateTransition(NdrStatus current, NdrStatus target)
    {
        if (!CanTransition(current, target))
        {
            throw new BusinessRuleViolationException(
                $"Cannot transition NDR from {current} to {target}");
        }
    }
}
```

---

## Action Types

### Call Action Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                        CALL ACTION FLOW                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   1. Agent initiates call (click-to-call or manual dial)        │
│                                                                  │
│   2. Record call attempt:                                        │
│      • Call status (connected/busy/not_answered/etc.)            │
│      • Duration (if connected)                                   │
│                                                                  │
│   3. If connected, record outcome:                               │
│      • Will accept delivery                                      │
│      • Wants to reschedule                                       │
│      • Refuses delivery                                          │
│      • Address correction needed                                 │
│                                                                  │
│   4. Record notes from conversation                              │
│                                                                  │
│   5. If reschedule: prompt for new date & address changes        │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### WhatsApp Action Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                     WHATSAPP ACTION FLOW                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   1. Select template or compose message:                         │
│      • Standard NDR notification                                 │
│      • Address confirmation request                              │
│      • Reattempt confirmation                                    │
│                                                                  │
│   2. Message variables auto-filled:                              │
│      • Customer name                                             │
│      • Order number                                              │
│      • AWB number                                                │
│      • NDR reason                                                │
│                                                                  │
│   3. Send via WhatsApp Business API (Twilio/Gupshup)            │
│                                                                  │
│   4. Track delivery status via webhook                           │
│                                                                  │
│   5. Record customer response if interactive                     │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Action Command Handler

```csharp
// Application/Features/NDR/Commands/AddNdrAction/AddNdrActionCommandHandler.cs
public class AddNdrActionCommandHandler : IRequestHandler<AddNdrActionCommand, Result<NdrActionDto>>
{
    private readonly ITenantDbContext _context;
    private readonly ICurrentUserService _userService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ISmsService _smsService;
    private readonly IMapper _mapper;

    public async Task<Result<NdrActionDto>> Handle(
        AddNdrActionCommand request,
        CancellationToken ct)
    {
        var ndr = await _context.NdrRecords
            .Include(n => n.Actions)
            .FirstOrDefaultAsync(n => n.Id == request.NdrId, ct);

        if (ndr == null)
            return Result<NdrActionDto>.Failure("NOT_FOUND", "NDR not found");

        NdrAction action;

        switch (request.ActionType)
        {
            case NdrActionType.Call:
                action = NdrAction.CreateCallAction(
                    ndr.Id,
                    _userService.UserId,
                    request.CallStatus!.Value,
                    request.CallDurationSeconds,
                    request.Outcome,
                    request.Notes);
                break;

            case NdrActionType.WhatsApp:
                // Send WhatsApp message
                var whatsAppResult = await _whatsAppService.SendMessageAsync(
                    ndr.CustomerPhone,
                    request.MessageContent!);

                if (!whatsAppResult.IsSuccess)
                    return Result<NdrActionDto>.Failure(whatsAppResult.Error!);

                action = NdrAction.CreateWhatsAppAction(
                    ndr.Id,
                    _userService.UserId,
                    request.MessageContent!,
                    request.Outcome,
                    request.Notes);

                action.Metadata["message_sid"] = whatsAppResult.Value!.MessageSid;
                break;

            case NdrActionType.Sms:
                // Send SMS
                var smsResult = await _smsService.SendAsync(
                    ndr.CustomerPhone,
                    request.MessageContent!);

                if (!smsResult.IsSuccess)
                    return Result<NdrActionDto>.Failure(smsResult.Error!);

                action = NdrAction.CreateSmsAction(
                    ndr.Id,
                    _userService.UserId,
                    request.MessageContent!);
                break;

            case NdrActionType.ReattemptRequest:
                action = NdrAction.CreateReattemptRequest(
                    ndr.Id,
                    _userService.UserId,
                    request.ReattemptDate!.Value,
                    request.NewAddress,
                    request.NewPhone,
                    request.Notes);

                // Schedule reattempt with courier
                await ScheduleCourierReattemptAsync(ndr, request);
                break;

            default:
                return Result<NdrActionDto>.Failure("INVALID_ACTION", "Invalid action type");
        }

        ndr.AddAction(action);
        await _context.SaveChangesAsync(ct);

        return Result<NdrActionDto>.Success(_mapper.Map<NdrActionDto>(action));
    }

    private async Task ScheduleCourierReattemptAsync(NdrRecord ndr, AddNdrActionCommand request)
    {
        // Get shipment and courier adapter
        var shipment = await _context.Shipments
            .Include(s => s.CourierConfig)
            .FirstAsync(s => s.Id == ndr.ShipmentId);

        // Call courier API to schedule reattempt
        // This would be implemented in the courier adapter
    }
}
```

---

## Assignment System

### Auto-Assignment Rules

```csharp
// Infrastructure/BackgroundJobs/Jobs/NdrAutoAssignmentJob.cs
public class NdrAutoAssignmentJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NdrAutoAssignmentJob> _logger;

    [Queue("ndr")]
    [AutomaticRetry(Attempts = 2)]
    public async Task ProcessUnassignedNdrsAsync(Guid tenantId)
    {
        using var scope = _scopeFactory.CreateScope();

        await SetTenantContextAsync(scope, tenantId);

        var context = scope.ServiceProvider.GetRequiredService<ITenantDbContext>();

        // Get unassigned NDRs
        var unassignedNdrs = await context.NdrRecords
            .Where(n => n.Status == NdrStatus.Open && n.AssignedToId == null)
            .OrderBy(n => n.Priority)
            .ThenBy(n => n.DueDate)
            .ToListAsync();

        if (!unassignedNdrs.Any())
            return;

        // Get available agents with their current load
        var agents = await GetAvailableAgentsAsync(context);

        foreach (var ndr in unassignedNdrs)
        {
            // Find best agent using round-robin with load balancing
            var bestAgent = agents
                .OrderBy(a => a.CurrentNdrCount)
                .ThenBy(a => a.LastAssignedAt)
                .FirstOrDefault(a => a.CurrentNdrCount < a.MaxNdrCapacity);

            if (bestAgent == null)
            {
                _logger.LogWarning("No available agents for NDR assignment");
                break;
            }

            ndr.AssignTo(bestAgent.UserId);
            bestAgent.CurrentNdrCount++;
            bestAgent.LastAssignedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    private async Task<List<AgentLoad>> GetAvailableAgentsAsync(ITenantDbContext context)
    {
        // Get users with NDR permission who are active
        var agents = await context.Users
            .Where(u => u.IsActive)
            .Where(u => u.UserRoles.Any(ur =>
                ur.Role.RolePermissions.Any(rp =>
                    rp.Permission.Code == "ndr.action")))
            .Select(u => new AgentLoad
            {
                UserId = u.Id,
                UserName = u.Name,
                CurrentNdrCount = context.NdrRecords
                    .Count(n => n.AssignedToId == u.Id &&
                           n.Status != NdrStatus.Resolved),
                MaxNdrCapacity = 50, // Configurable per user
                LastAssignedAt = context.NdrRecords
                    .Where(n => n.AssignedToId == u.Id)
                    .Max(n => (DateTime?)n.AssignedAt)
            })
            .ToListAsync();

        return agents;
    }
}
```

### Manual Assignment UI Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                     NDR ASSIGNMENT UI                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. Select NDR(s) from inbox                                     │
│     □ NDR-20240115-ABC123   High   Customer Unavailable         │
│     ☑ NDR-20240115-DEF456   Med    Wrong Address                │
│     ☑ NDR-20240115-GHI789   Low    COD Not Ready                │
│                                                                  │
│  2. Click "Assign" button                                        │
│                                                                  │
│  3. Select team member from dropdown:                            │
│     ┌──────────────────────────────────────────────┐            │
│     │ Select Team Member                     ▼     │            │
│     ├──────────────────────────────────────────────┤            │
│     │ 👤 John Doe      (12 active NDRs)            │            │
│     │ 👤 Jane Smith    (8 active NDRs)             │            │
│     │ 👤 Bob Wilson    (15 active NDRs)            │            │
│     └──────────────────────────────────────────────┘            │
│                                                                  │
│  4. Confirm assignment                                           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Analytics & Reporting

### NDR Analytics Query

```csharp
// Application/Features/NDR/Queries/GetNdrAnalytics/GetNdrAnalyticsQueryHandler.cs
public class GetNdrAnalyticsQueryHandler
    : IRequestHandler<GetNdrAnalyticsQuery, NdrAnalyticsDto>
{
    private readonly ITenantDbContext _context;

    public async Task<NdrAnalyticsDto> Handle(
        GetNdrAnalyticsQuery request,
        CancellationToken ct)
    {
        var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        var ndrsQuery = _context.NdrRecords
            .Where(n => n.CreatedAt >= startDate && n.CreatedAt <= endDate);

        // Overall stats
        var totalNdrs = await ndrsQuery.CountAsync(ct);
        var resolvedNdrs = await ndrsQuery.CountAsync(n => n.Status == NdrStatus.Resolved, ct);
        var deliveredNdrs = await ndrsQuery.CountAsync(n => n.Resolution == NdrResolution.Delivered, ct);
        var rtoNdrs = await ndrsQuery.CountAsync(n => n.Resolution == NdrResolution.Rto, ct);

        // By reason
        var byReason = await ndrsQuery
            .GroupBy(n => n.ReasonCode)
            .Select(g => new ReasonBreakdown
            {
                ReasonCode = g.Key,
                Count = g.Count(),
                DeliveredCount = g.Count(n => n.Resolution == NdrResolution.Delivered),
                RtoCount = g.Count(n => n.Resolution == NdrResolution.Rto)
            })
            .ToListAsync(ct);

        // By employee performance
        var byEmployee = await ndrsQuery
            .Where(n => n.AssignedToId != null)
            .GroupBy(n => new { n.AssignedToId, n.AssignedTo!.Name })
            .Select(g => new EmployeePerformance
            {
                UserId = g.Key.AssignedToId!.Value,
                UserName = g.Key.Name,
                TotalAssigned = g.Count(),
                Resolved = g.Count(n => n.Status == NdrStatus.Resolved),
                Delivered = g.Count(n => n.Resolution == NdrResolution.Delivered),
                AverageResolutionHours = g
                    .Where(n => n.ResolvedAt != null)
                    .Average(n => (double?)EF.Functions
                        .DateDiffHour(n.CreatedAt, n.ResolvedAt!.Value)) ?? 0,
                TotalActions = g.Sum(n => n.Actions.Count)
            })
            .ToListAsync(ct);

        // Trend data
        var trend = await ndrsQuery
            .GroupBy(n => n.CreatedAt.Date)
            .Select(g => new DailyTrend
            {
                Date = g.Key,
                Total = g.Count(),
                Resolved = g.Count(n => n.Status == NdrStatus.Resolved),
                Delivered = g.Count(n => n.Resolution == NdrResolution.Delivered)
            })
            .OrderBy(t => t.Date)
            .ToListAsync(ct);

        return new NdrAnalyticsDto
        {
            Period = new DateRange(startDate, endDate),
            Summary = new NdrSummary
            {
                TotalNdrs = totalNdrs,
                ResolvedNdrs = resolvedNdrs,
                DeliveredNdrs = deliveredNdrs,
                RtoNdrs = rtoNdrs,
                ResolutionRate = totalNdrs > 0 ? (decimal)resolvedNdrs / totalNdrs * 100 : 0,
                DeliverySuccessRate = resolvedNdrs > 0 ? (decimal)deliveredNdrs / resolvedNdrs * 100 : 0
            },
            ByReason = byReason,
            ByEmployee = byEmployee,
            Trend = trend
        };
    }
}
```

### Analytics Dashboard Cards

```tsx
// components/features/ndr/ndr-analytics-dashboard.tsx
export function NdrAnalyticsDashboard({ analytics }: { analytics: NdrAnalytics }) {
  return (
    <div className="space-y-6">
      {/* Summary cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatsCard
          title="Total NDRs"
          value={analytics.summary.totalNdrs}
          icon={AlertCircle}
        />
        <StatsCard
          title="Resolved"
          value={analytics.summary.resolvedNdrs}
          description={`${analytics.summary.resolutionRate.toFixed(1)}% resolution rate`}
          icon={CheckCircle}
          trend="up"
        />
        <StatsCard
          title="Delivered"
          value={analytics.summary.deliveredNdrs}
          description={`${analytics.summary.deliverySuccessRate.toFixed(1)}% success`}
          icon={Package}
          trend="up"
        />
        <StatsCard
          title="RTO"
          value={analytics.summary.rtoNdrs}
          icon={RotateCcw}
          variant="warning"
        />
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>NDR by Reason</CardTitle>
          </CardHeader>
          <CardContent>
            <PieChart data={analytics.byReason} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Daily Trend</CardTitle>
          </CardHeader>
          <CardContent>
            <LineChart data={analytics.trend} />
          </CardContent>
        </Card>
      </div>

      {/* Employee performance table */}
      <Card>
        <CardHeader>
          <CardTitle>Employee Performance</CardTitle>
        </CardHeader>
        <CardContent>
          <EmployeePerformanceTable data={analytics.byEmployee} />
        </CardContent>
      </Card>
    </div>
  );
}
```

---

## Automation Rules

### Configurable Automation

```csharp
// Domain/Entities/NDR/NdrAutomationRule.cs
public class NdrAutomationRule : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Trigger conditions
    public NdrTriggerType TriggerType { get; set; }
    public NdrReasonCode? ReasonCode { get; set; }
    public NdrPriority? Priority { get; set; }
    public int? AttemptCountThreshold { get; set; }
    public int? HoursAfterCreation { get; set; }

    // Actions to perform
    public NdrAutomationAction Action { get; set; }
    public string? ActionConfig { get; set; } // JSON config

    public int ExecutionOrder { get; set; }
}

public enum NdrTriggerType
{
    OnCreate,               // When NDR is created
    OnStatusChange,         // When status changes
    OnAttemptThreshold,     // After X attempts
    OnTimeElapsed,          // After X hours without resolution
    OnNoAction              // No action taken in X hours
}

public enum NdrAutomationAction
{
    SendSms,
    SendWhatsApp,
    AssignToUser,
    ChangePriority,
    InitiateRto,
    SendEmail,
    CreateTask
}
```

### Automation Engine

```csharp
// Infrastructure/BackgroundJobs/Jobs/NdrAutomationJob.cs
public class NdrAutomationJob
{
    public async Task ProcessAutomationRulesAsync(Guid tenantId, Guid ndrId, NdrTriggerType trigger)
    {
        using var scope = _scopeFactory.CreateScope();
        await SetTenantContextAsync(scope, tenantId);

        var context = scope.ServiceProvider.GetRequiredService<ITenantDbContext>();

        var ndr = await context.NdrRecords
            .Include(n => n.Actions)
            .FirstOrDefaultAsync(n => n.Id == ndrId);

        if (ndr == null) return;

        // Get applicable rules
        var rules = await context.Set<NdrAutomationRule>()
            .Where(r => r.IsActive)
            .Where(r => r.TriggerType == trigger)
            .Where(r => r.ReasonCode == null || r.ReasonCode == ndr.ReasonCode)
            .Where(r => r.Priority == null || r.Priority == ndr.Priority)
            .OrderBy(r => r.ExecutionOrder)
            .ToListAsync();

        foreach (var rule in rules)
        {
            if (ShouldExecuteRule(ndr, rule))
            {
                await ExecuteRuleAsync(ndr, rule, scope.ServiceProvider);
            }
        }
    }

    private bool ShouldExecuteRule(NdrRecord ndr, NdrAutomationRule rule)
    {
        // Check attempt threshold
        if (rule.AttemptCountThreshold.HasValue &&
            ndr.AttemptCount < rule.AttemptCountThreshold.Value)
            return false;

        // Check time elapsed
        if (rule.HoursAfterCreation.HasValue)
        {
            var hoursElapsed = (DateTime.UtcNow - ndr.CreatedAt).TotalHours;
            if (hoursElapsed < rule.HoursAfterCreation.Value)
                return false;
        }

        return true;
    }

    private async Task ExecuteRuleAsync(
        NdrRecord ndr,
        NdrAutomationRule rule,
        IServiceProvider services)
    {
        var config = rule.ActionConfig != null
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(rule.ActionConfig)
            : new Dictionary<string, object>();

        switch (rule.Action)
        {
            case NdrAutomationAction.SendSms:
                var smsService = services.GetRequiredService<ISmsService>();
                var smsTemplate = config.GetValueOrDefault("template", "ndr_reminder") as string;
                await smsService.SendTemplateAsync(ndr.CustomerPhone, smsTemplate!, new
                {
                    customer_name = ndr.CustomerName,
                    order_number = ndr.Order.OrderNumber
                });
                break;

            case NdrAutomationAction.SendWhatsApp:
                var whatsAppService = services.GetRequiredService<IWhatsAppService>();
                var waTemplate = config.GetValueOrDefault("template", "ndr_notification") as string;
                await whatsAppService.SendTemplateAsync(ndr.CustomerPhone, waTemplate!, new
                {
                    customer_name = ndr.CustomerName,
                    order_number = ndr.Order.OrderNumber,
                    ndr_reason = ndr.ReasonDescription
                });
                break;

            case NdrAutomationAction.InitiateRto:
                ndr.InitiateRto();
                break;

            case NdrAutomationAction.ChangePriority:
                var newPriority = Enum.Parse<NdrPriority>(config["priority"]!.ToString()!);
                ndr.UpdatePriority(newPriority);
                break;
        }
    }
}
```

---

## Next Steps

See the following documents for more details:
- [API Design](08-api-design.md)
- [Mobile Readiness](09-mobile-readiness.md)
- [Development Roadmap](10-development-roadmap.md)
