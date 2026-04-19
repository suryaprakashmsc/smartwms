import { Navigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

export default function AuthCallbackPage() {
  const auth = useAuth();

  if (auth.isLoading || auth.activeNavigator === "signinRedirect") {
    return (
      <section className="login-screen">
        <div className="login-card">
          <p className="eyebrow">Security</p>
          <h2>Completing sign-in</h2>
          <p>Please wait while we verify your identity with AWS Cognito.</p>
        </div>
      </section>
    );
  }

  if (auth.error) {
    return (
      <section className="login-screen">
        <div className="login-card">
          <p className="eyebrow">Security</p>
          <h2>Sign-in failed</h2>
          <p className="subtle-text error-text">{auth.error.message}</p>
        </div>
      </section>
    );
  }

  return <Navigate to={auth.isAuthenticated ? "/" : "/login"} replace />;
}
