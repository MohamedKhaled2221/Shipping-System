using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ShippingSystem.Api.HealthChecks;
using ShippingSystem.Api.Middleware;
using ShippingSystem.Api.RateLimiting;
using ShippingSystem.Application;
using ShippingSystem.Infrastructure;
using ShippingSystem.Infrastructure.Options;
using ShippingSystem.Infrastructure.Persistence;
using ShippingSystem.Infrastructure.Persistence.Seeding;
using ShippingSystem.Infrastructure.Realtime;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Each layer's own composition root — Program.cs itself never registers a repository,
// handler, or validator directly, only calls the two extension methods that do.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product Shipping & Delivery Management API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token returned by /api/v1/auth/*/login — no 'Bearer ' prefix needed here."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Bound directly from configuration here (rather than via DI) because AddJwtBearer's
// options callback below needs the values immediately, before the service provider that
// would resolve IOptions<JwtOptions> even exists.
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddApiRateLimiting();

// .NET 8+ IExceptionHandler pipeline — see GlobalExceptionHandler for the actual mapping.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    await DataSeeder.SeedAsync(app.Services);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AngularClient");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SRS §5 — real-time shipment status push. TrackingHub is anonymous (see its own doc
// comment), matching /api/v1/tracking's [AllowAnonymous]: both surface the same public,
// tracking-number-scoped data through two transports.
app.MapHub<TrackingHub>("/hubs/tracking");

// Liveness: "is the process itself running." Predicate = _ => false runs ZERO registered
// checks — an orchestrator should never restart this pod just because SQL Server or Redis
// is temporarily unreachable; that's what readiness is for, below.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

// Readiness: "can this instance actually serve traffic right now." Runs every check tagged
// "ready" (SqlServerHealthCheck, RedisHealthCheck — see Infrastructure's DependencyInjection).
// An orchestrator should stop ROUTING to this pod on failure here, not restart it.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

app.Run();

// Exposed for WebApplicationFactory-based integration tests in a later module.
public partial class Program { }
