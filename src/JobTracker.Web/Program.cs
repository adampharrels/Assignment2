using ApexCharts;
using JobTracker.Analytics;
using JobTracker.Analytics.Text;
using JobTracker.Core.Interfaces;
using JobTracker.Data;
using JobTracker.Data.Seed;
using JobTracker.Data.Services;
using JobTracker.Web.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddApexCharts();

// Database: a SQLite file created on first run. Services create a short-lived context per call.
string connectionString = builder.Configuration.GetConnectionString("JobTracker") ?? "Data Source=jobtracker.db";
builder.Services.AddDbContextFactory<JobTrackerDbContext>(options => options.UseSqlite(connectionString));

// Services (owner in brackets). Add your registration here, or ask Adam to.
builder.Services.AddScoped<IProfileService, ProfileService>();                 // K1 (Adam)
builder.Services.AddScoped<IApplicationService, ApplicationService>();         // K2/S1 (Sarah)
// TODO (K2, Sarah): ApplicationService also implements IApplicationDataSource. Swap the line below to it once
// the seed applications are saved to the database, so every screen reads the same data.
builder.Services.AddSingleton<IApplicationDataSource, SeedApplicationDataSource>(); // G1 (Grace)
builder.Services.AddSingleton<ISkillExtractor, DictionarySkillExtractor>();      // A3 (Adam)
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();            // A1-A5 (Adam)

// G2: replace the demo adapter when S1 provides totals, history and its creation route.
builder.Services.AddScoped<IDashboardApplicationSource, DemoDashboardApplicationSource>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ImportExportService>();                               // G5 (Grace)

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services.GetRequiredService<IDbContextFactory<JobTrackerDbContext>>());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
