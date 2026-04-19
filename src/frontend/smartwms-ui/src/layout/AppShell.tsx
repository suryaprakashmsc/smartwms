import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

const links = [
  { to: "/", label: "Dashboard" },
  { to: "/products", label: "Products" },
  { to: "/orders", label: "Orders" },
  { to: "/warehouse", label: "Warehouse" }
];

export default function AppShell() {
  const auth = useAuth();

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <p className="eyebrow">Smart Inventory Platform</p>
        <h1>SmartWMS</h1>
        <nav>
          {links.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end={link.to === "/"}
              className={({ isActive }) => (isActive ? "nav-link active" : "nav-link")}
            >
              {link.label}
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-footer">
          {auth.userEmail ? <p className="subtle-text">{auth.userEmail}</p> : null}
          <button
            type="button"
            className="secondary-button sidebar-action"
            onClick={() => void auth.logout()}
            disabled={auth.isLoading}
          >
            Sign out
          </button>
        </div>
      </aside>
      <main className="main-content">
        <Outlet />
      </main>
    </div>
  );
}
