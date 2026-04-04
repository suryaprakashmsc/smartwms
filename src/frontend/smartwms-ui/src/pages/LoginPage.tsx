import { useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

export default function LoginPage() {
  const auth = useAuth();
  const navigate = useNavigate();

  const handleLogin = () => {
    auth.login();
    navigate("/");
  };

  return (
    <section className="login-screen">
      <div className="login-card">
        <p className="eyebrow">Security Demo</p>
        <h2>Sign in to SmartWMS</h2>
        <p>Placeholder flow for Cognito and JWT integration in Phase 3.</p>
        <button className="primary-button" onClick={handleLogin}>
          Start demo session
        </button>
      </div>
    </section>
  );
}
