const tokenKey = "smartwms.demo.token";

export function useAuth() {
  const isAuthenticated = typeof window !== "undefined" && Boolean(localStorage.getItem(tokenKey));

  const login = () => localStorage.setItem(tokenKey, "demo-session");
  const logout = () => localStorage.removeItem(tokenKey);

  return {
    isAuthenticated,
    login,
    logout
  };
}
