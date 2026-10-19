# Grad Job Tracker

> Work in progress. Update the screens and run steps before submission.

A graduate job-application tracker whose main feature is **analytics**. It tracks companies, applications,
interview stages, salaries, locations and job descriptions, then uses statistics, TF-IDF text mining and
hand-written machine-learning models to show what is (and isn't) getting you interviews.

31927 / 32998 .NET Applications Development, Assignment 2 (Spring 2026). Team: Adam, Sarah, Grace.

## How to run (Visual Studio 2022, Windows lab PC)

1. Unzip and open **`JobTracker.sln`** in Visual Studio 2022 (needs the .NET 9 SDK and the
   *ASP.NET and web development* workload).
2. In Solution Explorer, right-click **JobTracker.Web** and choose **Set as Startup Project**.
3. Press **F5**. NuGet packages restore automatically. The app opens in the browser at http://localhost:5080.
4. On first run a SQLite database (`jobtracker.db`) is created and filled with demo data.
   To reset it, close the app and delete `src/JobTracker.Web/jobtracker.db`.

Tests: **Test → Run All Tests** (NUnit, project `JobTracker.Tests`).

### Command line (Windows / macOS / Linux)
```bash
dotnet run --project src/JobTracker.Web
dotnet test
```

## Screens
| Screen | What it does |
|---|---|
| Dashboard | KPIs, funnel, top insights, upcoming interviews, recent activity |
| Board | Drag-and-drop Kanban pipeline |
| Applications | Search / filter / add / edit / delete applications |
| Application details | Description with highlighted skills, stage timeline, interviews, predicted interview chance |
| Analytics | Funnel, interview rates with confidence intervals, skill demand (TF-IDF), skill gap, model comparison, what-if predictor |
| Interview Calendar | Month view, schedule and record interview outcomes, .ics export |
| Companies | Company notes and per-company stats |
| Import & export | CSV/JSON application import and export |
| Profile | Resume upload, overview, skills and demo-data reset |

## Project structure
```
src/JobTracker.Core        domain model, enums, interfaces, extension methods
src/JobTracker.Data        EF Core (SQLite) context, seed data, services
src/JobTracker.Analytics   statistics, TF-IDF, skill extraction, logistic regression, naive Bayes, insights
src/JobTracker.Web         Blazor Server UI (MudBlazor + ApexCharts)
tests/JobTracker.Tests     NUnit tests
docs/                      project specification, user stories, task allocation
```
