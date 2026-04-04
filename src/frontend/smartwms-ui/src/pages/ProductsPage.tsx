const products = [
  { sku: "WMS-001", name: "Handheld Scanner", qty: 24 },
  { sku: "WMS-002", name: "Thermal Label Roll", qty: 180 }
];

export default function ProductsPage() {
  return (
    <section>
      <p className="eyebrow">Inventory</p>
      <h2>Products</h2>
      <div className="panel">
        {products.map((product) => (
          <div key={product.sku} className="table-row">
            <span>{product.sku}</span>
            <span>{product.name}</span>
            <strong>{product.qty} units</strong>
          </div>
        ))}
      </div>
    </section>
  );
}
