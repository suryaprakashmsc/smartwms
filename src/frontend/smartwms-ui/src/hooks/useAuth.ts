import { useAuthContext } from "../context/AuthContext";

export function useAuth() {
  const { user, token, isLoading, login, logout, isAuthenticated, error } = useAuthContext()

  return {
    isAuthenticated,
    isLoading,
    error,
    userEmail: user?.email ?? undefined,
    login,
    logout
  }
}
