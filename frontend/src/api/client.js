/**
 * API client for the School Admission System backend.
 * Reads the base URL from VITE_API_BASE_URL and attaches
 * the X-Student-Id header when studentId is provided.
 */

const rawBaseUrl = import.meta.env.VITE_API_BASE_URL;
const BASE_URL = rawBaseUrl !== undefined && rawBaseUrl !== ''
  ? rawBaseUrl.replace(/\/+$/, '')
  : 'http://localhost:8080';

export class ApiError extends Error {
  constructor(status, body) {
    super(body?.error || body?.title || `Request failed with status ${status}`);
    this.status = status;
    this.body = body;
  }
}

async function request(path, { method = 'GET', body, studentId } = {}) {
  const headers = {};

  // Only attach Content-Type when there is a request body (avoids unnecessary CORS preflights on GET)
  if (body !== undefined && body !== null) {
    headers['Content-Type'] = 'application/json';
  }

  if (studentId) {
    headers['X-Student-Id'] = studentId;
  }

  let res;
  try {
    res = await fetch(`${BASE_URL}${path}`, {
      method,
      headers,
      body: body !== undefined && body !== null ? JSON.stringify(body) : undefined,
    });
  } catch (networkErr) {
    throw new Error(
      `Unable to connect to the backend API at ${BASE_URL}. Ensure the backend service is running and accessible.`
    );
  }

  // 204 No Content
  if (res.status === 204) return null;

  const contentType = res.headers.get('content-type') || '';
  const data = contentType.includes('application/json') ? await res.json() : null;

  if (!res.ok) {
    throw new ApiError(res.status, data);
  }

  return data;
}

// ── Class Endpoints ─────────────────────────────────────────

export function createClass(payload) {
  return request('/classes', { method: 'POST', body: payload });
}

export function listClasses() {
  return request('/classes');
}

export function getClass(classId) {
  return request(`/classes/${classId}`);
}

// ── Hold Endpoints ──────────────────────────────────────────

export function createHold(classId, studentId) {
  return request(`/classes/${classId}/holds`, { method: 'POST', studentId });
}

export function getHold(holdId) {
  return request(`/holds/${holdId}`);
}

export function confirmHold(holdId, studentId) {
  return request(`/holds/${holdId}/confirm`, { method: 'POST', studentId });
}

export function cancelHold(holdId, studentId) {
  return request(`/holds/${holdId}`, { method: 'DELETE', studentId });
}

// ── Student Endpoints ───────────────────────────────────────

export function getMyHolds(studentId) {
  return request('/me/holds', { studentId });
}
