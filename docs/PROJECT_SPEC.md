# Grad Job Tracker: Project Specification

**Team:** Adam (lead, analytics/ML), Sarah, Grace
**Subject:** 31927 / 32998 .NET Applications Development, Assignment 2
**Due:** Fri 16 Oct 2026, 11:59pm (+3 days if we need extension)

> **Status: draft proposal for the kickoff meeting.** Sections 1–2 describe the idea and stack.
> Everything from section 3 onwards (architecture, data model, screens, analytics) is a *starting point*
> for the team to change, add to or replace. Class and method names are suggestions; none of this code exists yet.

---

## 1. Problem and motivation

Graduates send out dozens of applications per hiring season. Most people track them in a spreadsheet,
which can only tell you *what* happened. It can't tell you *why* some applications turn into interviews
and others don't.

Grad Job Tracker stores every application, its job description, stages and interviews. It then
**analyses** that history to answer the questions a job seeker actually has:

- Which of my applications are turning into interviews, and at what point do I drop out?
- Do React roles convert better than Python roles for me, and is that difference real or just noise?
- Do referrals, tailored resumes or cover letters change my odds?
- Which skills do employers keep asking for that I'm missing?
- How well does my resume match this specific job, and how likely is it to get me an interview?

**Positioning:** this is an analytics app that happens to have a tracker, not a job database with
a chart added on. The analytics and ML layer is the main feature, and every other screen exists to
feed it clean data.

## 2. Tech stack (and why)

| Choice | Reason |
|---|---|
| **.NET 9 / C# 13** | Lab requirement (Visual Studio 2022 + .NET 9). Do **not** move to net10. |
| **Blazor Server** (interactive server render mode) | Runs on Mac *and* Windows, opens in VS 2022 on the lab PCs, and earns the +2 "not WinForms" bonus. WinForms/WPF don't run on macOS. |
| **MudBlazor 8** | Ready-made Material components (data grid, dialogs, drag-and-drop, date pickers, sliders, chips, snackbars) let us cover the UI-element categories quickly. |
| **Blazor-ApexCharts** | Interactive charts (bar, line, funnel, heat map) with tooltips and click events. |
| **EF Core 9 + SQLite** | Entity Framework bonus. A file database needs no server install on lab machines and can be recreated and seeded on first run. |
| **NUnit 4** | Required test framework. |
| **Hand-written ML** (TF-IDF, logistic regression, naive Bayes, cross-validation) | Earns the "own ML algorithm" bonus. No LLM APIs, which don't count. |
| **GitHub Actions** | CI on Linux + Windows with the .NET 9 SDK, plus a packaged submission zip that's checked to compile from a clean unzip. |

## 3. Architecture (proposed)

The four projects already exist but are empty. What goes in them is up to us:

```
JobTracker.sln
├─ src/JobTracker.Core        Entities, enums, interfaces, analytics result records, extension methods. No dependencies.
├─ src/JobTracker.Data        EF Core DbContext (SQLite), seed data, CRUD services.           → Core
├─ src/JobTracker.Analytics   Statistics, TF-IDF, skill extraction, classifiers, insights.    → Core
├─ src/JobTracker.Web         Blazor Server UI (pages + components), DI wiring.               → Core, Data, Analytics
└─ tests/JobTracker.Tests     NUnit tests for Core, Data and Analytics.
```

Ideas to discuss:
- **Low coupling:** pages talk to services through interfaces (e.g. an application service, an analytics
  service), so a page never depends on how the data is stored. Analytics gets its data through those
  interfaces instead of querying the database itself.
- **Cross-screen communication:** a small shared service with C# events. For example, when a card moves
  on the Board, the Dashboard is told to refresh. This also covers the "delegates" requirement.
- **DbContext lifetime in Blazor Server:** create a short-lived DbContext per operation
  (look up `IDbContextFactory`) rather than keeping one open for the whole session.
- **Schema changes:** `EnsureCreated` instead of migrations is simpler with three people editing the model.
  The cost is deleting your local database file whenever the model changes.

## 4. Data model (proposed)

