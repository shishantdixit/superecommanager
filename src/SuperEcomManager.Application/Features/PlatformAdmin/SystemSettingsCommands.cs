using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Platform;

namespace SuperEcomManager.Application.Features.PlatformAdmin;

#region DTOs

public class PlatformSettingDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SettingsByCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public List<PlatformSettingDto> Settings { get; set; } = new();
}

#endregion

#region Commands

/// <summary>
/// Command to create or update a platform setting.
/// </summary>
public class UpsertPlatformSettingCommand : IRequest<Result<PlatformSettingDto>>
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public bool IsEncrypted { get; set; }
}

public class UpsertPlatformSettingCommandHandler : IRequestHandler<UpsertPlatformSettingCommand, Result<PlatformSettingDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<UpsertPlatformSettingCommandHandler> _logger;

    public UpsertPlatformSettingCommandHandler(IApplicationDbContext dbContext, ILogger<UpsertPlatformSettingCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<PlatformSettingDto>> Handle(UpsertPlatformSettingCommand request, CancellationToken cancellationToken)
    {
        var existingSetting = await _dbContext.PlatformSettings
            .FirstOrDefaultAsync(s => s.Key == request.Key, cancellationToken);

        PlatformSettings setting;

        if (existingSetting != null)
        {
            existingSetting.UpdateValue(request.Value);
            existingSetting.UpdateMetadata(request.Description, request.IsPublic);
            setting = existingSetting;
            _logger.LogInformation("Updated platform setting {Key}", request.Key);
        }
        else
        {
            setting = PlatformSettings.Create(
                request.Key,
                request.Value,
                request.Category,
                request.Description,
                request.IsPublic,
                request.IsEncrypted);
            _dbContext.PlatformSettings.Add(setting);
            _logger.LogInformation("Created platform setting {Key}", request.Key);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<PlatformSettingDto>.Success(new PlatformSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.IsEncrypted ? "********" : setting.Value,
            Description = setting.Description,
            Category = setting.Category,
            IsPublic = setting.IsPublic,
            IsEncrypted = setting.IsEncrypted,
            UpdatedAt = setting.UpdatedAt
        });
    }
}

/// <summary>
/// Command to update multiple settings at once.
/// </summary>
public class UpdateBulkSettingsCommand : IRequest<Result>
{
    public List<SettingUpdateItem> Settings { get; set; } = new();
}

