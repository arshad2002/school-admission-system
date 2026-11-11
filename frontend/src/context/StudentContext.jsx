import { createContext, useContext, useState, useEffect } from 'react';

const STORAGE_KEY = 'studentId';
const StudentContext = createContext(null);

export function StudentProvider({ children }) {
  const [studentId, setStudentId] = useState(() => {
    return localStorage.getItem(STORAGE_KEY) || '';
  });

  useEffect(() => {
    if (studentId) {
      localStorage.setItem(STORAGE_KEY, studentId);
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  }, [studentId]);

  return (
    <StudentContext.Provider value={{ studentId, setStudentId }}>
      {children}
    </StudentContext.Provider>
  );
}

export function useStudent() {
  const ctx = useContext(StudentContext);
  if (!ctx) throw new Error('useStudent must be used within StudentProvider');
  return ctx;
}
