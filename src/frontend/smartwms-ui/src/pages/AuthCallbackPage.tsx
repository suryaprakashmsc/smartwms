import { Navigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { useEffect, useState } from "react";

export default function AuthCallbackPage() {
  const auth = useAuth();
  const [hasError, setHasError] = useState(false);

  useEffect(() => {
    // If auth completes during callback, we'll redirect
    if (!auth.isLoading && auth.isAuthenticated) {
      return; // Component will render Navigate below
    }

    // If there's an explicit error, show it immediately
    if (auth.error && !auth.isLoading) {
      setHasError(true)
      return
    }

    // Set a longer timeout to detect if something went wrong (10 seconds)
    // AWS Cognito token exchange can take a few seconds
    const timer = setTimeout(() => {
      if (!auth.isAuthenticated && !auth.isLoading) {
        setHasError(true);
      }
    }, 10000);

    return () => clearTimeout(timer);
  }, [auth.isLoading, auth.isAuthenticated, auth.error]);

  if (auth.isAuthenticated && !auth.isLoading) {
    return <Navigate to="/" replace />;
  }

  if (hasError || auth.error) {
    return (
      <section className="login-screen">
        <div className="login-card">
          <p className="eyebrow">Security</p>
          <h2>Sign-in failed</h2>
          <p className="subtle-text error-text">
            {auth.error || "Failed to complete authentication. Please try again."}
          </p>
          <button
            className="primary-button"
            onClick={() => {
              setHasError(false);
              window.location.href = "/login";
            }}
          >
            Back to Login
          </button>
        </div>
      </section>
    );
  }

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
