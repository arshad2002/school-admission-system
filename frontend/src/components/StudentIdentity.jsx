import { useStudent } from '../context/StudentContext.jsx';

export default function StudentIdentity() {
  const { studentId, setStudentId } = useStudent();

  const isValid = /^\d{17}$/.test(studentId);

  return (
    <div className="bg-white border-b border-gray-200 shadow-sm sticky top-0 z-50">
      <div className="max-w-6xl mx-auto px-4 py-3 flex flex-col sm:flex-row items-start sm:items-center gap-2">
        <label htmlFor="student-id" className="text-sm font-semibold text-gray-700 whitespace-nowrap">
          Birth Registration No:
        </label>
        <div className="flex items-center gap-2 flex-1 w-full sm:w-auto">
          <input
            id="student-id"
            type="text"
            inputMode="numeric"
            maxLength={17}
            placeholder="Enter 17-digit number"
            value={studentId}
            onChange={(e) => {
              const val = e.target.value.replace(/\D/g, '').slice(0, 17);
              setStudentId(val);
            }}
            className={`flex-1 sm:w-72 px-3 py-2 border rounded-lg text-sm font-mono tracking-wider focus:outline-none focus:ring-2 ${
              studentId && !isValid
                ? 'border-red-400 focus:ring-red-300'
                : 'border-gray-300 focus:ring-blue-400'
            }`}
          />
          {studentId && !isValid && (
            <span className="text-xs text-red-500 whitespace-nowrap">Must be exactly 17 digits</span>
          )}
          {isValid && (
            <span className="text-xs text-green-600 whitespace-nowrap">✓ Valid</span>
          )}
        </div>
      </div>
    </div>
  );
}
