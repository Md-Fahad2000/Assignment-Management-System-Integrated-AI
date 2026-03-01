# Assignment Management System with Integrated AI

**Student:** Muhammad Fahad 
**Supervisor:** Marie Keary  

A web-based system to help students manage daily routines, assignments, deadlines, and priorities, with **AI-powered scheduling** and assignment roadmaps.

## Tech Stack

- **Frontend:** React.js
- **Backend:** C# ASP.NET Core Web API
- **Database:** MySQL
- **Version control:** GitHub

## Features

- Student authentication (register/login)
- Daily routine management
- Assignment and deadline management
- AI smart scheduling
- AI assignment roadmap
- Deadline reminders
- Productivity tracking

## Project Structure

```
├── frontend/          # React app
├── backend/           # ASP.NET Web API
├── database/          # MySQL schema and scripts

└── README.md
```



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

Open http://localhost:3000 — use Dashboard, Assignments, and Routine .

### Run backend

```bash
cd backend/AssignmentManagement.Api
dotnet run
```

API runs at http://localhost:5000; Swagger at http://localhost:5000/swagger.

### Database

1. Create database and tables: run `database/01_schema.sql` in MySQL (e.g. `mysql -u root -p < database/01_schema.sql` or execute in MySQL Workbench).
2. Optional: run `database/02_seed.sql` for sample data (if present).

## License

Academic project — FYP.
