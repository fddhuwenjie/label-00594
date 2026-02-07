import { Routes, Route, Navigate } from 'react-router-dom'
import { useStore } from './store/useStore'
import MainLayout from './layouts/MainLayout'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import RequestListPage from './pages/RequestListPage'
import RequestFormPage from './pages/RequestFormPage'
import RequestDetailPage from './pages/RequestDetailPage'
import ApprovalPage from './pages/ApprovalPage'
import NotificationPage from './pages/NotificationPage'
import UserManagePage from './pages/UserManagePage'

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const user = useStore((state) => state.user)
  return user ? <>{children}</> : <Navigate to="/login" replace />
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <PrivateRoute>
            <MainLayout />
          </PrivateRoute>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="requests" element={<RequestListPage />} />
        <Route path="requests/new" element={<RequestFormPage />} />
        <Route path="requests/:id" element={<RequestDetailPage />} />
        <Route path="requests/:id/edit" element={<RequestFormPage />} />
        <Route path="approvals" element={<ApprovalPage />} />
        <Route path="notifications" element={<NotificationPage />} />
        <Route path="users" element={<UserManagePage />} />
      </Route>
    </Routes>
  )
}

export default App
