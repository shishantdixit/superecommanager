# Backend Structure

## Table of Contents
1. [Overview](#overview)
2. [Project Structure](#project-structure)
3. [Layer Details](#layer-details)
4. [Key Patterns](#key-patterns)
5. [Configuration](#configuration)
6. [Dependency Injection](#dependency-injection)

---

## Overview

The backend follows **Clean Architecture** principles with **CQRS** (Command Query Responsibility Segregation) pattern using MediatR. This ensures:

- **Separation of Concerns** - Each layer has a specific responsibility
- **Testability** - Business logic is isolated and easily testable
- **Maintainability** - Changes in one area don't ripple through the system
- **Scalability** - Easy to add new features without touching existing code

### Technology Stack
- **.NET 10** - Latest LTS version
- **Entity Framework Core** - ORM for database access
- **MediatR** - CQRS implementation
- **FluentValidation** - Request validation
- **AutoMapper** - Object mapping
- **Serilog** - Structured logging
- **Hangfire** - Background job processing
- **Redis** - Distributed caching

---

## Project Structure

```
SuperEcomManager/
│
├── src/
│   │
│   ├── SuperEcomManager.Domain/                    # Core Business Logic
│   │   ├── Common/
│   │   │   ├── BaseEntity.cs                       # Base entity with Id
│   │   │   ├── AuditableEntity.cs                  # Adds created/updated tracking
│   │   │   ├── ITenantEntity.cs                    # Interface for tenant-scoped entities
│   │   │   ├── ISoftDeletable.cs                   # Interface for soft delete
│   │   │   └── DomainEvent.cs                      # Base domain event
│   │   │
│   │   ├── Entities/
│   │   │   ├── Tenants/
│   │   │   │   ├── Tenant.cs
│   │   │   │   ├── TenantSettings.cs
│   │   │   │   └── TenantSubscription.cs
│   │   │   │
│   │   │   ├── Identity/
│   │   │   │   ├── User.cs
│   │   │   │   ├── Role.cs
│   │   │   │   ├── Permission.cs
│   │   │   │   ├── UserRole.cs
│   │   │   │   └── RefreshToken.cs
│   │   │   │
│   │   │   ├── Orders/
│   │   │   │   ├── Order.cs
│   │   │   │   ├── OrderItem.cs
│   │   │   │   └── OrderStatusHistory.cs
│   │   │   │
│   │   │   ├── Shipments/
│   │   │   │   ├── Shipment.cs
│   │   │   │   ├── ShipmentItem.cs
│   │   │   │   ├── ShipmentTracking.cs
│   │   │   │   └── CourierConfig.cs
│   │   │   │
│   │   │   ├── NDR/
│   │   │   │   ├── NdrRecord.cs
│   │   │   │   ├── NdrAction.cs
│   │   │   │   └── NdrRemark.cs
│   │   │   │
│   │   │   ├── Inventory/
│   │   │   │   ├── Product.cs
│   │   │   │   ├── ProductVariant.cs
│   │   │   │   ├── Inventory.cs
│   │   │   │   ├── StockMovement.cs
│   │   │   │   └── ChannelProduct.cs
│   │   │   │
│   │   │   ├── Channels/
│   │   │   │   └── SalesChannel.cs
│   │   │   │
│   │   │   ├── Subscriptions/
│   │   │   │   ├── Plan.cs
│   │   │   │   ├── Feature.cs
│   │   │   │   ├── PlanFeature.cs
│   │   │   │   ├── Subscription.cs
│   │   │   │   └── TenantFeature.cs
│   │   │   │
│   │   │   ├── Finance/
│   │   │   │   ├── Expense.cs
│   │   │   │   └── RevenueRecord.cs
│   │   │   │
│   │   │   └── Notifications/
│   │   │       ├── NotificationTemplate.cs
│   │   │       └── NotificationLog.cs
│   │   │
│   │   ├── Enums/
│   │   │   ├── OrderStatus.cs
│   │   │   ├── PaymentStatus.cs
│   │   │   ├── FulfillmentStatus.cs
│   │   │   ├── ShipmentStatus.cs
│   │   │   ├── NdrStatus.cs
│   │   │   ├── NdrReasonCode.cs
│   │   │   ├── ChannelType.cs
│   │   │   ├── CourierType.cs
│   │   │   ├── PaymentMethod.cs
│   │   │   ├── MovementType.cs
│   │   │   └── NotificationType.cs
│   │   │
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs
│   │   │   ├── Address.cs
│   │   │   ├── PhoneNumber.cs
│   │   │   ├── Email.cs
│   │   │   ├── Awb.cs
│   │   │   └── Dimensions.cs
│   │   │
│   │   ├── Events/
│   │   │   ├── Orders/
│   │   │   │   ├── OrderCreatedEvent.cs
│   │   │   │   ├── OrderStatusChangedEvent.cs
│   │   │   │   └── OrderCancelledEvent.cs
│   │   │   ├── Shipments/
│   │   │   │   ├── ShipmentCreatedEvent.cs
│   │   │   │   └── ShipmentStatusChangedEvent.cs
│   │   │   ├── NDR/
│   │   │   │   ├── NdrCreatedEvent.cs
│   │   │   │   └── NdrResolvedEvent.cs
│   │   │   └── Inventory/
│   │   │       └── StockLevelChangedEvent.cs
│   │   │
│   │   ├── Exceptions/
│   │   │   ├── DomainException.cs
│   │   │   ├── EntityNotFoundException.cs
│   │   │   ├── BusinessRuleViolationException.cs
│   │   │   └── ConcurrencyException.cs
│   │   │
│   │   └── SuperEcomManager.Domain.csproj
│   │
│   │
│   ├── SuperEcomManager.Application/               # Application Logic (Use Cases)
│   │   ├── Common/
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs           # Request validation
│   │   │   │   ├── LoggingBehavior.cs              # Request/response logging
│   │   │   │   ├── PerformanceBehavior.cs          # Performance monitoring
│   │   │   │   ├── TenantBehavior.cs               # Tenant context injection
│   │   │   │   ├── AuthorizationBehavior.cs        # Permission checks
│   │   │   │   └── UnhandledExceptionBehavior.cs   # Global exception handling
│   │   │   │
│   │   │   ├── Interfaces/
│   │   │   │   ├── IApplicationDbContext.cs        # Shared DB context
│   │   │   │   ├── ITenantDbContext.cs             # Tenant-specific DB context
│   │   │   │   ├── ICurrentTenantService.cs        # Current tenant info
│   │   │   │   ├── ICurrentUserService.cs          # Current user info
│   │   │   │   ├── IFeatureFlagService.cs          # Feature flag checks
│   │   │   │   ├── IPermissionService.cs           # Permission checks
│   │   │   │   ├── IDateTimeService.cs             # DateTime abstraction
│   │   │   │   ├── ICacheService.cs                # Caching abstraction
│   │   │   │   └── IEventBus.cs                    # Event publishing
│   │   │   │
│   │   │   ├── Mappings/
│   │   │   │   ├── IMapFrom.cs                     # AutoMapper interface
│   │   │   │   └── MappingProfile.cs               # Global mapping profile
│   │   │   │
│   │   │   ├── Models/
│   │   │   │   ├── Result.cs                       # Operation result wrapper
│   │   │   │   ├── PaginatedList.cs                # Paginated response
│   │   │   │   ├── ApiResponse.cs                  # Standard API response
│   │   │   │   └── SortingParams.cs                # Sorting parameters
│   │   │   │
│   │   │   └── Exceptions/
│   │   │       ├── ValidationException.cs
│   │   │       ├── ForbiddenAccessException.cs
│   │   │       ├── FeatureNotEnabledException.cs
│   │   │       └── TenantNotFoundException.cs
│   │   │
│   │   ├── Features/
│   │   │   │
│   │   │   ├── Auth/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── Login/
│   │   │   │   │   │   ├── LoginCommand.cs
│   │   │   │   │   │   ├── LoginCommandHandler.cs
│   │   │   │   │   │   └── LoginCommandValidator.cs
│   │   │   │   │   ├── Register/
│   │   │   │   │   │   ├── RegisterCommand.cs
│   │   │   │   │   │   ├── RegisterCommandHandler.cs
│   │   │   │   │   │   └── RegisterCommandValidator.cs
│   │   │   │   │   ├── RefreshToken/
│   │   │   │   │   ├── ForgotPassword/
│   │   │   │   │   ├── ResetPassword/
│   │   │   │   │   └── Logout/
│   │   │   │   ├── Queries/
│   │   │   │   │   └── GetCurrentUser/
│   │   │   │   └── DTOs/
│   │   │   │       ├── AuthResponse.cs
│   │   │   │       └── TokenDto.cs
│   │   │   │
│   │   │   ├── Tenants/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateTenant/
│   │   │   │   │   ├── UpdateTenant/
│   │   │   │   │   ├── SuspendTenant/
│   │   │   │   │   └── DeleteTenant/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetTenant/
│   │   │   │   │   ├── GetTenants/
│   │   │   │   │   └── GetTenantSettings/
│   │   │   │   └── DTOs/
│   │   │   │       ├── TenantDto.cs
│   │   │   │       └── TenantSettingsDto.cs
│   │   │   │
│   │   │   ├── Users/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateUser/
│   │   │   │   │   ├── UpdateUser/
│   │   │   │   │   ├── DeleteUser/
│   │   │   │   │   ├── AssignRole/
│   │   │   │   │   └── UpdateUserFeatures/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetUser/
│   │   │   │   │   ├── GetUsers/
│   │   │   │   │   └── GetUserPermissions/
│   │   │   │   └── DTOs/
│   │   │   │
│   │   │   ├── Roles/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateRole/
│   │   │   │   │   ├── UpdateRole/
│   │   │   │   │   ├── DeleteRole/
│   │   │   │   │   └── UpdateRolePermissions/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetRole/
│   │   │   │   │   ├── GetRoles/
│   │   │   │   │   └── GetPermissions/
│   │   │   │   └── DTOs/
│   │   │   │
│   │   │   ├── Orders/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateOrder/
│   │   │   │   │   ├── UpdateOrder/
│   │   │   │   │   ├── UpdateOrderStatus/
│   │   │   │   │   ├── CancelOrder/
│   │   │   │   │   ├── SyncOrders/
│   │   │   │   │   └── BulkUpdateOrders/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetOrder/
│   │   │   │   │   ├── GetOrders/
│   │   │   │   │   ├── SearchOrders/
│   │   │   │   │   └── GetOrderStats/
│   │   │   │   ├── DTOs/
│   │   │   │   │   ├── OrderDto.cs
│   │   │   │   │   ├── OrderItemDto.cs
│   │   │   │   │   ├── OrderListDto.cs
│   │   │   │   │   └── OrderStatsDto.cs
│   │   │   │   └── EventHandlers/
│   │   │   │       ├── OrderCreatedEventHandler.cs
│   │   │   │       └── OrderStatusChangedEventHandler.cs
│   │   │   │
│   │   │   ├── Shipments/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateShipment/
│   │   │   │   │   ├── CancelShipment/
│   │   │   │   │   ├── UpdateShipmentStatus/
│   │   │   │   │   ├── ProcessWebhook/
│   │   │   │   │   └── BulkCreateShipments/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetShipment/
│   │   │   │   │   ├── GetShipments/
│   │   │   │   │   ├── GetShipmentTracking/
│   │   │   │   │   └── GetShipmentStats/
│   │   │   │   ├── DTOs/
│   │   │   │   └── EventHandlers/
│   │   │   │
│   │   │   ├── NDR/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateNdrRecord/
│   │   │   │   │   ├── AssignNdr/
│   │   │   │   │   ├── AddNdrAction/
│   │   │   │   │   ├── AddNdrRemark/
│   │   │   │   │   ├── ScheduleReattempt/
│   │   │   │   │   ├── CloseNdr/
│   │   │   │   │   └── BulkAssignNdr/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetNdrInbox/
│   │   │   │   │   ├── GetNdrRecord/
│   │   │   │   │   ├── GetMyAssignedNdrs/
│   │   │   │   │   ├── GetNdrAnalytics/
│   │   │   │   │   └── GetEmployeeNdrPerformance/
│   │   │   │   ├── DTOs/
│   │   │   │   │   ├── NdrRecordDto.cs
│   │   │   │   │   ├── NdrListDto.cs
│   │   │   │   │   ├── NdrActionDto.cs
│   │   │   │   │   └── NdrAnalyticsDto.cs
│   │   │   │   └── EventHandlers/
│   │   │   │
│   │   │   ├── Inventory/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateProduct/
│   │   │   │   │   ├── UpdateProduct/
│   │   │   │   │   ├── DeleteProduct/
│   │   │   │   │   ├── AdjustStock/
│   │   │   │   │   ├── SyncInventory/
│   │   │   │   │   └── MapChannelProduct/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetProduct/
│   │   │   │   │   ├── GetProducts/
│   │   │   │   │   ├── GetInventoryLevels/
│   │   │   │   │   ├── GetStockMovements/
│   │   │   │   │   └── GetLowStockProducts/
│   │   │   │   └── DTOs/
│   │   │   │
│   │   │   ├── Channels/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── ConnectChannel/
│   │   │   │   │   ├── DisconnectChannel/
│   │   │   │   │   ├── UpdateChannelSettings/
│   │   │   │   │   └── SyncChannelData/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetChannel/
│   │   │   │   │   ├── GetChannels/
│   │   │   │   │   └── GetAvailableChannelTypes/
│   │   │   │   └── DTOs/
│   │   │   │
│   │   │   ├── Couriers/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── AddCourierConfig/
│   │   │   │   │   ├── UpdateCourierConfig/
│   │   │   │   │   ├── DeleteCourierConfig/
│   │   │   │   │   └── SetDefaultCourier/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetCourierConfigs/
│   │   │   │   │   └── GetAvailableCourierTypes/
│   │   │   │   └── DTOs/
│   │   │   │
│   │   │   ├── Subscriptions/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateSubscription/
│   │   │   │   │   ├── ChangePlan/
│   │   │   │   │   ├── CancelSubscription/
│   │   │   │   │   └── ToggleTenantFeature/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetSubscription/
│   │   │   │   │   ├── GetPlans/
│   │   │   │   │   ├── GetFeatures/
│   │   │   │   │   └── GetMyFeatures/
│   │   │   │   └── DTOs/
│   │   │   │
│   │   │   ├── Finance/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateExpense/
│   │   │   │   │   ├── UpdateExpense/
│   │   │   │   │   └── DeleteExpense/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetExpenses/
│   │   │   │   │   ├── GetProfitLoss/
│   │   │   │   │   └── GetPlatformWiseAnalytics/
│   │   │   │   └── DTOs/
│   │   │   │
│   │   │   ├── Analytics/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetDashboardStats/
│   │   │   │   │   ├── GetOrderAnalytics/
│   │   │   │   │   ├── GetChannelAnalytics/
│   │   │   │   │   └── GetNdrAnalytics/
│   │   │   │   └── DTOs/
│   │   │   │
│   │   │   └── Notifications/
│   │   │       ├── Commands/
│   │   │       │   ├── SendNotification/
│   │   │       │   ├── CreateTemplate/
│   │   │       │   └── UpdatePreferences/
│   │   │       ├── Queries/
│   │   │       │   ├── GetNotificationLogs/
│   │   │       │   └── GetTemplates/
│   │   │       └── DTOs/
│   │   │
│   │   ├── Abstractions/
│   │   │   ├── Channels/
│   │   │   │   ├── IChannelAdapter.cs
│   │   │   │   ├── IChannelAdapterFactory.cs
│   │   │   │   ├── ChannelCredentials.cs
│   │   │   │   └── UnifiedModels/
│   │   │   │       ├── UnifiedOrder.cs
│   │   │   │       ├── UnifiedOrderItem.cs
│   │   │   │       └── UnifiedProduct.cs
│   │   │   │
│   │   │   ├── Couriers/
│   │   │   │   ├── ICourierAdapter.cs
│   │   │   │   ├── ICourierAdapterFactory.cs
│   │   │   │   └── CourierModels/
│   │   │   │       ├── ShipmentRequest.cs
│   │   │   │       ├── ShipmentResponse.cs
│   │   │   │       └── TrackingEvent.cs
│   │   │   │
│   │   │   └── Notifications/
│   │   │       ├── IEmailService.cs
│   │   │       ├── ISmsService.cs
│   │   │       └── IWhatsAppService.cs
│   │   │
│   │   └── DependencyInjection.cs
│   │
│   │
│   ├── SuperEcomManager.Infrastructure/            # External Concerns
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs             # Shared schema context
│   │   │   ├── TenantDbContext.cs                  # Tenant schema context
│   │   │   ├── TenantDbContextFactory.cs           # Factory for tenant contexts
│   │   │   │
│   │   │   ├── Configurations/
│   │   │   │   ├── Shared/
│   │   │   │   │   ├── TenantConfiguration.cs
│   │   │   │   │   ├── PlanConfiguration.cs
│   │   │   │   │   ├── FeatureConfiguration.cs
│   │   │   │   │   └── SubscriptionConfiguration.cs
│   │   │   │   └── Tenant/
│   │   │   │       ├── UserConfiguration.cs
│   │   │   │       ├── OrderConfiguration.cs
│   │   │   │       ├── ShipmentConfiguration.cs
│   │   │   │       ├── NdrRecordConfiguration.cs
│   │   │   │       └── ... (all entity configs)
│   │   │   │
│   │   │   ├── Interceptors/
│   │   │   │   ├── AuditableEntityInterceptor.cs
│   │   │   │   ├── SoftDeleteInterceptor.cs
│   │   │   │   └── DomainEventInterceptor.cs
│   │   │   │
│   │   │   ├── Migrations/
│   │   │   │   ├── Shared/                          # Migrations for public schema
│   │   │   │   └── Tenant/                          # Migrations for tenant schemas
│   │   │   │
│   │   │   └── Repositories/
│   │   │       ├── GenericRepository.cs
│   │   │       └── UnitOfWork.cs
│   │   │
│   │   ├── Identity/
│   │   │   ├── JwtTokenService.cs
│   │   │   ├── CurrentUserService.cs
│   │   │   ├── PermissionService.cs
│   │   │   └── PasswordHasher.cs
│   │   │
│   │   ├── MultiTenancy/
│   │   │   ├── TenantResolver.cs                   # Resolves tenant from request
│   │   │   ├── CurrentTenantService.cs             # Provides current tenant info
│   │   │   ├── TenantSchemaService.cs              # Manages tenant schemas
│   │   │   └── TenantConnectionFactory.cs          # Creates tenant DB connections
│   │   │
│   │   ├── FeatureManagement/
│   │   │   ├── FeatureFlagService.cs
│   │   │   ├── SubscriptionFeatureProvider.cs
│   │   │   └── TenantFeatureProvider.cs
│   │   │
│   │   ├── Caching/
│   │   │   ├── RedisCacheService.cs
│   │   │   ├── DistributedCacheService.cs
│   │   │   └── CacheKeys.cs
│   │   │
│   │   ├── Messaging/
│   │   │   ├── RabbitMqService.cs
│   │   │   ├── EventBus.cs
│   │   │   └── Consumers/
│   │   │       ├── OrderEventConsumer.cs
│   │   │       ├── ShipmentEventConsumer.cs
│   │   │       └── NdrEventConsumer.cs
│   │   │
│   │   ├── BackgroundJobs/
│   │   │   ├── HangfireConfiguration.cs
│   │   │   └── Jobs/
│   │   │       ├── OrderSyncJob.cs
│   │   │       ├── InventorySyncJob.cs
│   │   │       ├── ShipmentTrackingJob.cs
│   │   │       ├── NdrProcessingJob.cs
│   │   │       ├── NotificationJob.cs
│   │   │       └── CleanupJob.cs
│   │   │
│   │   ├── ExternalServices/
│   │   │   ├── Email/
│   │   │   │   └── SendGridService.cs              # Primary email provider
│   │   │   ├── SMS/
│   │   │   │   └── Msg91Service.cs                 # Primary SMS provider (India)
│   │   │   └── WhatsApp/
│   │   │       └── GupshupService.cs               # Primary WhatsApp provider (India)
│   │   │
│   │   ├── Security/
│   │   │   ├── EncryptionService.cs
│   │   │   ├── DataMaskingService.cs
│   │   │   └── AuditLogService.cs
│   │   │
│   │   ├── Services/
│   │   │   └── DateTimeService.cs
│   │   │
│   │   └── DependencyInjection.cs
│   │
│   │
│   ├── SuperEcomManager.Integrations/              # External Platform Integrations
│   │   │
│   │   ├── Channels/
│   │   │   ├── Common/
│   │   │   │   ├── BaseChannelAdapter.cs
│   │   │   │   ├── ChannelAdapterFactory.cs
│   │   │   │   ├── ChannelWebhookProcessor.cs
│   │   │   │   └── RateLimiter.cs
│   │   │   │
│   │   │   ├── Shopify/
│   │   │   │   ├── ShopifyAdapter.cs
│   │   │   │   ├── ShopifyOrderService.cs
│   │   │   │   ├── ShopifyInventoryService.cs
│   │   │   │   ├── ShopifyProductService.cs
│   │   │   │   ├── ShopifyWebhookHandler.cs
│   │   │   │   ├── Mappers/
│   │   │   │   │   ├── ShopifyOrderMapper.cs
│   │   │   │   │   └── ShopifyProductMapper.cs
│   │   │   │   └── Models/
│   │   │   │       ├── ShopifyOrder.cs
│   │   │   │       ├── ShopifyProduct.cs
│   │   │   │       └── ShopifyWebhookPayload.cs
│   │   │   │
│   │   │   ├── Amazon/
│   │   │   │   ├── AmazonAdapter.cs
│   │   │   │   ├── AmazonSpApiClient.cs
│   │   │   │   ├── Mappers/
│   │   │   │   └── Models/
│   │   │   │
│   │   │   ├── Flipkart/
│   │   │   │   └── ... (similar structure)
│   │   │   │
│   │   │   ├── Meesho/
│   │   │   │   └── ... (similar structure)
│   │   │   │
│   │   │   └── WooCommerce/
│   │   │       └── ... (similar structure)
│   │   │
│   │   ├── Couriers/
│   │   │   ├── Common/
│   │   │   │   ├── BaseCourierAdapter.cs
│   │   │   │   ├── CourierAdapterFactory.cs
│   │   │   │   └── CourierWebhookProcessor.cs
│   │   │   │
│   │   │   ├── Shiprocket/
│   │   │   │   ├── ShiprocketAdapter.cs
│   │   │   │   ├── ShiprocketApiClient.cs
│   │   │   │   ├── ShiprocketWebhookHandler.cs
│   │   │   │   ├── Mappers/
│   │   │   │   └── Models/
│   │   │   │
│   │   │   ├── Delhivery/
│   │   │   │   └── ... (similar structure)
│   │   │   │
│   │   │   ├── BlueDart/
│   │   │   │   └── ... (similar structure)
│   │   │   │
│   │   │   └── Custom/
│   │   │       └── CustomCourierAdapter.cs
│   │   │
│   │   └── DependencyInjection.cs
│   │
│   │
│   └── SuperEcomManager.API/                       # Web API (Presentation)
│       │
│       ├── Controllers/
│       │   ├── V1/
│       │   │   ├── AuthController.cs
│       │   │   ├── UsersController.cs
│       │   │   ├── RolesController.cs
│       │   │   ├── OrdersController.cs
│       │   │   ├── ShipmentsController.cs
│       │   │   ├── NdrController.cs
│       │   │   ├── InventoryController.cs
│       │   │   ├── ProductsController.cs
│       │   │   ├── ChannelsController.cs
│       │   │   ├── CouriersController.cs
│       │   │   ├── FinanceController.cs
│       │   │   ├── AnalyticsController.cs
│       │   │   ├── NotificationsController.cs
│       │   │   ├── SettingsController.cs
│       │   │   └── WebhooksController.cs
│       │   │
│       │   └── SuperAdmin/
│       │       ├── TenantsController.cs
│       │       ├── PlansController.cs
│       │       ├── FeaturesController.cs
│       │       ├── SubscriptionsController.cs
│       │       └── SystemController.cs
│       │
│       ├── Middleware/
│       │   ├── TenantResolutionMiddleware.cs
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   ├── RequestLoggingMiddleware.cs
│       │   ├── CorrelationIdMiddleware.cs
│       │   └── RateLimitingMiddleware.cs
│       │
│       ├── Filters/
│       │   ├── RequirePermissionAttribute.cs
│       │   ├── RequireFeatureAttribute.cs
│       │   ├── ValidateTenantAttribute.cs
│       │   ├── AuditLogAttribute.cs
│       │   └── ApiExceptionFilterAttribute.cs
│       │
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs
│       │   ├── ApplicationBuilderExtensions.cs
│       │   └── ClaimsPrincipalExtensions.cs
│       │
│       ├── Hubs/
│       │   ├── NotificationHub.cs
│       │   └── DashboardHub.cs
│       │
│       ├── HealthChecks/
│       │   ├── DatabaseHealthCheck.cs
│       │   ├── RedisHealthCheck.cs
│       │   └── RabbitMqHealthCheck.cs
│       │
│       ├── Properties/
│       │   └── launchSettings.json
│       │
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── appsettings.Production.json
│       ├── Program.cs
│       ├── Dockerfile
│       │
│       └── SuperEcomManager.API.csproj
│
│
├── tests/
│   ├── SuperEcomManager.Domain.Tests/
│   ├── SuperEcomManager.Application.Tests/
│   ├── SuperEcomManager.Infrastructure.Tests/
│   ├── SuperEcomManager.Integrations.Tests/
│   └── SuperEcomManager.API.Tests/
│
│
├── docker/
│   ├── docker-compose.yml
│   ├── docker-compose.override.yml
│   ├── docker-compose.prod.yml
│   └── .env.example
│
│
├── scripts/
│   ├── init-db.sql
│   ├── create-tenant-schema.sql
│   ├── seed-data.sql
│   └── run-migrations.sh
│
│
├── SuperEcomManager.sln
├── .editorconfig
├── .gitignore
├── Directory.Build.props
└── README.md
```

---

## Layer Details

### Domain Layer

The innermost layer containing enterprise business rules. No dependencies on other layers.

```csharp
// Example: Domain Entity
namespace SuperEcomManager.Domain.Entities.Orders;

public class Order : AuditableEntity, ISoftDeletable
{
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid ChannelId { get; private set; }
    public string ExternalOrderId { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public FulfillmentStatus FulfillmentStatus { get; private set; }

    public string CustomerName { get; private set; } = string.Empty;
    public string? CustomerEmail { get; private set; }
    public string? CustomerPhone { get; private set; }

    public Address ShippingAddress { get; private set; } = null!;
    public Address? BillingAddress { get; private set; }

    public Money Subtotal { get; private set; } = Money.Zero;
    public Money DiscountAmount { get; private set; } = Money.Zero;
    public Money TaxAmount { get; private set; } = Money.Zero;
    public Money ShippingAmount { get; private set; } = Money.Zero;
    public Money TotalAmount { get; private set; } = Money.Zero;

    public PaymentMethod? PaymentMethod { get; private set; }
    public DateTime OrderDate { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private readonly List<OrderStatusHistory> _statusHistory = new();
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public DateTime? DeletedAt { get; set; }

    private Order() { } // EF Core

    public static Order Create(
        Guid channelId,
        string externalOrderId,
        string customerName,
        Address shippingAddress,
        Money totalAmount,
        DateTime orderDate)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            ChannelId = channelId,
            ExternalOrderId = externalOrderId,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            FulfillmentStatus = FulfillmentStatus.Unfulfilled,
            CustomerName = customerName,
            ShippingAddress = shippingAddress,
            TotalAmount = totalAmount,
            OrderDate = orderDate
        };

        order.AddDomainEvent(new OrderCreatedEvent(order.Id));
        return order;
    }

    public void UpdateStatus(OrderStatus newStatus, Guid? userId = null)
    {
        if (Status == newStatus) return;

        var oldStatus = Status;
        Status = newStatus;

        _statusHistory.Add(new OrderStatusHistory(Id, newStatus, userId));
        AddDomainEvent(new OrderStatusChangedEvent(Id, oldStatus, newStatus));
    }

    public void AddItem(OrderItem item)
    {
        _items.Add(item);
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        Subtotal = new Money(_items.Sum(i => i.TotalAmount.Amount), TotalAmount.Currency);
        TotalAmount = Subtotal - DiscountAmount + TaxAmount + ShippingAmount;
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}
```

### Application Layer

Contains application business rules. Uses CQRS pattern with MediatR.

```csharp
// Example: Command
namespace SuperEcomManager.Application.Features.Orders.Commands.CreateOrder;

public record CreateOrderCommand : IRequest<Result<OrderDto>>
{
    public Guid ChannelId { get; init; }
    public string ExternalOrderId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string? CustomerPhone { get; init; }
    public AddressDto ShippingAddress { get; init; } = null!;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "INR";
    public DateTime OrderDate { get; init; }
    public List<CreateOrderItemDto> Items { get; init; } = new();
}

// Validator
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty().WithMessage("Channel is required");

        RuleFor(x => x.ExternalOrderId)
            .NotEmpty().WithMessage("External order ID is required")
            .MaximumLength(255);

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required")
            .MaximumLength(255);

        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Total amount must be greater than 0");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required");
    }
}

// Handler
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly ITenantDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _userService;

    public CreateOrderCommandHandler(
        ITenantDbContext context,
        IMapper mapper,
        ICurrentUserService userService)
    {
        _context = context;
        _mapper = mapper;
        _userService = userService;
    }

    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate
        var existingOrder = await _context.Orders
            .FirstOrDefaultAsync(o =>
                o.ChannelId == request.ChannelId &&
                o.ExternalOrderId == request.ExternalOrderId,
                cancellationToken);

        if (existingOrder != null)
        {
            return Result<OrderDto>.Failure("DUPLICATE_ORDER", "Order already exists");
        }

        // Create order
        var shippingAddress = new Address(
            request.ShippingAddress.Name,
            request.ShippingAddress.Phone,
            request.ShippingAddress.Line1,
            request.ShippingAddress.Line2,
            request.ShippingAddress.City,
            request.ShippingAddress.State,
            request.ShippingAddress.PostalCode,
            request.ShippingAddress.Country);

        var order = Order.Create(
            request.ChannelId,
            request.ExternalOrderId,
            request.CustomerName,
            shippingAddress,
            new Money(request.TotalAmount, request.Currency),
            request.OrderDate);

        order.CustomerEmail = request.CustomerEmail;
        order.CustomerPhone = request.CustomerPhone;

        // Add items
        foreach (var itemDto in request.Items)
        {
            var item = new OrderItem(
                order.Id,
                itemDto.Sku,
                itemDto.Name,
                itemDto.Quantity,
                new Money(itemDto.UnitPrice, request.Currency));

            order.AddItem(item);
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<OrderDto>.Success(_mapper.Map<OrderDto>(order));
    }
}
```

### Infrastructure Layer

Contains implementations for interfaces defined in Application layer.

```csharp
// Example: Multi-tenant DbContext
namespace SuperEcomManager.Infrastructure.Persistence;

public class TenantDbContext : DbContext, ITenantDbContext
{
    private readonly ICurrentTenantService _tenantService;
    private readonly string _schema;

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        ICurrentTenantService tenantService) : base(options)
    {
        _tenantService = tenantService;
        _schema = _tenantService.SchemaName;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<NdrRecord> NdrRecords => Set<NdrRecord>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Inventory> Inventory => Set<Inventory>();
    public DbSet<SalesChannel> SalesChannels => Set<SalesChannel>();
    // ... other DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set default schema for tenant
        modelBuilder.HasDefaultSchema(_schema);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TenantDbContext).Assembly,
            t => t.Namespace?.Contains("Tenant") ?? false);

        base.OnModelCreating(modelBuilder);
    }
}
```

### API Layer

Thin layer that handles HTTP concerns and delegates to Application layer.

```csharp
// Example: Controller
namespace SuperEcomManager.API.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get orders with filtering and pagination
    /// </summary>
    [HttpGet]
    [RequireFeature("orders_management")]
    [RequirePermission("orders.view")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<OrderListDto>>), 200)]
    public async Task<IActionResult> GetOrders([FromQuery] GetOrdersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<PaginatedList<OrderListDto>>.Success(result));
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequireFeature("orders_management")]
    [RequirePermission("orders.view")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var result = await _mediator.Send(new GetOrderQuery { Id = id });

        if (result == null)
            return NotFound(ApiResponse<object>.Failure("NOT_FOUND", "Order not found"));

        return Ok(ApiResponse<OrderDto>.Success(result));
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    [RequireFeature("orders_management")]
    [RequirePermission("orders.create")]
    [AuditLog("Order created")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Failure(result.Error!.Code, result.Error.Message));

        return CreatedAtAction(
            nameof(GetOrder),
            new { id = result.Value!.Id },
            ApiResponse<OrderDto>.Success(result.Value));
    }

    /// <summary>
    /// Update order status
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [RequireFeature("orders_management")]
    [RequirePermission("orders.edit")]
    [AuditLog("Order status updated")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusCommand command)
    {
        command.OrderId = id;
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Failure(result.Error!.Code, result.Error.Message));

        return Ok(ApiResponse<OrderDto>.Success(result.Value));
    }
}
```

---

## Key Patterns

### CQRS with MediatR

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  Controller  │────▶│   MediatR    │────▶│   Handler    │
└──────────────┘     └──────────────┘     └──────────────┘
                            │
                            │ Pipeline Behaviors
                            ▼
                     ┌──────────────┐
                     │  Validation  │
                     │   Logging    │
                     │  Permission  │
                     │   Feature    │
                     └──────────────┘
```

### Result Pattern

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string code, string message) => new(new Error(code, message));
}
```

### Domain Events

```csharp
// Publishing
public class Order : AuditableEntity
{
    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}

// Handling in SaveChanges
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
{
    var entities = ChangeTracker.Entries<AuditableEntity>()
        .Where(e => e.Entity.DomainEvents.Any())
        .Select(e => e.Entity)
        .ToList();

    var domainEvents = entities.SelectMany(e => e.DomainEvents).ToList();

    var result = await base.SaveChangesAsync(cancellationToken);

    // Dispatch events after save
    foreach (var domainEvent in domainEvents)
    {
        await _mediator.Publish(domainEvent, cancellationToken);
    }

    entities.ForEach(e => e.ClearDomainEvents());

    return result;
}
```

---

## Configuration

### appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=superecommanager;Username=postgres;Password=xxx",
    "Redis": "localhost:6379"
  },

  "Jwt": {
    "Secret": "your-256-bit-secret-key-here",
    "Issuer": "SuperEcomManager",
    "Audience": "SuperEcomManagerClients",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },

  "MultiTenancy": {
    "DefaultSchema": "public",
    "TenantSchemaPrefix": "tenant_",
    "ResolutionStrategy": "Header"
  },

  "Redis": {
    "InstanceName": "superecom:",
    "DefaultExpirationMinutes": 30
  },

  "RabbitMq": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  },

  "Hangfire": {
    "DashboardPath": "/hangfire",
    "Workers": 5
  },

  "Integrations": {
    "Shopify": {
      "ApiVersion": "2024-01"
    },
    "Shiprocket": {
      "BaseUrl": "https://apiv2.shiprocket.in/v1/external"
    }
  },

  "Notifications": {
    "SendGrid": {
      "ApiKey": "",
      "FromEmail": "noreply@superecom.com",
      "FromName": "SuperEcomManager"
    },
    "Msg91": {
      "AuthKey": "",
      "SenderId": "",
      "DltTemplateId": ""
    },
    "Gupshup": {
      "ApiKey": "",
      "AppName": "",
      "SourceMobile": ""
    }
  },

  "Security": {
    "EncryptionKey": "your-encryption-key",
    "AllowedOrigins": ["http://localhost:3000"],
    "RateLimiting": {
      "EnableRateLimiting": true,
      "RequestsPerMinute": 100
    }
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

---

## Dependency Injection

### Program.cs Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIntegrations(builder.Configuration);

// Add API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT config */ });

// Add SignalR
builder.Services.AddSignalR();

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

app.Run();
```

---

## Next Steps

See the following documents for more details:
- [Frontend Structure](04-frontend-structure.md)
- [Feature Flags & RBAC](05-feature-flags-rbac.md)
- [API Design](08-api-design.md)