| Entity | Key fields |
|---|---|
| `Company` | Name (unique), Industry, Size (enum), Website, Notes |
| `JobApplication` | Company, RoleTitle, Location, WorkMode, Source, Status, SalaryMin/Max, JobDescription, JobUrl, DatePosted, DateApplied, HadReferral, TailoredResume, WroteCoverLetter, Notes |
| `StageEvent` | Application, FromStatus, ToStatus, ChangedAt, Note (written on **every** status change) |
| `Interview` | Application, Type, ScheduledAt, DurationMinutes, LocationOrLink, Interviewer, PrepNotes, Outcome |
| `Skill` | Name (canonical, unique), Category, InMyProfile |
| `ApplicationSkill` | Application ↔ Skill, ExtractedAutomatically, Weight (TF-IDF) |
| `UserProfile` | FullName, TargetRole, ResumeText |

**Worth agreeing on:** analytics should look at the *furthest stage reached* (from the stage history),
not the current status. An application that was rejected *after* an interview still counts as "got an interview".

### Status rules (proposed)

Pipeline: `Wishlist → Applied → Assessment → Interview → FinalRound → Offer → Accepted`

- Pipeline moves go **forward only**, and skipping stages is allowed (Applied → Interview).
- Any non-terminal application can exit to `Rejected`, `Withdrawn` or `Ghosted`.
- `Ghosted` can be revived to a later pipeline stage (they replied late) or to `Rejected`.
- `Accepted`, `Rejected` and `Withdrawn` are terminal.
- Moving to `Applied` sets `DateApplied` if it's empty. Every move writes a `StageEvent`.
- Illegal moves throw `InvalidOperationException`, which the UI shows in a snackbar.

## 5. Screens (proposed)

