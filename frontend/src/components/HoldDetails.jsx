import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import { getHold, confirmHold, cancelHold } from '../api/client.js';
import { useStudent } from '../context/StudentContext.jsx';
import { useCountdown, formatCountdown } from '../hooks/useCountdown.js';

const STATUS_CONFIG = {
  HELD: { bg: 'bg-amber-100', text: 'text-amber-800', label: 'Held' },
  CONFIRMED: { bg: 'bg-green-100', text: 'text-green-800', label: 'Confirmed' },
  EXPIRED: { bg: 'bg-red-100', text: 'text-red-800', label: 'Expired' },
  CANCELLED: { bg: 'bg-gray-100', text: 'text-gray-600', label: 'Cancelled' },
};

export default function HoldDetails() {
  const { holdId } = useParams();
  const navigate = useNavigate();
  const { studentId } = useStudent();
  const isValid = /^\d{17}$/.test(studentId);

  const [hold, setHold] = useState(null);
  const [loading, setLoading] = useState(true);
  const [confirming, setConfirming] = useState(false);
  const [cancelling, setCancelling] = useState(false);

  useEffect(() => {
    async function fetch() {
      try {
        const data = await getHold(holdId);
        setHold(data);
      } catch (err) {
        toast.error(err.status === 404 ? 'Hold not found.' : err.message);
      } finally {
        setLoading(false);
      }
    }
    fetch();
  }, [holdId]);

  // Derive effective status (auto-expire on client)
  const remaining = useCountdown(hold?.status === 'HELD' ? hold.expiresAt : null);
  const effectiveStatus =
    hold?.status === 'HELD' && remaining <= 0 ? 'EXPIRED' : hold?.status || 'HELD';
  const cfg = STATUS_CONFIG[effectiveStatus] || STATUS_CONFIG.HELD;

  async function handleConfirm() {
    if (!isValid) return toast.error('Enter a valid 17-digit Birth Registration Number first.');
    setConfirming(true);
    try {
      await confirmHold(holdId, studentId);
      setHold((prev) => ({ ...prev, status: 'CONFIRMED' }));
      toast.success('Admission confirmed!');
    } catch (err) {
      if (err.status === 403) toast.error('This hold does not belong to you.');
      else if (err.status === 409) toast.error(err.message || 'Cannot confirm this hold.');
      else toast.error(err.message || 'Failed to confirm.');
    } finally {
      setConfirming(false);
    }
  }

  async function handleCancel() {
    if (!isValid) return toast.error('Enter a valid 17-digit Birth Registration Number first.');
    setCancelling(true);
    try {
      await cancelHold(holdId, studentId);
      setHold((prev) => ({ ...prev, status: 'CANCELLED' }));
      toast.success('Hold cancelled.');
    } catch (err) {
      if (err.status === 403) toast.error('This hold does not belong to you.');
      else toast.error(err.message || 'Failed to cancel.');
    } finally {
      setCancelling(false);
    }
  }

  if (loading) {
    return (
      <div className="text-center py-12 text-gray-500">
        <div className="inline-block w-6 h-6 border-2 border-gray-300 border-t-blue-600 rounded-full animate-spin mb-2" />
        <p className="text-sm">Loading hold details…</p>
      </div>
    );
  }

  if (!hold) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-500 mb-4">Hold not found.</p>
        <Link to="/" className="text-blue-600 hover:underline text-sm">← Back to Dashboard</Link>
      </div>
    );
  }

  return (
    <div className="max-w-lg mx-auto">
      <Link to="/" className="text-blue-600 hover:underline text-sm mb-4 inline-block">← Back to Dashboard</Link>

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-lg font-bold text-gray-800">Hold Details</h2>
          <span className={`px-3 py-1 rounded-full text-xs font-semibold ${cfg.bg} ${cfg.text}`}>
            {cfg.label}
          </span>
        </div>

        <dl className="space-y-3 text-sm">
          <Row label="Hold ID" value={hold.holdId} mono />
          <Row label="Class ID" value={hold.classId} mono />
          <Row label="Expires At" value={new Date(hold.expiresAt).toLocaleString()} />
        </dl>

        {/* Countdown Timer */}
        {effectiveStatus === 'HELD' && remaining > 0 && (
          <div className="mt-6 p-4 bg-amber-50 rounded-lg border border-amber-200 text-center">
            <p className="text-xs text-amber-600 mb-1 font-medium">Time Remaining</p>
            <p className={`text-3xl font-mono font-bold ${
              remaining <= 60 ? 'text-red-600 animate-pulse' : 'text-amber-800'
            }`}>
              {formatCountdown(remaining)}
            </p>
          </div>
        )}

        {effectiveStatus === 'EXPIRED' && (
          <div className="mt-6 p-4 bg-red-50 rounded-lg border border-red-200 text-center">
            <p className="text-sm text-red-700 font-medium">This hold has expired. The seat has been released.</p>
          </div>
        )}

        {effectiveStatus === 'CONFIRMED' && (
          <div className="mt-6 p-4 bg-green-50 rounded-lg border border-green-200 text-center">
            <p className="text-sm text-green-700 font-medium">✓ Admission confirmed and fee paid.</p>
          </div>
        )}

        {/* Action Buttons */}
        <div className="mt-6 flex gap-3">
          {(effectiveStatus === 'HELD' && remaining > 0) && (
            <>
              <button
                onClick={handleConfirm}
                disabled={confirming}
                className="flex-1 py-2.5 bg-green-600 text-white text-sm font-medium rounded-lg hover:bg-green-700 disabled:opacity-50 transition-colors"
              >
                {confirming ? 'Processing…' : '💳 Pay Admission Fee'}
              </button>
              <button
                onClick={handleCancel}
                disabled={cancelling}
                className="py-2.5 px-5 bg-white border border-red-300 text-red-600 text-sm font-medium rounded-lg hover:bg-red-50 disabled:opacity-50 transition-colors"
              >
                {cancelling ? 'Cancelling…' : 'Cancel'}
              </button>
            </>
          )}

          {effectiveStatus === 'CONFIRMED' && (
            <button
              onClick={handleCancel}
              disabled={cancelling}
              className="flex-1 py-2.5 bg-white border border-red-300 text-red-600 text-sm font-medium rounded-lg hover:bg-red-50 disabled:opacity-50 transition-colors"
            >
              {cancelling ? 'Cancelling…' : 'Cancel Admission'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

function Row({ label, value, mono }) {
  return (
    <div className="flex justify-between items-center">
      <dt className="text-gray-500">{label}</dt>
      <dd className={`text-gray-900 font-medium ${mono ? 'font-mono text-xs' : ''}`}>{value}</dd>
    </div>
  );
}
