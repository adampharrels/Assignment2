# User Stories

Persona: **Jess**, a final-year student applying for graduate tech roles, 30–60 applications per season.
Each story lists its slice (see [TASKS.md](TASKS.md)) and acceptance criteria (AC).
Draft: add, change or drop stories at kickoff.
Priority: **M** = must have for the demo, **S** = should have, **C** = could have.

## Tracking (Sarah)

**US-01 (M, S1): Add an application.**
As Jess, I want to record a new application with company, role, location, salary, source and the job description, so that everything about it is in one place.
- AC: Company autocomplete lets me pick an existing company or type a new one, which gets created.
- AC: Required fields are role, company and location. Salary min must be ≤ max. URL must be valid. Applied date can't be in the future.
- AC: Invalid input shows field-level messages, and nothing is saved.
- AC: On save, skills are extracted from the description automatically (Adam's skill extraction).

**US-02 (M, S1): Edit or delete an application.**
As Jess, I want to fix mistakes or remove an application.
- AC: Edit reuses the add dialog, pre-filled.
- AC: Delete asks for confirmation and removes its interviews and history.

**US-03 (M, S2): Find applications.**
As Jess, I want to search and filter my applications, so I can find one quickly.
- AC: Free-text search across role, company and notes.
- AC: Filters for status, source, work mode, location, skill and minimum salary (slider). Filters combine.
- AC: Columns sort, and an empty result shows a friendly message.

**US-04 (M, S3): Move applications through the pipeline.**
As Jess, I want to drag an application card to its new stage, so updating progress takes one second.
- AC: One column per stage, with a card count in each header.
- AC: Dropping a card into an allowed stage saves it, records a stage event, and the Dashboard updates without a manual refresh.
- AC: Dropping into an illegal stage snaps the card back and shows the reason.
- AC: Right-click on a card gives Open / Edit / Reject / Withdraw / Delete.

**US-05 (M, S4): See everything about one application.**
As Jess, I want a detail page showing the description, history and interviews.
- AC: Stage history shows as a timeline with dates.
- AC: Recognised skills are highlighted in the job description.
- AC: Interviews for this application are listed, and I can add one (Grace's dialog).

## Interviews, companies, data (Grace)

**US-06 (M, G3): Schedule interviews.**
As Jess, I want to see my interviews on a calendar, so I don't double-book or forget one.
- AC: Month view with previous/next navigation, showing interviews on their days.
- AC: Clicking a day opens an add dialog with application, type, date/time, duration, link and prep notes.
- AC: Overlapping interviews are rejected with a message.
- AC: I can mark an outcome (Passed / Failed / Cancelled).

**US-07 (S, G3): Export interviews.**
As Jess, I want to export interviews to an `.ics` file, so they show up in my normal calendar.

**US-08 (M, G2): See where I stand at a glance.**
As Jess, I want a dashboard with my key numbers, so I know how the search is going.
- AC: KPI cards for total, submitted, response rate, interview rate, final rounds and offers.
- AC: Upcoming interviews (next 7 days) and the 5 most recent stage changes.
- AC: The top 3 insights from analytics, or an empty state if there aren't any yet.
- AC: Numbers refresh when I change something on another screen.

**US-09 (S, G4): Manage companies.**
As Jess, I want to keep notes on companies and see all my applications to each one.
- AC: Company name is unique. The website must be a valid URL.
- AC: Each company shows its application count and the best stage reached.

**US-10 (M, G5): Import and export my data.**
As Jess, I want to import my existing spreadsheet (CSV) and back up everything (JSON).
- AC: Import validates every row, imports the good ones, and lists errors with row numbers for the rest.
- AC: Export produces a file I can re-import (round trip).

**US-11 (M, G6): Describe myself.**
As Jess, I want to paste my resume and tick the skills I have, so fit scores and skill gaps are about *me*.

## Analytics and ML (Adam)

**US-12 (M, A1): See my funnel.**
As Jess, I want to see how many applications survive each stage, so I know where I'm losing out.
- AC: Funnel chart with counts and conversion % between stages.

**US-13 (M, A1): Compare what works.**
As Jess, I want interview rates broken down by skill, source, location and salary band, so I can focus on what converts.
- AC: Bar chart per breakdown with 95% confidence intervals.
- AC: Groups with fewer than 5 applications are visibly flagged as unreliable.
- AC: A tactics view compares referral / tailored resume / cover letter against applications without them.

**US-14 (M, A1): Understand timing.**
As Jess, I want to know how long companies take to reply, and how often I get ghosted.
- AC: Median days to response, ghosting rate, and a weekly applications-vs-interviews trend line.

**US-15 (M, A2/A3): Know what employers want.**
As Jess, I want to see which skills appear most in the job descriptions I apply to.
- AC: Top-20 skill chart computed with TF-IDF over all stored descriptions.
- AC: Skills I already have are visually distinguished.

**US-16 (M, A3): Know what to learn next.**
As Jess, I want a ranked list of in-demand skills I'm missing.
- AC: Ranked by demand × interview lift, with a short explanation for each.

**US-17 (M, A4/A6): Predict my chances.**
As Jess, I want to see how likely an application is to get an interview, and why.
- AC: The details page shows probability, resume fit score, matched/missing skills, and the top 3 factors.
- AC: The Analytics page shows both models' cross-validated accuracy against a baseline, so I know how far to trust it.

**US-18 (S, A6): What-if.**
As Jess, I want to paste a job description *before* applying and get a fit score and predicted chance.

**US-19 (M, A5): Get told what matters.**
As Jess, I want plain-English insights such as "Referral applications got 3× more interviews", so I don't have to read every chart.
- AC: Insights are ranked, every one states its sample size, and none are produced from tiny samples.
