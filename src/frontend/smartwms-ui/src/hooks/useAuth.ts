import { useCallback, useEffect } from "react";
import { useAuth as useOidcAuth } from "react-oidc-context";
import { buildCognitoLogoutUrl } from "../auth/cognitoAuth";
import { clearAccessToken, setAccessToken } from "../services/authTokenStore";

export function useAuth() {
  const auth = useOidcAuth();
  const accessToken = auth.user?.access_token;

  useEffect(() => {
    if (accessToken) {
      setAccessToken(accessToken);
      return;
    }

    clearAccessToken();
  }, [accessToken]);

  const login = useCallback(async () => {
    await auth.signinRedirect();
  }, [auth]);

  const logout = useCallback(async () => {
    clearAccessToken();
    await auth.removeUser();
    window.location.assign(buildCognitoLogoutUrl());
  }, [auth]);

  const isLoading =
    auth.isLoading || auth.activeNavigator === "signinRedirect" || auth.activeNavigator === "signoutRedirect";

  return {
    isAuthenticated: auth.isAuthenticated,
    isLoading,
    error: auth.error ?? null,
    userEmail: (auth.user?.profile.email as string | undefined) ?? undefined,
    activeNavigator: auth.activeNavigator,
    login,
    logout
  };
}
