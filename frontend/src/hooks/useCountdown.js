import { useState, useEffect, useRef } from 'react';

/**
 * Returns the remaining seconds until `expiresAt` (ISO string).
 * Ticks down every second. Returns 0 when expired.
 */
export function useCountdown(expiresAt) {
  const [remaining, setRemaining] = useState(() => calcRemaining(expiresAt));
  const intervalRef = useRef(null);

  useEffect(() => {
    setRemaining(calcRemaining(expiresAt));

    if (!expiresAt) return;

    intervalRef.current = setInterval(() => {
      const r = calcRemaining(expiresAt);
      setRemaining(r);
      if (r <= 0) clearInterval(intervalRef.current);
    }, 1000);

    return () => clearInterval(intervalRef.current);
  }, [expiresAt]);

  return remaining;
}

function calcRemaining(expiresAt) {
  if (!expiresAt) return 0;
  return Math.max(0, Math.floor((new Date(expiresAt).getTime() - Date.now()) / 1000));
}

/**
 * Format seconds as MM:SS or HH:MM:SS.
 */
export function formatCountdown(totalSeconds) {
  if (totalSeconds <= 0) return '00:00';
  const h = Math.floor(totalSeconds / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  const s = totalSeconds % 60;
  const pad = (n) => String(n).padStart(2, '0');
  return h > 0 ? `${pad(h)}:${pad(m)}:${pad(s)}` : `${pad(m)}:${pad(s)}`;
}
