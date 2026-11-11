# School Admission System

A high-concurrency School Admission System backend built with **.NET 10** and **PostgreSQL**, paired with a **React 18 + Vite** frontend, fully containerized using **Docker** and **Docker Compose**.

---

## 🏛 Architecture & Tech Stack

- **Backend:** .NET 10.0 Web API, Entity Framework Core 10, Npgsql PostgreSQL Provider, Scalar OpenAPI Reference
- **Frontend:** React 18, Vite, Tailwind CSS, React Hot Toast, React Router DOM
- **Database:** PostgreSQL 16 (with row-level locking `SELECT ... FOR UPDATE` and EF Core optimistic concurrency)
- **Containerization:** Docker multi-stage builds (`mcr.microsoft.com/dotnet/sdk:10.0` & `aspnet:10.0`, `node:20-alpine` & `nginx:alpine`), Docker Compose

---

## 🚀 Quick Start (Docker Compose)

The entire application stack (Database, Backend API, Frontend SPA) can be launched with a single command:

```bash
docker compose up --build
```

### Service Endpoints

| Service | URL | Description |
|---|---|---|
| **Frontend UI** | [http://localhost:3000](http://localhost:3000) | React SPA dashboard & application portal |
| **Backend API** | [http://localhost:8080](http://localhost:8080) | RESTful API |
| **Scalar API Docs** | [http://localhost:8080/scalar/v1](http://localhost:8080/scalar/v1) | Interactive OpenAPI reference |
| **OpenAPI Schema** | [http://localhost:8080/openapi/v1.json](http://localhost:8080/openapi/v1.json) | Raw OpenAPI v3 specification |
| **PostgreSQL** | `localhost:5432` | Database (`admissions_db`, user: `postgres`) |

---

## 🔒 Domain Rules & Concurrency Strategy

1. **Seat Calculation:**
   $$\text{AvailableSeats} = \max(0, \text{TotalSeats} - \text{ConfirmedSeats} - \text{ActiveHeldSeats})$$
   - `ActiveHeldSeats`: Holds where `Status = 'HELD'` and `ExpiresAt > UTC_NOW`.
   - `ConfirmedSeats`: Atomically incremented count on the `Classes` entity upon hold confirmation.

2. **Strict Concurrency Protection:**
   - Every seat-mutating transaction (`CreateHold`, `ConfirmHold`, `CancelHold`) uses PostgreSQL row-level locking (`SELECT ... FOR UPDATE`) on the target class row.
   - This ensures that under high concurrent load (e.g. 100 students applying for 1 seat simultaneously), the invariant:
     $$(\text{Active Held} + \text{Confirmed}) \le \text{TotalSeats}$$
     is **strictly preserved** without race conditions or overbooking.

3. **Dynamic Auto-Expiration:**
   - Holds expire automatically after `HOLD_DURATION_SECONDS` (default: 600s / 10 minutes).
   - Seat availability queries and hold status checks evaluate expiration dynamically against UTC time: any hold past its expiration timestamp is instantly treated as `EXPIRED` without requiring background workers or manual cron triggers.

4. **Student Identity:**
   - Authenticated via the `X-Student-Id` HTTP header containing a 17-digit Birth Registration Number.
   - Enforced across all student endpoints (`POST /classes/{id}/holds`, `POST /holds/{id}/confirm`, `DELETE /holds/{id}`, `GET /me/holds`).

5. **Single Active Hold Constraint:**
   - A student can have only **one** active hold or confirmed admission per class at a time.

6. **Idempotency:**
   - Confirming an already-confirmed hold returns `200 OK` without double-incrementing `ConfirmedSeats`.
   - Cancelling an already-cancelled hold returns `204 No Content`.

---

## 📡 API Specification

All payloads and responses use `application/json` with camelCase property naming.

### 1. Create Class
- **Method / Path:** `POST /classes`
- **Request Body:**
  ```json
  {
    "name": "Class 6 - Morning Shift",
    "totalSeats": 120
  }
  ```
- **Response (201 Created):**
  ```json
  {
    "id": "c_a1b2c3d4e5",
    "name": "Class 6 - Morning Shift",
    "totalSeats": 120
  }
  ```

### 2. List Classes
- **Method / Path:** `GET /classes`
- **Response (200 OK):**
  ```json
  [
    {
      "id": "c_a1b2c3d4e5",
      "name": "Class 6 - Morning Shift",
      "totalSeats": 120,
      "heldSeats": 15,
      "confirmedSeats": 80,
      "availableSeats": 25
    }
  ]
  ```

### 3. Get Class Details
- **Method / Path:** `GET /classes/{classId}`
- **Response (200 OK):**
  ```json
  {
    "id": "c_a1b2c3d4e5",
    "name": "Class 6 - Morning Shift",
    "totalSeats": 120,
    "heldSeats": 15,
    "confirmedSeats": 80,
    "availableSeats": 25
  }
  ```

### 4. Apply / Hold Seat
- **Method / Path:** `POST /classes/{classId}/holds`
- **Headers:** `X-Student-Id: <17-digit-number>`
- **Response (201 Created):**
  ```json
  {
    "holdId": "h_f6e5d4c3b2",
    "classId": "c_a1b2c3d4e5",
    "studentId": "20152691234567890",
    "status": "HELD",
    "expiresAt": "2026-11-01T04:10:00Z"
  }
  ```
- **Response (409 Conflict):** If no seats are available or student already has an active hold.

### 5. Check Hold Status
- **Method / Path:** `GET /holds/{holdId}`
- **Response (200 OK):**
  ```json
  {
    "holdId": "h_f6e5d4c3b2",
    "classId": "c_a1b2c3d4e5",
    "status": "HELD",
    "expiresAt": "2026-11-01T04:10:00Z"
  }
  ```
  *(Status dynamically evaluates to `"EXPIRED"` if `expiresAt` is in the past.)*

### 6. Confirm Hold (Pay Fee)
- **Method / Path:** `POST /holds/{holdId}/confirm`
- **Headers:** `X-Student-Id: <17-digit-number>`
- **Response (200 OK):**
  ```json
  {
    "holdId": "h_f6e5d4c3b2",
    "status": "CONFIRMED"
  }
  ```

### 7. Cancel Hold
- **Method / Path:** `DELETE /holds/{holdId}`
- **Headers:** `X-Student-Id: <17-digit-number>`
- **Response:** `204 No Content`

### 8. My Holds
- **Method / Path:** `GET /me/holds`
- **Headers:** `X-Student-Id: <17-digit-number>`
- **Response (200 OK):**
  ```json
  [
    {
      "holdId": "h_f6e5d4c3b2",
      "classId": "c_a1b2c3d4e5",
      "className": "Class 6 - Morning Shift",
      "status": "HELD",
      "expiresAt": "2026-11-01T04:10:00Z"
    }
  ]
  ```

---

## 💻 Local Development Without Docker

### Prerequisites
- .NET 10 SDK
- Node.js 20+
- PostgreSQL 16+ running locally

### Running Backend
```bash
cd backend/AdmissionSystem/AdmissionSystem.Api
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=admissions_db;Username=postgres;Password=postgres;"
dotnet run
```

### Running Frontend
```bash
cd frontend
npm install
npm run dev
```
Open `http://localhost:5173` in your browser.
