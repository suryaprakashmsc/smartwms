import { useEffect, useState, type ReactNode } from 'react'
import { AuthContext, type AuthUser } from './authContextValue'
import { fetchAuthSession, signInWithRedirect, signOut, getCurrentUser } from 'aws-amplify/auth'
import { Hub } from 'aws-amplify/utils'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [token, setToken] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    let generation = 0
    async function loadSession() {
      const request = ++generation
      try {
        const session = await fetchAuthSession()
        const accessToken = session.tokens?.accessToken?.toString()
        const currentUser = accessToken ? await getCurrentUser() : null
        if (!active || request !== generation) return
        setError(null)
        setToken(accessToken ?? null)
        setUser(currentUser ? {
          email: currentUser.signInDetails?.loginId ?? currentUser.username,
          name: currentUser.username,
        } : null)
      } catch (err) {
        if (!active || request !== generation) return
        setUser(null)
        setToken(null)
        setError(err instanceof Error ? err.message : 'Unable to load your session')
      } finally {
        if (active && request === generation) setIsLoading(false)
      }
    }
    void loadSession()
    const unsubscribe = Hub.listen('auth', ({ payload }) => {
      if (payload.event === 'signedIn' || payload.event === 'signInWithRedirect') {
        void loadSession()
      } else if (payload.event === 'signedOut') {
        generation++
        setUser(null)
        setToken(null)
        setError(null)
        setIsLoading(false)
      }
    })
    return () => { active = false; generation++; unsubscribe() }
  }, [])

  const login = async () => {
    try { setError(null); await signInWithRedirect() }
    catch (err) { setError(err instanceof Error ? err.message : 'Unable to sign in') }
  }
  const logout = async () => {
    try {
      await signOut({ global: true })
      setUser(null)
      setToken(null)
      setError(null)
    } catch (err) { setError(err instanceof Error ? err.message : 'Unable to sign out') }
  }
  return (
    <AuthContext.Provider value={{ user, token, isLoading, error, login, logout, isAuthenticated: user !== null && token !== null }}>
      {children}
    </AuthContext.Provider>
  )
}
