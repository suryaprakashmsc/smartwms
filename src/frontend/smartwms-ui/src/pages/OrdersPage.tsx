const orders = [
  { number: "PO-1001", supplier: "Northwind Parts", status: "Draft" },
  { number: "PO-1002", supplier: "Global Packing Co", status: "Approved" }
];

export default function OrdersPage() {
  return (
    <section>
      <p className="eyebrow">Procurement</p>
      <h2>Purchase orders</h2>
      <div className="panel">
        {orders.map((order) => (
          <div key={order.number} className="table-row">
            <span>{order.number}</span>
            <span>{order.supplier}</span>
            <strong>{order.status}</strong>
          </div>
        ))}
      </div>
    </section>
  );
}