public class SettingUpdateItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class UpdateBulkSettingsCommandHandler : IRequestHandler<UpdateBulkSettingsCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<UpdateBulkSettingsCommandHandler> _logger;

    public UpdateBulkSettingsCommandHandler(IApplicationDbContext dbContext, ILogger<UpdateBulkSettingsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateBulkSettingsCommand request, CancellationToken cancellationToken)
    {
        var keys = request.Settings.Select(s => s.Key).ToList();
        var existingSettings = await _dbContext.PlatformSettings
            .Where(s => keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, cancellationToken);

        foreach (var item in request.Settings)
        {
            if (existingSettings.TryGetValue(item.Key, out var setting))
            {
                setting.UpdateValue(item.Value);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated {Count} platform settings", request.Settings.Count);

        return Result.Success();
    }
}

/// <summary>
/// Command to delete a platform setting.
/// </summary>
public class DeletePlatformSettingCommand : IRequest<Result>
{
    public string Key { get; set; } = string.Empty;
}

public class DeletePlatformSettingCommandHandler : IRequestHandler<DeletePlatformSettingCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<DeletePlatformSettingCommandHandler> _logger;

    public DeletePlatformSettingCommandHandler(IApplicationDbContext dbContext, ILogger<DeletePlatformSettingCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(DeletePlatformSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await _dbContext.PlatformSettings
            .FirstOrDefaultAsync(s => s.Key == request.Key, cancellationToken);

        if (setting == null)
        {
            return Result.Failure("Setting not found");
        }

        _dbContext.PlatformSettings.Remove(setting);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted platform setting {Key}", request.Key);

        return Result.Success();
    }
}

/// <summary>
/// Command to seed default platform settings.
/// </summary>
public class SeedDefaultSettingsCommand : IRequest<Result>
{
}

public class SeedDefaultSettingsCommandHandler : IRequestHandler<SeedDefaultSettingsCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<SeedDefaultSettingsCommandHandler> _logger;

    public SeedDefaultSettingsCommandHandler(IApplicationDbContext dbContext, ILogger<SeedDefaultSettingsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(SeedDefaultSettingsCommand request, CancellationToken cancellationToken)
    {
        var existingKeys = await _dbContext.PlatformSettings
            .Select(s => s.Key)
            .ToListAsync(cancellationToken);

        var defaults = GetDefaultSettings();
        var toAdd = defaults.Where(d => !existingKeys.Contains(d.Key)).ToList();

        if (toAdd.Any())
        {
            _dbContext.PlatformSettings.AddRange(toAdd);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded {Count} default platform settings", toAdd.Count);
        }

        return Result.Success();
    }

    private static List<PlatformSettings> GetDefaultSettings()
    {
        return new List<PlatformSettings>
        {
            // General
            PlatformSettings.Create(SettingKeys.PlatformName, "SuperEcomManager", SettingCategories.General, "Platform display name", true),
            PlatformSettings.Create(SettingKeys.SupportEmail, "support@superecommanager.com", SettingCategories.General, "Support email address", true),
            PlatformSettings.Create(SettingKeys.SupportPhone, "", SettingCategories.General, "Support phone number", true),
            PlatformSettings.Create(SettingKeys.TermsUrl, "", SettingCategories.General, "Terms of service URL", true),
            PlatformSettings.Create(SettingKeys.PrivacyUrl, "", SettingCategories.General, "Privacy policy URL", true),

            // Security
            PlatformSettings.Create(SettingKeys.PasswordMinLength, "8", SettingCategories.Security, "Minimum password length"),
            PlatformSettings.Create(SettingKeys.SessionTimeoutMinutes, "60", SettingCategories.Security, "Session timeout in minutes"),
            PlatformSettings.Create(SettingKeys.MaxLoginAttempts, "5", SettingCategories.Security, "Maximum login attempts before lockout"),
            PlatformSettings.Create(SettingKeys.LockoutDurationMinutes, "30", SettingCategories.Security, "Account lockout duration in minutes"),
            PlatformSettings.Create(SettingKeys.RequireTwoFactor, "false", SettingCategories.Security, "Require two-factor authentication"),

            // Features
            PlatformSettings.Create(SettingKeys.MaintenanceMode, "false", SettingCategories.Feature, "Enable maintenance mode", true),
            PlatformSettings.Create(SettingKeys.RegistrationEnabled, "true", SettingCategories.Feature, "Enable new tenant registration", true),
            PlatformSettings.Create(SettingKeys.TrialDays, "14", SettingCategories.Feature, "Default trial period in days"),
            PlatformSettings.Create(SettingKeys.MaxTenantsPerUser, "5", SettingCategories.Feature, "Maximum tenants per user")
        };
    }
}

#endregion

#region Queries

/// <summary>
/// Query to get all platform settings.
/// </summary>
public class GetPlatformSettingsQuery : IRequest<List<SettingsByCategoryDto>>
{
    public string? Category { get; set; }
    public bool PublicOnly { get; set; }
}

public class GetPlatformSettingsQueryHandler : IRequestHandler<GetPlatformSettingsQuery, List<SettingsByCategoryDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPlatformSettingsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<SettingsByCategoryDto>> Handle(GetPlatformSettingsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.PlatformSettings.AsNoTracking();

        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(s => s.Category == request.Category);
        }

        if (request.PublicOnly)
        {
            query = query.Where(s => s.IsPublic);
        }

        var settings = await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .ToListAsync(cancellationToken);

        return settings
            .GroupBy(s => s.Category)
            .Select(g => new SettingsByCategoryDto
            {
                Category = g.Key,
                Settings = g.Select(s => new PlatformSettingDto
                {
                    Id = s.Id,
                    Key = s.Key,
                    Value = s.IsEncrypted ? "********" : s.Value,
                    Description = s.Description,
                    Category = s.Category,
                    IsPublic = s.IsPublic,
                    IsEncrypted = s.IsEncrypted,
                    UpdatedAt = s.UpdatedAt
                }).ToList()
            })
            .ToList();
    }
}

/// <summary>
/// Query to get a specific setting by key.
/// </summary>
public class GetPlatformSettingByKeyQuery : IRequest<Result<PlatformSettingDto>>
{
    public string Key { get; set; } = string.Empty;
}

public class GetPlatformSettingByKeyQueryHandler : IRequestHandler<GetPlatformSettingByKeyQuery, Result<PlatformSettingDto>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetPlatformSettingByKeyQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PlatformSettingDto>> Handle(GetPlatformSettingByKeyQuery request, CancellationToken cancellationToken)
    {
        var setting = await _dbContext.PlatformSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == request.Key, cancellationToken);

        if (setting == null)
        {
            return Result<PlatformSettingDto>.Failure("Setting not found");
        }

        return Result<PlatformSettingDto>.Success(new PlatformSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.IsEncrypted ? "********" : setting.Value,
            Description = setting.Description,
            Category = setting.Category,
            IsPublic = setting.IsPublic,
            IsEncrypted = setting.IsEncrypted,
            UpdatedAt = setting.UpdatedAt
        });
    }
}

/// <summary>
/// Query to get setting categories.
/// </summary>
public class GetSettingCategoriesQuery : IRequest<List<string>>
{
}

public class GetSettingCategoriesQueryHandler : IRequestHandler<GetSettingCategoriesQuery, List<string>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetSettingCategoriesQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<string>> Handle(GetSettingCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.PlatformSettings
            .AsNoTracking()
            .Select(s => s.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }
}

#endregion
