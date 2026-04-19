import { missingCognitoConfigKeys } from "../auth/cognitoAuth";
import { useAuth } from "../hooks/useAuth";

export default function LoginPage() {
  const auth = useAuth();
  const hasConfigError = missingCognitoConfigKeys.length > 0;

  const handleLogin = async () => {
    await auth.login();
  };

  return (
    <section className="login-screen">
      <div className="login-card">
        <p className="eyebrow">Security</p>
        <h2>Sign in to SmartWMS</h2>
        <p>Use the AWS Cognito Hosted UI to sign in and receive JWT tokens for API access.</p>
        {auth.error ? <p className="subtle-text error-text">{auth.error.message}</p> : null}
        {hasConfigError ? (
          <p className="subtle-text error-text">
            Missing config: {missingCognitoConfigKeys.join(", ")}
          </p>
        ) : null}
        <button
          className="primary-button"
          onClick={() => void handleLogin()}
          disabled={auth.isLoading || hasConfigError}
        >
          {auth.isLoading ? "Redirecting..." : "Sign in with Cognito"}
        </button>
      </div>
    </section>
  );
}
