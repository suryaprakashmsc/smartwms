import StatusCard from "../components/StatusCard";

export default function DashboardPage() {
  return (
    <section>
      <p className="eyebrow">Phase 3 Starter</p>
      <h2>Warehouse command center</h2>
      <div className="card-grid">
        <StatusCard label="Inventory service" value="Ready" tone="good" />
        <StatusCard label="Open purchase orders" value="12" tone="neutral" />
        <StatusCard label="Low stock alerts" value="3" tone="warn" />
      </div>
    </section>
  );
}
