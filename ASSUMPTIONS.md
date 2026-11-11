# Architectural & Business Logic Assumptions

This document outlines the architectural decisions, trade-offs, and design assumptions made during the implementation of the School Admission System.

---

## 1. Concurrency Strategy: Row-Level Locking (`SELECT ... FOR UPDATE`)

### Decision
Instead of relying solely on EF Core optimistic concurrency tokens (which reject conflicting transactions and force clients to retry), the system uses **pessimistic row-level locking via PostgreSQL `SELECT ... FOR UPDATE`** during all seat mutation operations (`CreateHold`, `ConfirmHold`, `CancelHold`).

### Rationale
- Under high concurrent load (e.g., flash admission where hundreds of applicants compete for the final 5 seats), optimistic concurrency suffers from excessive write conflicts and high retry overhead.
- By acquiring an exclusive row lock on the specific `Classes` record inside an explicit database transaction, competing seat requests are queued linearly by PostgreSQL's lock manager.
- Once the lock is acquired, the transaction performs an exact recalculation:
  $$\text{AvailableSeats} = \text{TotalSeats} - \text{ConfirmedSeats} - \text{ActiveHeldSeats}$$
- If available seats reach zero, subsequent requests are cleanly rejected with `409 Conflict` without data corruption, negative seat counts, or phantom overbooking.
- As a secondary safety net, EF Core's PostgreSQL system column concurrency token (`xmin`) is configured on the `ClassRoom` entity.

---

## 2. Dynamic Auto-Expiration vs. Background Worker

### Decision
Seat holds auto-expire dynamically at query and validation time based on UTC comparison (`ExpiresAt > DateTime.UtcNow`), rather than relying solely on a periodic background cleaner job.

### Rationale
- **Zero Latency:** Expired seats are immediately available for other students the exact millisecond the hold duration passes. A polling background worker (e.g. running every 30 seconds) would introduce a delay where seats remain locked even though their timer elapsed.
- **Resource Efficiency:** Eliminates continuous CPU and I/O polling overhead on idle databases.
- **Client Status Derivation:** The client UI also computes live remaining time from `expiresAt` and automatically switches state to `EXPIRED` once the timer reaches zero, disabling payment actions seamlessly.

---

## 3. Student Identity & Authentication

### Decision
- Identity is established strictly via the `X-Student-Id` HTTP header.
- The ID must be a **17-digit numeric string** corresponding to the student's Birth Registration Number.
- No session cookies or JWT tokens are utilized, adhering to the recruitment specification.
- On the frontend, this ID is permanently displayed at the top of the viewport, validated against `/^\d{17}$/`, and persisted in browser `localStorage` across page reloads.

---

## 4. One Hold Per Student Per Class Constraint

### Decision
A student cannot hold multiple seats in the same class simultaneously. If a student already has an active hold (`Status = 'HELD'` and `ExpiresAt > UTC_NOW`) or a confirmed admission (`Status = 'CONFIRMED'`), any subsequent application for that class is rejected with `409 Conflict`.
- Once a hold expires or is cancelled, the student is permitted to apply again if seats remain available.

---

## 5. Idempotent State Transitions

### Decision
- **Confirmation:** If `POST /holds/{holdId}/confirm` is invoked on a hold that has already been confirmed (`Status == "CONFIRMED"`), the system returns `200 OK` with `{ "holdId": "...", "status": "CONFIRMED" }` without double-incrementing `ConfirmedSeats`.
- **Cancellation:** If `DELETE /holds/{holdId}` is called on an already-cancelled hold, the endpoint returns `204 No Content` idempotently. If the cancelled hold was previously confirmed, `ConfirmedSeats` is decremented safely.

---

## 6. Docker & Networking

### Decision
- The frontend is served via high-performance Nginx on port `3000` (mapped to internal port `80`).
- The backend API is exposed on port `8080` (mapped to internal port `8080`).
- Both direct cross-origin calls (via CORS headers in ASP.NET Core) and reverse-proxy calls (via Nginx `/api/` upstream proxy to `http://backend:8080/`) are supported out-of-the-box.
- PostgreSQL database migrations are applied automatically at backend startup with retry logic, eliminating any manual CLI migration steps when running `docker compose up --build`.
