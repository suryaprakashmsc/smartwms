import { createContext, useContext, useEffect, useState } from 'react'
import { fetchAuthSession, signInWithRedirect, signOut, getCurrentUser } from 'aws-amplify/auth'
import { Hub } from 'aws-amplify/utils'

interface AuthUser { 
  email: string
  name: string 
}

interface AuthCtx {
  user: AuthUser | null
  token: string | null
  isLoading: boolean
  error: string | null
  login: () => void
  logout: () => void
  isAuthenticated: boolean
}

const AuthContext = createContext<AuthCtx>({} as AuthCtx)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [token, setToken] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadSession()

    // Listen for auth events (like signIn completion)
    const unsubscribe = Hub.listen('auth', ({ payload }) => {
      console.log('Auth event:', payload.event)
      if (payload.event === 'signedIn' || payload.event === 'signInWithRedirect') {
        console.log('Sign-in event detected, reloading session')
        loadSession()
      } else if (payload.event === 'signedOut') {
        setUser(null)
        setToken(null)
        setError(null)
      }
    })

    return () => unsubscribe()
  }, [])

  async function loadSession() {
    try {
      setError(null)
      console.log('Loading auth session...')
      const session = await fetchAuthSession()
      console.log('Session retrieved:', { hasIdToken: !!session.tokens?.idToken, hasAccessToken: !!session.tokens?.accessToken })
      
      // Use accessToken for API authorization (not idToken)
      // accessToken is designed for API calls, idToken is for identity info
      const accessToken = session.tokens?.accessToken?.toString()
      
      if (accessToken) {
        try {
          console.log('Fetching current user...')
          const cognitoUser = await getCurrentUser()
          console.log('Current user retrieved:', cognitoUser.username)
          setToken(accessToken)
          setUser({ 
            email: cognitoUser.signInDetails?.loginId ?? cognitoUser.username ?? '',
            name: cognitoUser.username 
          })
          setError(null)
        } catch (userError) {
          console.error('Error getting current user:', userError)
          // Still set token even if user fetch fails
          setToken(accessToken)
          setUser(null)
          setError(null)
        }
      } else {
        console.log('No access token in session')
        setUser(null)
        setToken(null)
      }
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : String(err)
      console.error('Error loading session:', errorMsg)
      setUser(null)
      setToken(null)
      setError(errorMsg)
    } finally {
      setIsLoading(false)
    }
  }

  const login = async () => {
    try {
      setError(null)
      await signInWithRedirect()
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : String(err)
      console.error('Sign in error:', errorMsg)
      setError(errorMsg)
    }
  }
  
  const logout = async () => {
    try {
      await signOut({ global: true })
      setUser(null)
      setToken(null)
      setError(null)
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : String(err)
      console.error('Logout error:', errorMsg)
      setError(errorMsg)
    }
  }

  const isAuthenticated = user !== null && token !== null

  return (
    <AuthContext.Provider value={{ user, token, isLoading, error, login, logout, isAuthenticated }}>
      {children}
    </AuthContext.Provider>
  )
}

export const useAuthContext = () => useContext(AuthContext)
