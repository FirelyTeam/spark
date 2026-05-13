import { createContext, useContext, useState, useEffect, type ReactNode } from 'react'

interface User {
  username: string
  email: string | null
  avatarUrl: string | null
  roles: string[]
}

interface AuthContextType {
  user: User | null
  isLoading: boolean
  isAuthenticated: boolean
  isAdmin: boolean
  authEnabled: boolean
  loginUrl: (returnUrl?: string) => string
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextType | null>(null)

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}

interface AuthProviderProps {
  children: ReactNode
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [authEnabled, setAuthEnabled] = useState(false)

  useEffect(() => {
    fetch('/api/auth/status')
      .then((res) => res.json())
      .then((data) => {
        setAuthEnabled(data.authEnabled ?? false)
        if (data.isAuthenticated) {
          setUser({
            username: data.username,
            email: data.email,
            avatarUrl: data.avatarUrl,
            roles: data.roles,
          })
        } else {
          setUser(null)
        }
      })
      .catch((error) => {
        console.error('Failed to check auth status:', error)
        setUser(null)
      })
      .finally(() => setIsLoading(false))
  }, [])

  const loginUrl = (returnUrl = '/admin') => {
    return `/api/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`
  }

  const logout = async () => {
    await fetch('/api/auth/logout', { method: 'POST' })
    setUser(null)
  }

  const isAuthenticated = user !== null
  const isAdmin = user?.roles.includes('Admin') ?? false

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated,
        isAdmin,
        authEnabled,
        loginUrl,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}
