using System.Threading.RateLimiting;
using ECommerce.Api.HealthChecks;
using ECommerce.Api.Middleware;
using ECommerce.Application;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register application and infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Configure JWT Bearer authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Keep original JWT claim names (sub, email, role) instead of mapping them
    // to long ClaimTypes URIs; ICurrentUserService looks them up by name.
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings!.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register HttpContextAccessor for ICurrentUserService
builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------------------------
// Rate limiting (configurable via the "RateLimiting" configuration section).
// Sensitive endpoints (auth, review creation) get stricter per-IP fixed
// windows on top of the global sliding window.
// ---------------------------------------------------------------------------
var globalLimit = builder.Configuration.GetValue("RateLimiting:Global:RequestsPerMinute", 600);
var authLimit = builder.Configuration.GetValue("RateLimiting:Auth:RequestsPerMinute", 10);
var reviewLimit = builder.Configuration.GetValue("RateLimiting:Reviews:RequestsPerMinute", 20);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.io/429",
            title = "Too many requests",
            status = StatusCodes.Status429TooManyRequests,
            detail = "You have exceeded the allowed number of requests. Please try again later.",
            instance = context.HttpContext.Request.Path.Value
        }, cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = globalLimit,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = authLimit;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("reviews", opt =>
    {
        opt.PermitLimit = reviewLimit;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

// ---------------------------------------------------------------------------
// Health checks: liveness (/health) vs readiness (/health/ready).
// Readiness distinguishes "process is alive" from "can serve traffic" by
// verifying SQL Server and Redis dependencies.
// ---------------------------------------------------------------------------
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running."), tags: new[] { "liveness" })
    .AddCheck<SqlServerHealthCheck>("sqlserver", tags: new[] { "ready", "db" })
    .AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready", "cache" });

var app = builder.Build();

// ---------------------------------------------------------------------------
// Development convenience: apply migrations and seed demo data so the API can
// be exercised immediately. Never runs in production and failures are logged,
// not fatal.
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    await app.InitializeDatabaseAsync();
}

// Configure the HTTP request pipeline.
// Correlation ids run first so every downstream log entry (including error
// logs emitted by the exception handler) carries the same identifier.
app.UseCorrelationId();

// Global exception handling must run early so it can catch errors from all
// downstream middleware and endpoints.
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Minimal Swagger UI served without extra package dependencies. The UI
    // loads the OpenAPI document produced by MapOpenApi (/openapi/v1.json).
    app.MapGet("/swagger", () => Results.Content(
        """
        <!DOCTYPE html>
        <html>
        <head>
            <title>ECommerce API — Swagger UI</title>
            <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
        </head>
        <body>
            <div id="swagger-ui"></div>
            <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
            <script>
                window.onload = () => {
                    window.ui = SwaggerUIBundle({
                        url: '/openapi/v1.json',
                        dom_id: '#swagger-ui',
                        deepLinking: true,
                        presets: [SwaggerUIBundle.presets.apis]
                    });
                };
            </script>
        </body>
        </html>
        """,
        "text/html"));
}

app.UseHttpsRedirection();

// Serve uploaded files
var uploadsPath = Path.Combine(AppContext.BaseDirectory, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health endpoints: liveness only checks the process; readiness also checks
// SQL Server and Redis so orchestrators know when the API can serve traffic.
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("liveness")
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.Run();

// Required for WebApplicationFactory access in integration tests.
public partial class Program;
