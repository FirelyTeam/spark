import { Link, Outlet, useLocation } from 'react-router-dom'
import { useState } from 'react'
import { useAuth } from '../hooks/useAuth'

export function Layout() {
  const location = useLocation()
  const { isAuthenticated, isAdmin, user, logout, loginUrl, isLoading, authEnabled } = useAuth()
  const [loggingIn, setLoggingIn] = useState(false)

  const navigation = [
    { name: 'Home', href: '/' },
    { name: 'Resources', href: '/resources' },
    ...(authEnabled && isAdmin ? [{ name: 'Admin', href: '/admin' }] : []),
  ]

  return (
    <div className="min-h-screen flex flex-col bg-spark-darker">
      <header className="bg-spark-dark border-b border-spark-border">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            <Link to="/" className="flex items-center gap-2 group">
              <img 
                src="/logo.png" 
                alt="Spark" 
                className="w-9 h-9 transition-transform group-hover:scale-110" 
              />
              <span className="text-xl font-bold bg-gradient-to-r from-spark-ice via-spark-cyan to-spark-blue bg-clip-text text-transparent">
                Spark FHIR
              </span>
            </Link>
            <div className="flex items-center gap-4">
              <nav className="flex gap-1">
                {navigation.map((item) => (
                  <Link
                    key={item.href}
                    to={item.href}
                    className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                      location.pathname === item.href
                        ? 'bg-spark-cyan text-spark-navy'
                        : 'text-gray-400 hover:bg-spark-surface hover:text-white'
                    }`}
                  >
                    {item.name}
                  </Link>
                ))}
              </nav>
              {!isLoading && authEnabled && (
                <div className="flex items-center gap-3 ml-4 pl-4 border-l border-spark-border">
                  {isAuthenticated ? (
                    <>
                      {user?.avatarUrl && (
                        <img
                          src={user.avatarUrl}
                          alt={user.username}
                          className="w-8 h-8 rounded-full ring-2 ring-spark-border"
                        />
                      )}
                      <span className="text-sm text-gray-400">{user?.username}</span>
                      <button
                        onClick={() => logout()}
                        className="px-3 py-1.5 text-sm rounded-md bg-spark-surface hover:bg-spark-border transition-colors text-gray-300"
                      >
                        Logout
                      </button>
                    </>
                  ) : (
                    <a
                      href={loginUrl(location.pathname)}
                      onClick={() => setLoggingIn(true)}
                      className="flex items-center gap-2 px-3 py-1.5 text-sm rounded-md bg-spark-surface hover:bg-spark-border transition-colors text-gray-300"
                    >
                      {loggingIn ? (
                        <>
                          <div className="w-4 h-4 border-2 border-gray-400 border-t-transparent rounded-full animate-spin" />
                          Redirecting...
                        </>
                      ) : (
                        <>
                          <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                            <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
                          </svg>
                          Login with GitHub
                        </>
                      )}
                    </a>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </header>

      <main className="flex-1">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <Outlet />
        </div>
      </main>

      <footer className="bg-spark-dark border-t border-spark-border">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <p className="text-center text-sm text-gray-500">
            Spark FHIR Server • <a href="https://github.com/FirelyTeam/spark" className="text-spark-cyan hover:underline">GitHub</a>
          </p>
        </div>
      </footer>
    </div>
  )
}
