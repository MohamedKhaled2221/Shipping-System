using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShippingSystem.Application.Abstractions.Notifications;
using ShippingSystem.Application.Abstractions.Payments;
using ShippingSystem.Application.Abstractions.Realtime;
using ShippingSystem.Application.Abstractions.Shipping;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Services;
using ShippingSystem.Infrastructure.BackgroundJobs;
using ShippingSystem.Infrastructure.Caching;
using ShippingSystem.Infrastructure.HealthChecks;
using ShippingSystem.Infrastructure.Notifications;
using ShippingSystem.Infrastructure.Options;
using ShippingSystem.Infrastructure.Payments;
using ShippingSystem.Infrastructure.Persistence;
using ShippingSystem.Infrastructure.Persistence.Interceptors;
using ShippingSystem.Infrastructure.Persistence.Repositories;
using ShippingSystem.Infrastructure.Realtime;
using ShippingSystem.Infrastructure.Services;
using ShippingSystem.Infrastructure.ShippingProviders;

namespace ShippingSystem.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer. Called once from the API's Program.cs as
/// `builder.Services.AddInfrastructure(builder.Configuration);` — every interface declared
/// in Application gets exactly one registration here.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Missing 'ConnectionStrings:Default'. Add it to appsettings.json or user secrets.");

        services.Configure<InventoryReservationOptions>(
            configuration.GetSection(InventoryReservationOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        AddPersistence(services, connectionString);
        AddRepositories(services);
        AddDomainServices(services);
        AddShippingProviders(services);
        AddPaymentsAndNotifications(services);
        AddAuth(services);
        AddBackgroundJobs(services);
        AddCaching(services, configuration);
        AddRealtime(services);
        AddHealthChecks(services);

        return services;
    }

    private static void AddPersistence(
        IServiceCollection services,
        string connectionString)
    {
        services.AddScoped<DomainEventDispatchInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
            options.AddInterceptors(
                sp.GetRequiredService<DomainEventDispatchInterceptor>());
        });

        services.AddDbContextFactory<AppDbContext>(
            options => options.UseSqlServer(connectionString),
            ServiceLifetime.Scoped);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IShippingAddressRepository, ShippingAddressRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryReservationRepository, InventoryReservationRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IDeliveryAgentRepository, DeliveryAgentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IShippingProviderConfigRepository, ShippingProviderConfigRepository>();
        services.AddScoped<IProcessedWebhookEventRepository, ProcessedWebhookEventRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
    }

    private static void AddDomainServices(IServiceCollection services)
    {
        // ITrackingNumberGenerator is declared in Domain.Services but implemented here — the
        // one Domain-declared interface Infrastructure fulfills directly, since it's a pure
        // "generate me a unique string" concern with no Application-level orchestration to add.
        services.AddScoped<ITrackingNumberGenerator, TrackingNumberGenerator>();

        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IInventoryReservationPolicy, InventoryReservationPolicy>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
    }

    private static void AddShippingProviders(IServiceCollection services)
    {
        // FR-11.2 — every concrete provider is registered here; IShippingProviderResolver
        // (below) picks the active one at runtime via ShippingProviderConfig. DHL/Aramex/
        // FedEx are intentionally honest stubs (see their own doc comments) — registering
        // them now, even unimplemented, means ShippingProviderConfig rows can reference
        // them and IShippingProviderResolver.Resolve(...) never throws "not registered" for
        // a name an admin might reasonably configure.
        services.AddScoped<IShippingProvider, LocalShippingProvider>();
        services.AddScoped<IShippingProvider, DHLShippingProvider>();
        services.AddScoped<IShippingProvider, AramexShippingProvider>();
        services.AddScoped<IShippingProvider, FedExShippingProvider>();

        services.AddScoped<IShippingProviderResolver, ShippingProviderResolver>();
    }

    private static void AddPaymentsAndNotifications(IServiceCollection services)
    {
        services.AddScoped<IPaymentGatewayService, SimulatedPaymentGatewayService>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
    }

    private static void AddAuth(IServiceCollection services)
    {
        // FR-1.1 to FR-1.4. PasswordHasher and JwtTokenGenerator are stateless/pure, so
        // Singleton is safe and avoids a per-request allocation; RefreshTokenStore takes the
        // ambient AppDbContext, so it must be Scoped to match that context's lifetime.
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
    }

    private static void AddBackgroundJobs(IServiceCollection services)
    {
        // FR-5.6 — the only Hosted/BackgroundService in the solution so far.
        services.AddHostedService<InventoryReservationExpiryJob>();
    }

    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException(
                "Missing 'ConnectionStrings:Redis'. Add it to appsettings.json or user secrets.");

        // NFR Caching. AddStackExchangeRedisCache registers IDistributedCache backed by
        // Redis — RedisCacheService (Infrastructure.Caching) is the only class in the
        // solution that ever resolves IDistributedCache directly; everything else depends
        // on ICacheService.
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "shippingsystem:";
        });

        services.AddScoped<ICacheService, RedisCacheService>();
    }

    private static void AddRealtime(IServiceCollection services)
    {
        // SRS §5 — the SignalR Hub for real-time shipment tracking. TrackingHub itself is
        // mapped to a route by the Api project's Program.cs (`app.MapHub<TrackingHub>(...)`,
        // referencing this Infrastructure type directly), but AddSignalR() — which registers
        // IHubContext<TrackingHub> for SignalRShipmentTrackingNotifier to consume — belongs
        // here, alongside every other "wire this Application interface to something real"
        // registration.
        services.AddSignalR();
        services.AddScoped<IShipmentTrackingNotifier, SignalRShipmentTrackingNotifier>();
    }

    private static void AddHealthChecks(IServiceCollection services)
    {
        // "ready" tags the two checks Api's /health/ready endpoint actually runs (dependency
        // checks); /health/live runs neither (see Program.cs) — a pod whose database or
        // Redis is temporarily down is not-ready, not dead, and an orchestrator should stop
        // routing traffic to it rather than restart the process.
        services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>("database", tags: new[] { "ready" })
            .AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready" });
    }
}
