import { useState } from 'react';
import { Routes, Route, NavLink } from 'react-router-dom';
import StudentIdentity from './components/StudentIdentity.jsx';
import ClassManagement from './components/ClassManagement.jsx';
import ClassDashboard from './components/ClassDashboard.jsx';
import HoldDetails from './components/HoldDetails.jsx';
import MyApplications from './components/MyApplications.jsx';

function NavTab({ to, children }) {
  return (
    <NavLink
      to={to}
      end
      className={({ isActive }) =>
        `px-4 py-2 text-sm font-medium rounded-lg transition-colors ${
          isActive
            ? 'bg-blue-600 text-white'
            : 'text-gray-600 hover:bg-gray-100'
        }`
      }
    >
      {children}
    </NavLink>
  );
}

export default function App() {
  // A key to trigger class list refresh after creating one
  const [refreshKey, setRefreshKey] = useState(0);

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Sticky Identity Bar */}
      <StudentIdentity />

      {/* Navigation */}
      <div className="max-w-6xl mx-auto px-4 pt-4">
        <nav className="flex gap-2 mb-6">
          <NavTab to="/">Dashboard</NavTab>
          <NavTab to="/my-applications">My Applications</NavTab>
        </nav>
      </div>

      {/* Page Content */}
      <main className="max-w-6xl mx-auto px-4 pb-12">
        <Routes>
          <Route
            path="/"
            element={
              <div className="space-y-6">
                <ClassManagement onClassCreated={() => setRefreshKey((k) => k + 1)} />
                <ClassDashboard refreshKey={refreshKey} />
              </div>
            }
          />
          <Route path="/holds/:holdId" element={<HoldDetails />} />
          <Route path="/my-applications" element={<MyApplications />} />
        </Routes>
      </main>
    </div>
  );
}
