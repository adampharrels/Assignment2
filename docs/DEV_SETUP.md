# Developer Setup

## Do I need Windows or a VM?

**No, not for day-to-day work.** The app is Blazor Server on .NET 9, which runs natively on macOS, Windows and Linux.
Windows only matters at the end, because the tutor compiles the project in **Visual Studio 2022 on Windows**.
That's covered by:

1. **CI**: every push builds and tests on `windows-latest` with the .NET 9 SDK, and also builds the
   *exact submission zip* from a clean unzip on Windows.
2. **One manual check on Wed 14 Oct**: open the CI-built zip in VS 2022 on a lab PC (or any Windows machine)
   and press F5.

If you want your own Windows box anyway (optional): on an **Intel Mac**, install VMware Fusion (free for
personal use), VirtualBox or Parallels, then a Windows 11 x64 evaluation image from Microsoft, plus
VS 2022 Community with the "ASP.NET and web development" workload. On an **Apple Silicon Mac** use
Parallels or UTM with Windows 11 ARM (VS 2022 runs on ARM64).

## macOS

```bash
# .NET 9 SDK (matches the lab). Having .NET 10 installed as well is fine.
brew install --cask dotnet-sdk@9      # or download from https://dotnet.microsoft.com/download/dotnet/9.0

git clone https://github.com/adampharrels/Assignment2.git
cd Assignment2
dotnet build
dotnet test
dotnet run --project src/JobTracker.Web    # then open http://localhost:5080
```

Editor: **VS Code** + the "C# Dev Kit" extension, or **JetBrains Rider** (free for students).
For hot reload while editing pages, use `dotnet watch --project src/JobTracker.Web`.

> **You need the .NET 9 runtime, not just .NET 10.** With only .NET 10 installed, the build and the
> tests still work (`RollForward=Major`), but the web app runs on the 10 runtime, where
> `_framework/blazor.web.js` returns 404. Pages load, but **nothing is interactive**: buttons, tabs,
> dialogs and charts do nothing. Check with `dotnet --list-runtimes`: you need a
> `Microsoft.AspNetCore.App 9.0.x` line. Install it with `brew install --cask dotnet-sdk@9` or from
> https://dotnet.microsoft.com/download/dotnet/9.0. Having .NET 10 as well is fine.
>
> Also **don't** change `TargetFramework`, and don't add packages that only support net10.0
> (CI on the .NET 9 SDK will catch it).

## Windows

Visual Studio 2022 (17.12 or later) with the **ASP.NET and web development** workload.
Open `JobTracker.sln`, set **JobTracker.Web** as the startup project, and press F5.

## Database

- Not set up yet; that comes after kickoff. The plan is a SQLite file (e.g. `jobtracker.db`, already git-ignored)
  that is created and seeded on first run.
- If we use `EnsureCreated`: **after pulling a model or seed-data change, delete the `.db` file** and run again.
- To inspect it: the "SQLite Viewer" VS Code extension, or DB Browser for SQLite.

## Packages: don't upgrade these
| Package | Version | Why pinned |
|---|---|---|
| Microsoft.EntityFrameworkCore.Sqlite | 9.0.x | EF Core 10 requires net10 |
| MudBlazor | 8.15.0 | Built for .NET 8/9; v9 docs and APIs differ |
| Blazor-ApexCharts | 6.1.0 | |
| NUnit / NUnit3TestAdapter / Test SDK | 4.4 / 5.2 / 17.14 | Known to work in VS 2022 Test Explorer |

## Useful docs
- MudBlazor 8 components: https://mudblazor.com/components/list
- Blazor-ApexCharts examples: https://apexcharts.github.io/Blazor-ApexCharts/
- EF Core: https://learn.microsoft.com/ef/core/
