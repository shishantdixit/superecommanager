using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Platform;

namespace SuperEcomManager.Application.Features.PlatformAdmin;

/// <summary>
/// Command to create a new platform admin.
/// </summary>
public class CreatePlatformAdminCommand : IRequest<Result<PlatformAdminDto>>
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsSuperAdmin { get; set; }
}

public class CreatePlatformAdminCommandHandler : IRequestHandler<CreatePlatformAdminCommand, Result<PlatformAdminDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreatePlatformAdminCommandHandler> _logger;

    public CreatePlatformAdminCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService,
        ILogger<CreatePlatformAdminCommandHandler> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<PlatformAdminDto>> Handle(CreatePlatformAdminCommand request, CancellationToken cancellationToken)
    {
        // Validate email uniqueness
        var emailExists = await _dbContext.PlatformAdmins
            .AnyAsync(a => a.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (emailExists)
        {
            return Result<PlatformAdminDto>.Failure("Email already exists");
        }

        // Validate password strength
        if (request.Password.Length < 8)
        {
            return Result<PlatformAdminDto>.Failure("Password must be at least 8 characters");
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var admin = Domain.Entities.Platform.PlatformAdmin.Create(
            request.Email,
            request.FirstName,
            request.LastName,
            passwordHash,
            request.IsSuperAdmin);

        _dbContext.PlatformAdmins.Add(admin);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created platform admin {Email} by {Creator}",
            admin.Email, _currentUserService.UserId);

        return Result<PlatformAdminDto>.Success(new PlatformAdminDto
        {
            Id = admin.Id,
            Email = admin.Email,
            FirstName = admin.FirstName,
            LastName = admin.LastName,
            IsSuperAdmin = admin.IsSuperAdmin,
            IsActive = admin.IsActive,
            CreatedAt = admin.CreatedAt
        });
    }
}

/// <summary>
/// Command to update a platform admin.
/// </summary>
public class UpdatePlatformAdminCommand : IRequest<Result<PlatformAdminDto>>
{
    public Guid AdminId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class UpdatePlatformAdminCommandHandler : IRequestHandler<UpdatePlatformAdminCommand, Result<PlatformAdminDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<UpdatePlatformAdminCommandHandler> _logger;

    public UpdatePlatformAdminCommandHandler(
        IApplicationDbContext dbContext,
        ILogger<UpdatePlatformAdminCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<PlatformAdminDto>> Handle(UpdatePlatformAdminCommand request, CancellationToken cancellationToken)
    {
        var admin = await _dbContext.PlatformAdmins
            .FirstOrDefaultAsync(a => a.Id == request.AdminId && a.DeletedAt == null, cancellationToken);

        if (admin == null)
        {
            return Result<PlatformAdminDto>.Failure("Admin not found");
        }

        admin.UpdateProfile(request.FirstName, request.LastName);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated platform admin {Email}", admin.Email);

        return Result<PlatformAdminDto>.Success(new PlatformAdminDto
        {
            Id = admin.Id,
            Email = admin.Email,
            FirstName = admin.FirstName,
            LastName = admin.LastName,
            IsSuperAdmin = admin.IsSuperAdmin,
            IsActive = admin.IsActive,
            LastLoginAt = admin.LastLoginAt,
            CreatedAt = admin.CreatedAt
        });
    }
}

/// <summary>
/// Command to activate a platform admin.
/// </summary>
public class ActivatePlatformAdminCommand : IRequest<Result>
{
    public Guid AdminId { get; set; }
}

public class ActivatePlatformAdminCommandHandler : IRequestHandler<ActivatePlatformAdminCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<ActivatePlatformAdminCommandHandler> _logger;

