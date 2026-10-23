using Expenses.Application;
using Expenses.Infrastructure.Persistence;
using Expenses.Infrastructure.Seeding;

using Shared.Auth;
using Shared.Web;

var builder = WebApplication.CreateBuilder(args);

// Fail at startup if a singleton captures a scoped service, or a registration is missing.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddApiDefaults("Expenses API");
builder.Services.AddJwtAuthentication(builder.Configuration);

// Composition root: the only place that knows the Infrastructure types.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ISqliteConnectionFactory>(services => new SqliteConnectionFactory(
    services.GetRequiredService<IConfiguration>().GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.")));
builder.Services.AddSingleton<SqliteMigrator>();
builder.Services.AddScoped<IExpenseRepository, SqliteExpenseRepository>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

await app.Services.GetRequiredService<SqliteMigrator>().MigrateAsync(CancellationToken.None);

// Demo data: on in Development (appsettings.Development.json), off elsewhere unless Seed__Enabled=true.
var seed = app.Configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();
if (seed.Enabled)
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(seed.Users, CancellationToken.None);
}

app.UseApiPipeline();

await app.RunAsync();

// Makes the implicit Program class visible to WebApplicationFactory<Program> in Expenses.Api.Tests.
public partial class Program;
