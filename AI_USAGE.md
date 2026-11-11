# AI Usage & Collaboration Disclosure

This document details the usage of Artificial Intelligence tooling during the planning, implementation, and refinement of the School Admission System.

---

## 🤖 Tooling Used
- **Platform:** Google Antigravity Agentic Coding Assistant
- **Roles:** Senior .NET Backend Developer, Senior React Frontend Developer, Senior DevOps Engineer

---

## 🛠 Areas of AI Assistance

### 1. Backend Architecture & Concurrency Control
- **Prompting Strategy:** Provided strict business constraints regarding high-concurrency ticket/seat allocation, requiring that `(Active Held + Confirmed)` never exceeds `TotalSeats`.
- **AI Contribution:** Designed and verified the PostgreSQL row-level locking strategy (`SELECT ... FOR UPDATE`) executed inside explicit database transactions, coupled with EF Core 10 entity definitions and `xmin` concurrency tokens.
- **Human Verification:** Verified that `Program.cs` cleanly registered services, used built-in .NET 10 `Microsoft.AspNetCore.OpenApi` without incompatible Swagger packages, and handled `HOLD_DURATION_SECONDS` correctly.

### 2. Frontend Development & Real-Time UX
- **Prompting Strategy:** Specified React 18, Vite, Tailwind CSS, live polling interval (3–5 seconds), a reactive countdown timer with instant zero-second auto-expiration, and persistent `X-Student-Id` headers.
- **AI Contribution:** Scaffolded the component structure (`StudentIdentity`, `ClassManagement`, `ClassDashboard`, `HoldDetails`, `MyApplications`), wrote the custom `useCountdown` hook with second-by-second ticking, and implemented the API client with centralized error toast handling.
- **Human Verification:** Confirmed that `npm run build` completed cleanly with zero warnings or bundle errors.

### 3. Containerization & DevOps Pipeline
- **Prompting Strategy:** Outlined requirements for Docker multi-stage builds (`sdk:10.0` -> `aspnet:10.0`, `node:20-alpine` -> `nginx:alpine`), automatic EF Core runtime migrations at backend startup, healthcheck dependencies, and unified Docker Compose orchestration.
- **AI Contribution:** Authored `backend/Dockerfile`, `frontend/Dockerfile`, `frontend/nginx.conf` (with SPA fallback and `/api/` reverse proxy), and root `docker-compose.yml`. Added startup migration retries in `Program.cs` to prevent race conditions with PostgreSQL container initialization.
