using Identity.Application;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Security;

using Shared.Auth;

var builder = WebApplication.CreateBuilder(args);

// Fail at startup if a singleton captures a scoped service, or a registration is missing.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddControllers(options =>
    // Field rules live in the Domain (AGENTS.md 10.10). Without this, ASP.NET would add its own [Required] check
    // for non-nullable properties, with a different message and a PascalCase key ("Password").
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddJwtAuthentication(builder.Configuration);

// Composition root: the only place that knows the Infrastructure types.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ISqliteConnectionFactory>(services => new SqliteConnectionFactory(
    services.GetRequiredService<IConfiguration>().GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.")));
builder.Services.AddSingleton<SqliteMigrator>();
builder.Services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<ITokenService, JwtUserTokenService>();
builder.Services.AddScoped<IUserRepository, SqliteUserRepository>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

await app.Services.GetRequiredService<SqliteMigrator>().MigrateAsync(CancellationToken.None);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Empty error responses (401 from the JWT handler, 404 for unknown routes) get a ProblemDetails body.
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

// Makes the implicit Program class visible to WebApplicationFactory<Program> in Identity.Api.Tests.
public partial class Program;
