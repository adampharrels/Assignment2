using ApexCharts;
using JobTracker.Analytics;
using JobTracker.Analytics.Demo;
using JobTracker.Analytics.Text;
using JobTracker.Core.Interfaces;
using JobTracker.Data;
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
// TODO (K2, Sarah): replace the demo data with the application service once it implements IApplicationDataSource.
builder.Services.AddSingleton<IApplicationDataSource, DemoApplicationDataSource>();
builder.Services.AddSingleton<ISkillExtractor, DictionarySkillExtractor>();      // A3 (Adam)
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();            // A1-A5 (Adam)

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
