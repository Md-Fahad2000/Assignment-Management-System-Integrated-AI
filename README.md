# Assignment Management System with Integrated AI

**Student:** Muhammad Fahad

A web-based system to help students manage daily routines, assignments, deadlines, and priorities, with **AI-powered scheduling** and assignment roadmaps.

## Tech Stack

- **Frontend:** React.js
- **Backend:** C# ASP.NET Core Web API
- **Database:** MySQL
- **Version control:** GitHub

## Features

- Student authentication (register / login) with **email verification** (6-digit code) and **forgot password** (email reset code)
- **Profile edit** — update name, email, and password (Week 3)
- Daily routine management
- Assignment and deadline management
- **Assignment detail full page** — view details and roadmap on a dedicated page; **AI responses saved** (summary, key points, quiz, etc.) (Week 3)
- AI smart scheduling
- AI assignment roadmap
- Deadline reminders
- Productivity tracking
- **Mental health & stress** — rule-based stress from **routine** (free hours/day) and **assignments due per week** (2 h/day per assignment; bands: 1 Low → 2 Mild → 3 High → 4+ Extreme), with gauge on a separate page (Week 3)
- **Assignment AI Assistant** — compact header and larger chat area; clearer message layout and reply formatting (Week 4)

## Project Structure

```
 frontend/          # React app
 backend/           # ASP.NET Web API
  database/          # MySQL schema and scripts
 README.md
```

## Week 1 & 2 Deliverables

- **Week 1:** React UI (Login, Register, Dashboard, Assignments, Routine), MySQL schema.
- **Week 2:** Backend auth (BCrypt + JWT), Assignment and Routine CRUD with MySQL, **AI** scheduling (Earliest-Deadline-First), priority logic, assignment roadmap. Frontend connected to API; protected routes.
- **AI roadmap:** Each step has a **date and time** (e.g. “Step 1: Read chapter — 2025-03-02 at 09:00”). Optionally set `OpenAI:ApiKey` in `appsettings.json` to use GPT for “how to do this assignment” steps; otherwise the app splits your uploaded document and assigns date and time from your routine. Run `database/` for time support.

## Week 3 Deliverables 

- **Assignment detail full page:** “View details & roadmap” opens a **full page** (`/assignments/:id`) instead of a modal. Same AI tools, roadmap, and step actions (explain, reschedule) on one page.
- **AI responses saved:** All AI tool results (Summarize, Key points, Key dates, Practice quiz, One-line summary, Best day, Suggested slots, Suggest title) are **saved** in the database and **loaded** when you open the assignment detail. Table: `assignment_ai_responses` (run `database/08_assignment_ai_responses.sql`).
- **Mental health & stress:** New **Mental health** page (`/mental-health`) with **stress from routine + workload**: average daily free time = 24 − routine busy hours; each pending assignment due in a week counts toward that week; **2 hours/day per assignment** vs free time can raise the level; bands 1→Low, 2→Mild, 3→High, 4+→Extreme; semi-circular meters; Refresh button.
- **Profile edit:** New **Profile** page (`/profile`) to edit full name, email, and password. API: `GET /api/auth/me`, `PUT /api/auth/profile`. Sidebar and session update after save.

## List of work done (Week 3)

| # | Task | Details |
|---|------|--------|
| 1 | Assignment detail full page | Replaced modal with full page at `/assignments/:id`; back to list; same AI tools and roadmap. |
| 2 | AI responses save/load | Every AI button (Summarize, Key points, Key dates, Quiz, One-line, Best day, Slots, Suggest title) saves its response to the DB; responses load when the detail page opens. |
| 3 | Mental health page | Separate page at `/mental-health`; sidebar link “Mental health”. |
| 4 | Stress (rule-based) | Routine + assignments due per week; 2 h/day per assignment vs free time; previous/current/next week counts. |
| 5 | Stress meters | Semi-circular gauge (Fear & Greed style) showing three values; color-coded with needle. |
| 6 | Profile edit page | Separate page at `/profile` to edit full name, email, and new password (optional); sidebar link “Profile”. |
| 7 | Profile API | Backend: `GET /auth/me`, `PUT /auth/profile`; `updateUser()` in AuthContext. |

## Week 4 Deliverables 

- **Chatbot UI improved:** Assignment AI Assistant page (`AssignmentAssistant`) — header, breadcrumbs, info banner, and assignment overview use **less vertical space**; the **chat area** uses more of the screen (full-width layout offset, flex layout, compact quick prompts and input bar). Styles in `frontend/src/pages/AssignmentAssistant.css`.
- **AI responses improved:** Assistant replies show **clearer structure** (paragraphs, spacing, lists) via `assistant-reply-content` styling; message bubbles and typography tuned for readability.

## List of work done (Week 4)

| # | Task | Details |
|---|------|--------|
| 1 | Chatbot layout | Larger chat region; compact chrome (header, banner, overview scroll/cap). |
| 2 | Message & reply UX | Wider assistant bubbles where appropriate; improved line height and formatted reply content. |
| 3 | Quick prompts & input | Smaller pill prompts and input row so more space stays for the conversation. |

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

### Email verification & password reset

1. Run the migration: `database/` (adds `email_verified`, `pending_registrations`, `password_reset_tokens`).
2. Configure **SMTP** in `backend/AssignmentManagement.Api/appsettings.json` under `Smtp`:
   - `Host`, `Port` (e.g. 587), `EnableSsl`, `User`, `Password`, `FromEmail`, `FromName`.
   - For Gmail, use an [App Password](https://support.google.com/accounts/answer/185833) and `smtp.gmail.com`.
3. **Registration flow:** user fills the form → receives a 6-digit code by email → enters it on **Verify email** (`/verify-email`) → account is created and they are logged in.
4. **Development without SMTP:** if `Smtp:Host` is empty, the API does not send emails; in **Development** mode the verification code is returned in the register response (`devVerificationCode`) for testing.
5. **Forgot password:** `/forgot-password` → email with code → `/reset-password` with email, code, and new password.



