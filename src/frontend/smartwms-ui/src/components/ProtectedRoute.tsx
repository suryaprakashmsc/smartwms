import { Navigate } from "react-router-dom";
import type { ReactNode } from "react";

type ProtectedRouteProps = {
  isAuthenticated: boolean;
  isLoading: boolean;
  children: ReactNode;
};

export default function ProtectedRoute({ isAuthenticated, isLoading, children }: ProtectedRouteProps) {
  if (isLoading) {
    return (
      <section className="login-screen">
        <div className="login-card">
          <p className="eyebrow">Security</p>
          <h2>Checking your session</h2>
          <p>Completing secure sign-in with AWS Cognito.</p>
        </div>
      </section>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
