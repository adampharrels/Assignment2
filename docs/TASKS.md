# Tasks, Slices and Ownership

> **Status: draft for the kickoff meeting.** The split and the ideas below are a starting point.
> Anyone can propose changes at kickoff, and class/file names are suggestions only.

**Issues:** every slice has its own issue, all listed in [#2 Tasks & slices](https://github.com/adampharrels/Assignment2/issues/2).

Each slice is a vertical piece of work (data + logic + UI + tests) owned by **one** person, in files
that **only that person edits**. That way nobody overwrites anyone else's work, and each person can
demo and explain their own code.

## 0. Done: project setup (Adam)

The repo only contains what a project template would generate:
- `JobTracker.sln` with 4 empty projects (Core, Data, Analytics, Web) + an NUnit test project.
- Packages installed: MudBlazor, Blazor-ApexCharts, EF Core SQLite, NUnit.
- Blazor app shell (app bar, side menu, one Home page) that builds and runs.
- GitHub Actions CI (build + test on Linux and Windows, packaged submission zip).

There are no entities, interfaces, pages or features yet. That's for the team to design.

## 1. Kickoff: agree on the contracts (whole team, decided by Wed 7 Oct)

Issue: [#3](https://github.com/adampharrels/Assignment2/issues/3)

Agree on these together, so everyone can then build in parallel:

1. **Screens:** which screens we build and who owns each. The proposed list is in the spec above.
2. **Data model:** entities and fields. The proposal is in [PROJECT_SPEC.md §4](PROJECT_SPEC.md).
3. **Interfaces between slices:** the methods each person's service offers to the others.
   Write down signatures only, not implementations.
4. **Status rules** for moving applications between stages.
5. **Analytics outputs:** what numbers and results the dashboard and details page need from Adam's analytics.

Then **each person writes the entity classes and interface for their own area**:

| Person | Writes | Issue |
|---|---|---|
| Adam | Skill-related entities, the analytics interface + result types, the EF `DbContext`, DI registrations in `Program.cs` | [#4](https://github.com/adampharrels/Assignment2/issues/4) |
| Sarah | Application + stage-history entities, enums for status/source/work mode, the application service interface | [#5](https://github.com/adampharrels/Assignment2/issues/5) |
| Grace | Company + interview entities, their service interfaces, the database seed-data class | [#6](https://github.com/adampharrels/Assignment2/issues/6) |

Adam merges these into one PR so `main` has the agreed contracts by Thu afternoon.

## 2. Slices

### Adam: analytics, ML, integration 

| Slice | What | Issue |
|---|---|---|
| **A1** Statistics | Conversion funnel, interview rates by skill/source/location/salary band with confidence intervals, timing (days to response, ghosting rate, weekly trend) | [#7](https://github.com/adampharrels/Assignment2/issues/7) |
| **A2** Text pipeline | Tokenizer, TF-IDF written by hand, cosine similarity, skill demand ranking | [#8](https://github.com/adampharrels/Assignment2/issues/8) |
| **A3** Skill extraction | Skill dictionary with synonyms, extracting skills from job descriptions, skill-gap ranking vs the user's profile | [#9](https://github.com/adampharrels/Assignment2/issues/9) |
| **A4** ML models | Feature building, logistic regression and naive Bayes written by hand, k-fold cross-validation, per-application prediction with explanations | [#10](https://github.com/adampharrels/Assignment2/issues/10) |
| **A5** Insights | Plain-English insights generated from the statistics | [#11](https://github.com/adampharrels/Assignment2/issues/11) |
| **A6** Analytics UI | Analytics page (charts) and the "skills & fit" panel shown on the application details page | [#12](https://github.com/adampharrels/Assignment2/issues/12) |
| **A7** Integration lead | Review and merge PRs, check the build in VS 2022 on Windows, final zip, assemble the report | [#13](https://github.com/adampharrels/Assignment2/issues/13) |

### Sarah: applications, board, details

| Slice | What | Issue |
|---|---|---|
| **S1** Application CRUD | Add/edit/delete applications, form with validation, recording stage history | [#14](https://github.com/adampharrels/Assignment2/issues/14) |
| **S2** Applications list | Table with search, filters (status, source, location, skill, salary slider) and sorting | [#15](https://github.com/adampharrels/Assignment2/issues/15) |
| **S3** Kanban board | Drag-and-drop stages, status-change rules, context menu, error message on illegal moves | [#16](https://github.com/adampharrels/Assignment2/issues/16) |
| **S4** Details page | Full details, job description, stage timeline, interviews for that application | [#17](https://github.com/adampharrels/Assignment2/issues/17) |

### Grace: dashboard, calendar, companies, data

| Slice | What | Issue |
|---|---|---|
| **G1** Seed data (**first: needed by Fri 9 Oct**) | 50+ realistic demo applications with varied skills, sources, locations, salaries, outcomes and job descriptions | [#18](https://github.com/adampharrels/Assignment2/issues/18) |
| **G2** Dashboard | KPI cards, upcoming interviews, recent activity, top insights | [#19](https://github.com/adampharrels/Assignment2/issues/19) |
| **G3** Interview calendar | Month view, add/edit interviews, overlap validation, outcomes, `.ics` export | [#20](https://github.com/adampharrels/Assignment2/issues/20) |
| **G4** Companies | Company CRUD + per-company stats | [#21](https://github.com/adampharrels/Assignment2/issues/21) |
| **G5** Import / export | CSV and JSON import/export with row validation and an error report | [#22](https://github.com/adampharrels/Assignment2/issues/22) |
| **G6** Profile | Resume text and "my skills", used for fit scoring and skill gap | [#23](https://github.com/adampharrels/Assignment2/issues/23) |

Everyone writes NUnit tests for their own slices.

## 3. Dependencies and conflicts

After kickoff, Sarah and Grace work in separate files that means you two can work in parallel.
These are the places where work touches:

| # | Conflict / dependency | Who waits on whom | Mitigation |
|---|---|---|---|
| C1 | Entities and `DbContext` | Everyone | Agreed at kickoff. Changes afterwards need a heads-up in the group chat, and Adam merges them. |
| C2 | `Program.cs` (DI) and `NavMenu.razor` | Everyone | Adam owns both. Ask him to add your service or page link (one line each). |
| C3 | Dashboard needs analytics results | Grace → Adam | Grace codes against the agreed interface with a **fake** class that returns sample numbers, then swaps it for the real one when Adam merges. |
| C4 | Details page shows Adam's skills & fit panel | Sarah ↔ Adam | The panel is its own component file. Sarah adds one tag to her page, and nobody edits the other's file. |
| C5 | "Add interview" from the details page | Sarah → Grace | Sarah shows interviews read-only first and adds the button once Grace's interview dialog is merged. |
| C6 | Import creates applications | Grace → Sarah | Import is scheduled late (Phase 2) and reuses Sarah's add-application method. |
| C7 | Skill extraction when an application is saved | Sarah → Adam | Agree the method at kickoff. Until it's ready, it does nothing. |
| C8 | ML needs realistic data | Adam → Grace | G1 is Grace's first task. Until it lands, Adam tests with small hand-made datasets in unit tests. |
| C9 | Test project | Everyone | Put each test file in the folder that mirrors the code it tests (e.g. tests for `src/JobTracker.Data/Services/` go in `tests/JobTracker.Tests/Data/Services/`), with a matching namespace. Never edit someone else's test file. |

**Waiting on someone? Use a fake.** Write a small class that implements the agreed interface and
returns hard-coded data. Your screen works today, and when the real service is merged you switch
one line in `Program.cs`.

## 4. Timeline (can be flexible)

| Dates | Adam | Sarah | Grace |
|---|---|---|---|
| **Tue 6** | Setup pushed | Clone, build, run, read the spec | Clone, build, run, read the spec |
| **Wed 7** | **Kickoff meeting** (all) | | |
| **Thu 8** | Contracts merged by afternoon | Own entities + interface | Own entities + interface |
| **Thu 8 – Sat 10** (Phase 1) | A1, A2, A3 | S1, S3 | **G1 (by Fri)**, G3, G4 |
| **Sun 11 – Mon 12** (Phase 2) | A4, A5, A6 | S2, S4 | G2, G5, G6 |
| **Tue 13** | Merge everything | Tests + polish | Tests + polish |
| **Tue 13, 11:59pm** | **Feature freeze**: bug fixes only after this | | |
| **Wed 14** | Test the CI-built zip on a Windows/VS 2022 machine | Bug bash + report section | Bug bash + report section |
| **Thu 15** | Report assembled → PDF; final zip | Rehearse demo | Rehearse demo |
| **Fri 16** | Team leader submits on Canvas (aim for midday) | | |

## 5. Workflow

1. One branch per slice, named `feat/<slice>-<short-name>`: `feat/a1-statistics`, `feat/s3-kanban-board`,
   `feat/g1-seed-data`. Bug fixes after merging use `fix/<short-name>`.
2. Commit small and often, push daily, and follow the [commit convention](#6-commit-convention).
3. Open a PR to `main`. **CI must be green** (it builds and tests on Windows too). One to two people can review and merge for fairness. Incase the other person is busy, I (Adam) will do it.
4. Pull `main` into your branch every morning: `git pull origin main`.
5. A slice is done when it works in the UI, validates input, handles empty and error states,
   has at least 1 NUnit test, and has comments on public members.
6. Keep a short log of what you built. You'll need it for your report section and the demo.

## 6. Commit convention

We use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <what changed, present tense, lower case>
```

| Type | Use for |
|---|---|
| `feat` | A new feature or screen |
| `fix` | A bug fix |
| `test` | Adding or changing tests only |
| `refactor` | Restructuring code without changing behaviour |
| `docs` | Docs, README or comments only |
| `style` | Formatting only (whitespace, indentation) |
| `chore` | Build, packages, CI, config |

**Scope** is the slice ID in lower case, e.g. `a1`, `s3`, `g1`, or `k` for kickoff/contracts.

Examples:
```
feat(s3): drag cards between board columns
fix(g3): reject interviews that overlap
test(a2): tf-idf weights on a toy corpus
docs(k): agreed data model
```

PR titles follow the same format. Put `Closes #<issue number>` in the PR description so the slice's
issue closes automatically when the PR is merged.
