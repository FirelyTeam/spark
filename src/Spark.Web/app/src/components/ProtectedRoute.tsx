import { useLocation } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

interface ProtectedRouteProps {
  children: React.ReactNode
  requireAdmin?: boolean
}

export function ProtectedRoute({ children, requireAdmin = false }: ProtectedRouteProps) {
  const { isAuthenticated, isAdmin, isLoading, loginUrl, authEnabled } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-spark-cyan" />
      </div>
    )
  }

  if (!authEnabled) {
    return (
      <div className="bg-yellow-900/20 border border-yellow-500/30 rounded-lg p-6 text-center">
        <h2 className="text-lg font-semibold text-yellow-400">Authentication Not Configured</h2>
        <p className="mt-2 text-yellow-300/80">
          GitHub OAuth is not configured. Set <code className="bg-spark-surface px-1 rounded">GitHub:ClientId</code> and{' '}
          <code className="bg-spark-surface px-1 rounded">GitHub:ClientSecret</code> in appsettings.json to enable admin access.
        </p>
      </div>
    )
  }

  if (!isAuthenticated) {
    // Redirect to GitHub OAuth
    window.location.href = loginUrl(location.pathname)
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-gray-400">Redirecting to GitHub login...</p>
      </div>
    )
  }

  if (requireAdmin && !isAdmin) {
    return (
      <div className="bg-red-900/20 border border-red-500/30 rounded-lg p-6 text-center">
        <h2 className="text-lg font-semibold text-red-400">Access Denied</h2>
        <p className="mt-2 text-red-300/80">
          You don't have permission to access this page.
        </p>
      </div>
    )
  }

  return <>{children}</>
}
