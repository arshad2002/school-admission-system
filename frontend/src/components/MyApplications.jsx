import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import { getMyHolds } from '../api/client.js';
import { useStudent } from '../context/StudentContext.jsx';

const STATUS_CONFIG = {
  HELD: { bg: 'bg-amber-100', text: 'text-amber-800', dot: 'bg-amber-500' },
  CONFIRMED: { bg: 'bg-green-100', text: 'text-green-800', dot: 'bg-green-500' },
  EXPIRED: { bg: 'bg-red-100', text: 'text-red-800', dot: 'bg-red-500' },
  CANCELLED: { bg: 'bg-gray-100', text: 'text-gray-600', dot: 'bg-gray-400' },
};

export default function MyApplications() {
  const { studentId } = useStudent();
  const isValid = /^\d{17}$/.test(studentId);
  const [holds, setHolds] = useState([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!isValid) return;
    setLoading(true);
    getMyHolds(studentId)
      .then(setHolds)
      .catch((err) => toast.error(err.message || 'Failed to load your applications.'))
      .finally(() => setLoading(false));
  }, [studentId, isValid]);

  if (!isValid) {
    return (
      <div className="text-center py-12 text-gray-500">
        <p className="text-sm">Enter your 17-digit Birth Registration Number to see your applications.</p>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="text-center py-12 text-gray-500">
        <div className="inline-block w-6 h-6 border-2 border-gray-300 border-t-blue-600 rounded-full animate-spin mb-2" />
        <p className="text-sm">Loading your applications…</p>
      </div>
    );
  }

  if (holds.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500">
        <p className="text-sm">You have no applications yet.</p>
        <Link to="/" className="text-blue-600 hover:underline text-sm mt-2 inline-block">
          Browse available classes →
        </Link>
      </div>
    );
  }

  return (
    <div>
      <h2 className="text-lg font-bold text-gray-800 mb-4">My Applications</h2>

      <div className="space-y-3">
        {holds.map((h) => {
          // Client-side auto-expire
          const effectiveStatus =
            h.status === 'HELD' && new Date(h.expiresAt) <= new Date() ? 'EXPIRED' : h.status;
          const cfg = STATUS_CONFIG[effectiveStatus] || STATUS_CONFIG.HELD;

          return (
            <Link
              key={h.holdId}
              to={`/holds/${h.holdId}`}
              className="block bg-white rounded-xl shadow-sm border border-gray-200 p-4 hover:border-blue-300 hover:shadow transition-all"
            >
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-gray-800">{h.className}</p>
                  <p className="text-xs text-gray-400 font-mono mt-0.5">{h.holdId}</p>
                </div>
                <div className="flex items-center gap-2">
                  <span className={`w-2 h-2 rounded-full ${cfg.dot}`} />
                  <span className={`px-2.5 py-0.5 rounded-full text-xs font-semibold ${cfg.bg} ${cfg.text}`}>
                    {effectiveStatus}
                  </span>
                </div>
              </div>
              <p className="text-xs text-gray-500 mt-2">
                Expires: {new Date(h.expiresAt).toLocaleString()}
              </p>
            </Link>
          );
        })}
      </div>
    </div>
  );
}