    public ActivatePlatformAdminCommandHandler(
        IApplicationDbContext dbContext,
        ILogger<ActivatePlatformAdminCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(ActivatePlatformAdminCommand request, CancellationToken cancellationToken)
    {
        var admin = await _dbContext.PlatformAdmins
            .FirstOrDefaultAsync(a => a.Id == request.AdminId && a.DeletedAt == null, cancellationToken);

        if (admin == null)
        {
            return Result.Failure("Admin not found");
        }

        if (admin.IsActive)
        {
            return Result.Failure("Admin is already active");
        }

        admin.Activate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated platform admin {Email}", admin.Email);

        return Result.Success();
    }
}

/// <summary>
/// Command to deactivate a platform admin.
/// </summary>
public class DeactivatePlatformAdminCommand : IRequest<Result>
{
    public Guid AdminId { get; set; }
}

public class DeactivatePlatformAdminCommandHandler : IRequestHandler<DeactivatePlatformAdminCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeactivatePlatformAdminCommandHandler> _logger;

    public DeactivatePlatformAdminCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<DeactivatePlatformAdminCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(DeactivatePlatformAdminCommand request, CancellationToken cancellationToken)
    {
        var admin = await _dbContext.PlatformAdmins
            .FirstOrDefaultAsync(a => a.Id == request.AdminId && a.DeletedAt == null, cancellationToken);

        if (admin == null)
        {
            return Result.Failure("Admin not found");
        }

        // Prevent self-deactivation
        if (admin.Id == _currentUserService.UserId)
        {
            return Result.Failure("Cannot deactivate yourself");
        }

        if (!admin.IsActive)
        {
            return Result.Failure("Admin is already inactive");
        }

        admin.Deactivate();
        admin.RevokeRefreshToken();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated platform admin {Email}", admin.Email);

        return Result.Success();
    }
}

/// <summary>
/// Command to delete (soft) a platform admin.
/// </summary>
public class DeletePlatformAdminCommand : IRequest<Result>
{
    public Guid AdminId { get; set; }
}

public class DeletePlatformAdminCommandHandler : IRequestHandler<DeletePlatformAdminCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeletePlatformAdminCommandHandler> _logger;

    public DeletePlatformAdminCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<DeletePlatformAdminCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(DeletePlatformAdminCommand request, CancellationToken cancellationToken)
    {
        var admin = await _dbContext.PlatformAdmins
            .FirstOrDefaultAsync(a => a.Id == request.AdminId && a.DeletedAt == null, cancellationToken);

        if (admin == null)
        {
            return Result.Failure("Admin not found");
        }

        // Prevent self-deletion
        if (admin.Id == _currentUserService.UserId)
        {
            return Result.Failure("Cannot delete yourself");
        }

        admin.DeletedAt = DateTime.UtcNow;
        admin.DeletedBy = _currentUserService.UserId;
        admin.RevokeRefreshToken();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted platform admin {Email}", admin.Email);

        return Result.Success();
    }
}

/// <summary>
/// Command to promote a platform admin to super admin.
/// </summary>
public class PromoteToSuperAdminCommand : IRequest<Result>
{
    public Guid AdminId { get; set; }
}

public class PromoteToSuperAdminCommandHandler : IRequestHandler<PromoteToSuperAdminCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<PromoteToSuperAdminCommandHandler> _logger;

    public PromoteToSuperAdminCommandHandler(
        IApplicationDbContext dbContext,
        ILogger<PromoteToSuperAdminCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(PromoteToSuperAdminCommand request, CancellationToken cancellationToken)
    {
        var admin = await _dbContext.PlatformAdmins
            .FirstOrDefaultAsync(a => a.Id == request.AdminId && a.DeletedAt == null, cancellationToken);

        if (admin == null)
        {
            return Result.Failure("Admin not found");
        }

        if (admin.IsSuperAdmin)
        {
            return Result.Failure("Admin is already a super admin");
        }

        admin.PromoteToSuperAdmin();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Promoted platform admin {Email} to super admin", admin.Email);

        return Result.Success();
    }
}

/// <summary>
/// Command to demote a super admin.
/// </summary>
public class DemoteFromSuperAdminCommand : IRequest<Result>
{
    public Guid AdminId { get; set; }
}

