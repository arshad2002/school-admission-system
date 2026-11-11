import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { listClasses, createHold } from '../api/client.js';
import { useStudent } from '../context/StudentContext.jsx';

const POLL_INTERVAL = 4000; // 4 seconds

export default function ClassDashboard({ refreshKey }) {
  const [classes, setClasses] = useState([]);
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState(null);
  const [applyingId, setApplyingId] = useState(null);
  const { studentId } = useStudent();
  const navigate = useNavigate();
  const isValid = /^\d{17}$/.test(studentId);

  const fetchClasses = useCallback(async (isInitial = false) => {
    try {
      const data = await listClasses();
      setClasses(data);
      setFetchError(null);
    } catch (err) {
      setFetchError(err.message || 'Failed to load classes.');
      if (isInitial) {
        toast.error(err.message || 'Failed to load classes.');
      }
    } finally {
      setLoading(false);
    }
  }, []);

  // Initial fetch + polling
  useEffect(() => {
    fetchClasses(true);
    const id = setInterval(() => fetchClasses(false), POLL_INTERVAL);
    return () => clearInterval(id);
  }, [fetchClasses, refreshKey]);

  async function handleApply(classId) {
    if (!isValid) return toast.error('Enter a valid 17-digit Birth Registration Number first.');

    setApplyingId(classId);
    try {
      const hold = await createHold(classId, studentId);
      toast.success('Seat held successfully!');
      navigate(`/holds/${hold.holdId}`);
    } catch (err) {
      if (err.status === 409) {
        toast.error(err.message || 'No available seats or you already have an active hold.');
      } else if (err.status === 404) {
        toast.error('Class not found.');
      } else {
        toast.error(err.message || 'Failed to apply.');
      }
    } finally {
      setApplyingId(null);
    }
  }

  if (loading) {
    return (
      <div className="text-center py-12 text-gray-500">
        <div className="inline-block w-6 h-6 border-2 border-gray-300 border-t-blue-600 rounded-full animate-spin mb-2" />
        <p className="text-sm">Connecting to backend and loading classes…</p>
      </div>
    );
  }

  if (fetchError && classes.length === 0) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-xl p-6 text-center my-4">
        <div className="text-red-600 font-semibold mb-1">Failed to Connect to Server</div>
        <p className="text-sm text-red-500 mb-4">{fetchError}</p>
        <button
          onClick={() => {
            setLoading(true);
            fetchClasses(true);
          }}
          className="px-4 py-2 bg-red-600 text-white text-sm font-medium rounded-lg hover:bg-red-700 transition-colors"
        >
          ↻ Retry Connection
        </button>
      </div>
    );
  }

  if (classes.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500">
        <p className="text-sm">No classes available. Create one above to get started.</p>
      </div>
    );
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-bold text-gray-800">Available Classes</h2>
        <span className="text-xs text-gray-400">Auto-refreshes every {POLL_INTERVAL / 1000}s</span>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {classes.map((c) => (
          <div
            key={c.id}
            className="bg-white rounded-xl shadow-sm border border-gray-200 p-5 flex flex-col"
          >
            <h3 className="font-semibold text-gray-800 mb-3 truncate" title={c.name}>
              {c.name}
            </h3>

            <div className="grid grid-cols-2 gap-2 text-sm mb-4">
              <Stat label="Total" value={c.totalSeats} color="text-gray-700" />
              <Stat label="Held" value={c.heldSeats} color="text-amber-600" />
              <Stat label="Confirmed" value={c.confirmedSeats} color="text-blue-600" />
              <Stat
                label="Available"
                value={c.availableSeats}
                color={c.availableSeats > 0 ? 'text-green-600' : 'text-red-600'}
              />
            </div>

            <button
              onClick={() => handleApply(c.id)}
              disabled={c.availableSeats <= 0 || applyingId === c.id}
              className={`mt-auto w-full py-2 text-sm font-medium rounded-lg transition-colors ${
                c.availableSeats > 0
                  ? 'bg-green-600 text-white hover:bg-green-700 disabled:opacity-50'
                  : 'bg-gray-200 text-gray-500 cursor-not-allowed'
              }`}
            >
              {applyingId === c.id
                ? 'Applying…'
                : c.availableSeats > 0
                  ? 'Apply for Seat'
                  : 'No Seats Available'}
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}

function Stat({ label, value, color }) {
  return (
    <div className="flex justify-between items-center bg-gray-50 rounded-lg px-3 py-1.5">
      <span className="text-gray-500">{label}</span>
      <span className={`font-bold ${color}`}>{value}</span>
    </div>
  );
}
