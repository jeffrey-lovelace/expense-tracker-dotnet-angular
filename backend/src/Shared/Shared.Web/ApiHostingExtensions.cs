using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Shared.Web;

/// <summary>
/// The hosting setup both APIs share, so the middleware order, CORS policy, error handling and Swagger cannot drift
/// apart between them. Business code never depends on this project.
/// </summary>
public static class ApiHostingExtensions
{
    /// <summary>The only CORS policy: the Angular app.</summary>
    public const string FrontendCorsPolicy = "frontend";

    public static IServiceCollection AddApiDefaults(this IServiceCollection services, string title)
    {
        services.AddControllers(options =>
            // Field rules live in the Domain (AGENTS.md 10.10). Without this, ASP.NET would add its own [Required]
            // check for non-nullable properties, with a different message and a PascalCase key ("Description").
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddFrontendCors();
        services.AddSwaggerWithJwt(title);
        return services;
    }

    /// <summary>
    /// The pipeline, in this order: exception handler, HTTPS (outside Development), CORS, authentication,
    /// authorization, controllers. CORS runs before authentication so even a 401 carries CORS headers and the
    /// browser lets the app read it.
    /// </summary>
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        // Empty error responses (401 from the JWT handler, 404 for unknown routes) get a ProblemDetails body.
        app.UseStatusCodePages();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors(FrontendCorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }

    // Origins come from "Cors:AllowedOrigins" (appsettings.json: http://localhost:4200). Read when the policy is first
    // used, so tests and environment variables can override them. A wildcard is refused: no AllowAnyOrigin.
    private static void AddFrontendCors(this IServiceCollection services)
    {
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (origins.Contains("*", StringComparer.Ordinal))
            {
                throw new InvalidOperationException("Cors:AllowedOrigins must list origins explicitly, not '*'.");
            }

            options.AddPolicy(FrontendCorsPolicy, policy => policy
                .WithOrigins(origins)
                .WithMethods("GET", "POST", "PUT", "DELETE")
                .WithHeaders("Authorization", "Content-Type")
                .WithExposedHeaders("Location")
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
        });
    }

    // Swagger UI (Development only) with an "Authorize" button that sends "Authorization: Bearer <token>".
    private static void AddSwaggerWithJwt(this IServiceCollection services, string title)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = title, Version = "v1" });

            var bearer = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the token from POST /api/auth/login (without the \"Bearer \" prefix).",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            };
            options.AddSecurityDefinition("Bearer", bearer);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearer] = [] });
        });
    }
}
