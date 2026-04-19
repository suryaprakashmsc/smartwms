const defaultOrigin = typeof window !== "undefined" ? window.location.origin : "http://localhost:5173";

const authority = import.meta.env.VITE_COGNITO_AUTHORITY ?? "";
const clientId = import.meta.env.VITE_COGNITO_CLIENT_ID ?? "";
const cognitoDomain = import.meta.env.VITE_COGNITO_DOMAIN ?? "";
const redirectUri = import.meta.env.VITE_COGNITO_REDIRECT_URI ?? `${defaultOrigin}/callback`;
const postLogoutRedirectUri =
  import.meta.env.VITE_COGNITO_POST_LOGOUT_REDIRECT_URI ?? `${defaultOrigin}/login`;
const scope = import.meta.env.VITE_COGNITO_SCOPE ?? "openid email profile";

export const missingCognitoConfigKeys = [
  ["VITE_COGNITO_AUTHORITY", authority],
  ["VITE_COGNITO_CLIENT_ID", clientId],
  ["VITE_COGNITO_DOMAIN", cognitoDomain]
]
  .filter(([, value]) => !value)
  .map(([key]) => key);

export const cognitoAuthConfig = {
  authority,
  client_id: clientId,
  redirect_uri: redirectUri,
  post_logout_redirect_uri: postLogoutRedirectUri,
  response_type: "code",
  scope
};

export function buildCognitoLogoutUrl(): string {
  if (!cognitoDomain || !clientId) {
    return postLogoutRedirectUri;
  }

  try {
    const normalizedDomain = cognitoDomain.endsWith("/") ? cognitoDomain.slice(0, -1) : cognitoDomain;
    const logoutUrl = new URL(`${normalizedDomain}/logout`);
    logoutUrl.searchParams.set("client_id", clientId);
    logoutUrl.searchParams.set("logout_uri", postLogoutRedirectUri);
    return logoutUrl.toString();
  } catch {
    return postLogoutRedirectUri;
  }
}
