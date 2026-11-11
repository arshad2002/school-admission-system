import { useState } from 'react';
import toast from 'react-hot-toast';
import { createClass } from '../api/client.js';

export default function ClassManagement({ onClassCreated }) {
  const [name, setName] = useState('');
  const [totalSeats, setTotalSeats] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    if (!name.trim()) return toast.error('Class name is required.');
    const seats = parseInt(totalSeats, 10);
    if (!seats || seats <= 0) return toast.error('Total seats must be a positive number.');

    setLoading(true);
    try {
      const created = await createClass({ name: name.trim(), totalSeats: seats });
      toast.success(`Class "${created.name}" created!`);
      setName('');
      setTotalSeats('');
      onClassCreated?.();
    } catch (err) {
      toast.error(err.message || 'Failed to create class.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
      <h2 className="text-lg font-bold text-gray-800 mb-4">Create New Class</h2>
      <form onSubmit={handleSubmit} className="flex flex-col sm:flex-row gap-3">
        <input
          type="text"
          placeholder="Class Name (e.g. Class 6 - Morning Shift)"
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
        />
        <input
          type="number"
          min="1"
          placeholder="Total Seats"
          value={totalSeats}
          onChange={(e) => setTotalSeats(e.target.value)}
          className="w-full sm:w-32 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
        />
        <button
          type="submit"
          disabled={loading}
          className="px-5 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors whitespace-nowrap"
        >
          {loading ? 'Creating…' : 'Create Class'}
        </button>
      </form>
    </div>
  );
}
