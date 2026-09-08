using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ShippingSystem.Application.Common.Behaviors;

namespace ShippingSystem.Application;

/// <summary>
/// Composition root for the Application layer. Called once from the API's Program.cs
/// as `builder.Services.AddApplication();` — the API project never registers MediatR
/// handlers or validators itself, keeping that wiring co-located with the code it wires.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // Order matters: validation must run before the request reaches the handler,
        // and should short-circuit before anything gets logged as "handled".
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
