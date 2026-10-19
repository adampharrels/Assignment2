using ApexCharts;
using JobTracker.Analytics;
using JobTracker.Analytics.Text;
using JobTracker.Core.Interfaces;
using JobTracker.Data;
using JobTracker.Data.Profile;
using JobTracker.Data.Services;
using JobTracker.Web;
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
// One ApplicationService per circuit serves both the screens and the analytics, so every screen reads
// the same applications from the database.
builder.Services.AddScoped<ApplicationService>();                                // K2/S1 (Sarah)
builder.Services.AddScoped<IApplicationService>(services => services.GetRequiredService<ApplicationService>());
builder.Services.AddScoped<IApplicationDataSource>(services => services.GetRequiredService<ApplicationService>());
builder.Services.AddScoped<ICompanyService, CompanyService>();                 // K3/G4 (Grace)
builder.Services.AddSingleton<ISkillExtractor, DictionarySkillExtractor>();      // A3 (Adam)
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();            // A1-A5 (Adam)

builder.Services.AddScoped<IInterviewService, InterviewService>();           // G3 (Grace)
builder.Services.AddScoped<IDashboardApplicationSource, DashboardApplicationSource>();   // G2 (Grace), A7
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ImportExportService>();                               // G5 (Grace)
builder.Services.AddScoped(services => new DemoDataService(                     // G6 (Grace)
    services.GetRequiredService<IDbContextFactory<JobTrackerDbContext>>(),
    services.GetRequiredService<ResumeFileStore>(),
    includeDemoApplications: true));
builder.Services.AddSingleton(new ResumeFileStore(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "resume"))); // G6 (Grace)

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services.GetRequiredService<IDbContextFactory<JobTrackerDbContext>>(),
    includeDemoApplications: true);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Unknown addresses show the app's own "Page not found" page instead of a blank browser error.
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseAntiforgery();
app.MapStaticAssets();
app.MapResumeFile();                                                             // G6 (Grace)
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
