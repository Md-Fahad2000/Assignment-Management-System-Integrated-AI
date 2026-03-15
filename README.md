# Assignment Management System with Integrated AI

**Student:** Muhammad Fahad

A web-based system to help students manage daily routines, assignments, deadlines, and priorities, with **AI-powered scheduling** and assignment roadmaps.

## Tech Stack

- **Frontend:** React.js
- **Backend:** C# ASP.NET Core Web API
- **Database:** MySQL
- **Version control:** GitHub

## Features

- Student authentication (register / login)
- **Profile edit** — update name, email, and password (Week 3)
- Daily routine management
- Assignment and deadline management
- **Assignment detail full page** — view details and roadmap on a dedicated page; **AI responses saved** (summary, key points, quiz, etc.) (Week 3)
- AI smart scheduling
- AI assignment roadmap
- Deadline reminders
- Productivity tracking
- **Mental health & stress** — AI-estimated stress (previous / current / future week) with gauge, on a separate page (Week 3)

## Project Structure

```
├── frontend/          # React app
├── backend/           # ASP.NET Web API
├── database/          # MySQL schema and scripts
└── README.md
```

## Week 1 & 2 Deliverables ✓

- **Week 1:** React UI (Login, Register, Dashboard, Assignments, Routine), MySQL schema.
- **Week 2:** Backend auth (BCrypt + JWT), Assignment and Routine CRUD with MySQL, **AI** scheduling (Earliest-Deadline-First), priority logic, assignment roadmap. Frontend connected to API; protected routes.
- **AI roadmap:** Each step has a **date and time** (e.g. “Step 1: Read chapter — 2025-03-02 at 09:00”). Optionally set `OpenAI:ApiKey` in `appsettings.json` to use GPT for “how to do this assignment” steps; otherwise the app splits your uploaded document and assigns date and time from your routine. Run `database/03_add_roadmap_suggested_time.sql` for time support.

## Week 3 Deliverables ✓

- **Assignment detail full page:** “View details & roadmap” opens a **full page** (`/assignments/:id`) instead of a modal. Same AI tools, roadmap, and step actions (explain, reschedule) on one page.
- **AI responses saved:** All AI tool results (Summarize, Key points, Key dates, Practice quiz, One-line summary, Best day, Suggested slots, Suggest title) are **saved** in the database and **loaded** when you open the assignment detail. Table: `assignment_ai_responses` (run `database/08_assignment_ai_responses.sql`).
- **Mental health & stress:** New **Mental health** page (`/mental-health`) with AI-estimated stress (previous week, current week, predicted future) from assignments and routine; semi-circular stress meters (gauge style); Refresh button. No manual logging.
- **Profile edit:** New **Profile** page (`/profile`) to edit full name, email, and password. API: `GET /api/auth/me`, `PUT /api/auth/profile`. Sidebar and session update after save.

## List of work done (Week 3)

| # | Task | Details |
|---|------|--------|
| 1 | Assignment detail full page | Replaced modal with full page at `/assignments/:id`; back to list; same AI tools and roadmap. |
| 2 | AI responses save/load | Every AI button (Summarize, Key points, Key dates, Quiz, One-line, Best day, Slots, Suggest title) saves its response to the DB; responses load when the detail page opens. |
| 3 | Mental health page | Separate page at `/mental-health`; sidebar link “Mental health”. |
| 4 | Stress AI-estimated | Student does not log manually; AI uses assignments and routine to estimate previous/current/future stress (1–5). |
| 5 | Stress meters | Semi-circular gauge (Fear & Greed style) showing three values; color-coded with needle. |
| 6 | Profile edit page | Separate page at `/profile` to edit full name, email, and new password (optional); sidebar link “Profile”. |
| 7 | Profile API | Backend: `GET /auth/me`, `PUT /auth/profile`; `updateUser()` in AuthContext. |

## Getting Started

### Prerequisites

- Node.js (for React)
- .NET SDK 10 (for backend)
- MySQL (for database)

### Run frontend

```bash
cd frontend
npm install
npm run dev
```

Open http://localhost:3000 and use Dashboard, Assignments, and Routine.

### Run backend

```bash
cd backend/AssignmentManagement.Api
dotnet run
```

API runs at http://localhost:5000; Swagger at http://localhost:5000/swagger.
