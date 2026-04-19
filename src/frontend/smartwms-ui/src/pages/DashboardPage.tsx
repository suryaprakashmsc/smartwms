import StatusCard from "../components/StatusCard";
import { useInventoryDashboard } from "../hooks/useInventoryDashboard";

const compactNumberFormatter = new Intl.NumberFormat("en-US", {
  notation: "compact",
  maximumFractionDigits: 1
});

const currencyFormatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  maximumFractionDigits: 0
});

export default function DashboardPage() {
  const dashboard = useInventoryDashboard();

  const inventoryHealthValue = dashboard.isError
    ? "Unavailable"
    : dashboard.isLoading
      ? "Loading"
      : "Connected";

  const inventoryHealthTone = dashboard.isError
    ? "warn"
    : dashboard.isLoading
      ? "neutral"
      : "good";

  return (
    <section>
      <p className="eyebrow">Inventory API</p>
      <h2>Warehouse command center</h2>
      <div className="card-grid">
        <StatusCard label="Inventory service" value={inventoryHealthValue} tone={inventoryHealthTone} />
        <StatusCard
          label="Active SKUs"
          value={dashboard.isLoading ? "..." : dashboard.metrics.activeSkus.toString()}
          tone="neutral"
        />
        <StatusCard
          label="On-hand units"
          value={dashboard.isLoading ? "..." : compactNumberFormatter.format(dashboard.metrics.totalUnits)}
          tone="neutral"
        />
        <StatusCard
          label="Low stock alerts"
          value={dashboard.isLoading ? "..." : dashboard.metrics.lowStockCount.toString()}
          tone={dashboard.metrics.lowStockCount > 0 ? "warn" : "good"}
        />
        <StatusCard
          label="Inventory value"
          value={dashboard.isLoading ? "..." : currencyFormatter.format(dashboard.metrics.inventoryValue)}
          tone="good"
        />
      </div>

      <div className="panel dashboard-panel">
        <div className="status-line">
          <h3 className="panel-title">Low stock watchlist</h3>
          <button
            type="button"
            className="secondary-button"
            onClick={() => void dashboard.refetch()}
            disabled={dashboard.isFetching}
          >
            {dashboard.isFetching ? "Refreshing..." : "Refresh"}
          </button>
        </div>

        {dashboard.isError ? (
          <p className="subtle-text">{dashboard.error.message}</p>
        ) : dashboard.lowStockProducts.length === 0 ? (
          <p className="subtle-text">No low stock products right now.</p>
        ) : (
          dashboard.lowStockProducts.slice(0, 6).map((product) => (
            <div key={product.id} className="table-row watchlist-row">
              <span>{product.sku}</span>
              <span>{product.name}</span>
              <strong>
                {product.quantityOnHand} / {product.reorderLevel}
              </strong>
            </div>
          ))
        )}
      </div>
    </section>
  );
}
