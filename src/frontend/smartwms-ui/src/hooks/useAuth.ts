import { useContext } from 'react';
import { AuthContext } from '../context/authContextValue';

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth requires AuthProvider');
  const { user, isLoading, login, logout, isAuthenticated, error } = context;

  return {
    isAuthenticated,
    isLoading,
    error,
    userEmail: user?.email ?? undefined,
    login,
    logout
  }
}