public class DemoteFromSuperAdminCommandHandler : IRequestHandler<DemoteFromSuperAdminCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DemoteFromSuperAdminCommandHandler> _logger;

    public DemoteFromSuperAdminCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<DemoteFromSuperAdminCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(DemoteFromSuperAdminCommand request, CancellationToken cancellationToken)
    {
        var admin = await _dbContext.PlatformAdmins
            .FirstOrDefaultAsync(a => a.Id == request.AdminId && a.DeletedAt == null, cancellationToken);

        if (admin == null)
        {
            return Result.Failure("Admin not found");
        }

        // Prevent self-demotion
        if (admin.Id == _currentUserService.UserId)
        {
            return Result.Failure("Cannot demote yourself");
        }

        if (!admin.IsSuperAdmin)
        {
            return Result.Failure("Admin is not a super admin");
        }

        // Ensure at least one super admin remains
        var superAdminCount = await _dbContext.PlatformAdmins
            .CountAsync(a => a.IsSuperAdmin && a.DeletedAt == null && a.IsActive, cancellationToken);

        if (superAdminCount <= 1)
        {
            return Result.Failure("Cannot demote the last super admin");
        }

        admin.DemoteFromSuperAdmin();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Demoted platform admin {Email} from super admin", admin.Email);

        return Result.Success();
    }
}

/// <summary>
/// Query to get list of platform admins.
/// </summary>
public class GetPlatformAdminsQuery : IRequest<PaginatedResult<PlatformAdminDto>>
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsSuperAdmin { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetPlatformAdminsQueryHandler : IRequestHandler<GetPlatformAdminsQuery, PaginatedResult<PlatformAdminDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPlatformAdminsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResult<PlatformAdminDto>> Handle(GetPlatformAdminsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.PlatformAdmins
            .AsNoTracking()
            .Where(a => a.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLowerInvariant();
            query = query.Where(a =>
                a.Email.ToLower().Contains(search) ||
                a.FirstName.ToLower().Contains(search) ||
                a.LastName.ToLower().Contains(search));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(a => a.IsActive == request.IsActive.Value);
        }

        if (request.IsSuperAdmin.HasValue)
        {
            query = query.Where(a => a.IsSuperAdmin == request.IsSuperAdmin.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var admins = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new PlatformAdminDto
            {
                Id = a.Id,
                Email = a.Email,
                FirstName = a.FirstName,
                LastName = a.LastName,
                IsSuperAdmin = a.IsSuperAdmin,
                IsActive = a.IsActive,
                LastLoginAt = a.LastLoginAt,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResult<PlatformAdminDto>(
            admins,
            totalCount,
            request.Page,
            request.PageSize);
    }
}

/// <summary>
/// Query to get a platform admin by ID.
/// </summary>
public class GetPlatformAdminByIdQuery : IRequest<Result<PlatformAdminDto>>
{
    public Guid AdminId { get; set; }
}

public class GetPlatformAdminByIdQueryHandler : IRequestHandler<GetPlatformAdminByIdQuery, Result<PlatformAdminDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPlatformAdminByIdQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PlatformAdminDto>> Handle(GetPlatformAdminByIdQuery request, CancellationToken cancellationToken)
    {
        var admin = await _dbContext.PlatformAdmins
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AdminId && a.DeletedAt == null, cancellationToken);

        if (admin == null)
        {
            return Result<PlatformAdminDto>.Failure("Admin not found");
        }

        return Result<PlatformAdminDto>.Success(new PlatformAdminDto
        {
            Id = admin.Id,
            Email = admin.Email,
            FirstName = admin.FirstName,
            LastName = admin.LastName,
            IsSuperAdmin = admin.IsSuperAdmin,
            IsActive = admin.IsActive,
            LastLoginAt = admin.LastLoginAt,
            CreatedAt = admin.CreatedAt
        });
    }
}