| # | Screen | Route | Owner | Purpose |
|---|---|---|---|---|
| 1 | Dashboard | `/` | Grace | KPI cards, mini funnel, top insights, upcoming interviews, recent activity |
| 2 | Board (Kanban) | `/board` | Sarah | Drag-and-drop pipeline, context menu, illegal-move feedback |
| 3 | Applications list | `/applications` | Sarah | Search/filter/sort grid, add/edit/delete dialogs |
| 4 | Application details | `/applications/{id}` | Sarah (+ Adam's panel) | Details, JD with skill highlights, stage timeline, interviews; **Skills & Fit panel** (Adam) |
| 5 | **Analytics** | `/analytics` | **Adam** | The main screen: see §6 |
| 6 | Interview calendar | `/calendar` | Grace | Month grid, add/edit interviews, outcomes, .ics export |
| 7 | Companies | `/companies` | Grace | Company CRUD + per-company stats |
| 8 | Profile & Data | `/settings` | Grace | Resume text, my skills, CSV/JSON import & export, reset demo data |

The spec requires at least 4 distinct screens. We have 8; screens 1–6 are the ones to show in the demo.

**UI element categories (spec requires ≥ 6):** buttons, headings, data grid/tables, dropdowns/selects,
autocomplete, modal dialogs, drag-and-drop board, context menu, sliders (salary filter), chips/tags,
date/time pickers, interactive charts (bar, line, funnel, heat map), progress/gauge, timeline,
snackbars/toasts, file upload, tabs, dark-mode toggle.

## 6. Analytics and ML (proposed, Adam)

All analytics are behind `IAnalyticsService`. Each rate is shown with a 95% Wilson confidence interval,
and groups with fewer than 5 applications are flagged. With 30–60 applications, "React 38% vs Python 21%"
can easily be noise, and showing that honestly is part of the feature.

### 6.1 Descriptive statistics (A1)
- **Conversion funnel:** Applied → Response → Interview → Final round → Offer, with stage-to-stage and overall conversion.
- **Interview rate by group**, with CI and *lift* vs baseline: skill, source (LinkedIn/Seek/Referral/…),
  location, work mode, salary band, company size, and tactics (referral, tailored resume, cover letter).
- **Timing:** median days to first response, ghosting rate, applications vs interviews per week,
  and "days after posting" vs response rate (does applying early help?).
- Implemented with one generic, delegate-driven helper:
  `RateBy<T>(items, Func<T, IEnumerable<string>> keySelector, Func<T, bool> isSuccess)`.

### 6.2 Text mining (A2, A3)
- **Tokenizer** that keeps tech tokens intact (`c#`, `.net`, `node.js`, `ci/cd`), removes stop words, and uses `yield return`.
- **TF-IDF** written by hand: smoothed IDF, L2-normalised vectors.
- **Skill extraction:** a curated dictionary (~150 skills, with synonyms like `js→javascript` and `k8s→kubernetes`,
  plus bigrams like "machine learning"), weighted by TF-IDF. It sits behind `ISkillExtractor`, so a second
  extractor can be swapped in (polymorphism).
- **Skill demand:** share of job descriptions mentioning each skill, plus its average TF-IDF weight.
- **Skill gap:** skills you don't have, ranked by demand × interview lift. The output is "learn this next".
- **Resume fit:** cosine similarity between your resume's TF-IDF vector and each JD's vector.

### 6.3 Predictive model (A4)
- **Target:** did the application reach the Interview stage?
- **Features:** referral, tailored resume, cover letter, resume-fit score, salary z-score, source (one-hot),
  work mode (one-hot), top-k skill indicators.
- **Models:** `LogisticRegressionClassifier` (batch gradient descent + L2) and `NaiveBayesClassifier`
  (Bernoulli, Laplace smoothing), both implementing `IClassifier`.
- **Evaluation:** stratified k-fold cross-validation (leave-one-out if n is small). Reports accuracy,
  precision, recall and log-loss, compared against a majority-class baseline.
- **Explainability:** per-application probability plus the top contributing features (weight × value).
- **What-if predictor:** paste a new JD and get the predicted interview chance, fit score, and matched/missing skills.

### 6.4 Insight engine (A6)
`InsightGenerator` is an abstract base (template method) with subclasses for source, skills, tactics,
ghosting/timing and skill gap. Each generator returns plain-English `Insight`s, e.g.
*"Referral applications reached interview 3.1× as often as average (n = 7)"*, ranked by importance.
Generators stay silent when the data is too small to support a claim.

### 6.5 Stretch (only if A1–A7 are done)
- k-means clustering of JDs into role families (Frontend / Data / Backend …).
- Interview pass rate by interview type.

## 7. Validation and error handling (all owners)
- Forms use MudForm validation: required fields, salary min ≤ max, valid URL, applied date not in the
  future, interview duration 5–600 min, unique company name.
- Services validate too, because the UI isn't the only caller (import uses them).
- Imports never crash: bad rows are skipped and listed in an error report.
- Every page has an empty state, and none of them crash when there's no data.
- Async calls in pages are wrapped in try/catch, with errors shown in a snackbar.

## 8. Marking-criteria map

Ideas for where each requirement could be met. Update this as we build.

| Criterion | Where we meet it |
|---|---|
| ≥ 4 resizable/responsive screens | 8 Blazor pages; MudBlazor grid is responsive by default |
| ≥ 6 UI element categories | See §5 list |
| Polymorphism with a real purpose | `IClassifier` (LogReg vs NB compared by CV); `InsightGenerator` subclasses; `ApplicationFileFormat` (CSV vs JSON); method overloads in analytics |
| ≥ 2 interfaces | `IApplicationService`, `IAnalyticsService`, `ISkillExtractor`, `IClassifier`, … |
| NUnit tests | `tests/JobTracker.Tests` (each person tests their own slices) |
| Anonymous method + LINQ + lambda | Analytics group-bys, filters in `SearchAsync`, insight generators |
| Generics | `RateBy<T>`, `IDbContextFactory<T>`, `List<GroupRate>`, `ApexChart<TItem>` |
| Collections / delegates / events | Cross-screen change events; key-selector delegates in analytics group-bys |
| Enums / properties / extension methods | `ApplicationStatus` etc.; `ApplicationExtensions`, `EnumExtensions` |
| File I/O + EF | SQLite via EF Core; CSV/JSON import/export; .ics export |
| Bonus: non-WinForms UI | Blazor Server |
| Bonus: EF / DB with LINQ / analytics / own ML | EF Core + LINQ; TF-IDF, logistic regression, naive Bayes, CV |

## 9. Out of scope
User accounts/login, multi-user sync, scraping job boards, LLM APIs, email integration.

## 10. Deliverables
1. Zip of the solution (CI builds it as `JobTracker-submission.zip`).
2. `README.md` with run instructions.
3. 1500–2000-word PDF report: idea, motivation, key features, usage, **each member's contribution**, references.
4. Lab demo: every member demos and explains their own slices.
