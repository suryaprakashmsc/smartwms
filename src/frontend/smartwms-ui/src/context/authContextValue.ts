import { createContext } from 'react'

export interface AuthUser { email: string; name: string }
export interface AuthCtx {
  user: AuthUser | null
  token: string | null
  isLoading: boolean
  error: string | null
  login: () => void
  logout: () => void
  isAuthenticated: boolean
}
export const AuthContext = createContext<AuthCtx | null>(null)
