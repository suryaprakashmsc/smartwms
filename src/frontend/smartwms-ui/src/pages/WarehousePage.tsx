const locations = [
  { code: "A-01-01", zone: "A", state: "Available" },
  { code: "B-02-03", zone: "B", state: "Occupied" }
];

export default function WarehousePage() {
  return (
    <section>
      <p className="eyebrow">Warehouse</p>
      <h2>Bin locations</h2>
      <div className="panel">
        {locations.map((location) => (
          <div key={location.code} className="table-row">
            <span>{location.code}</span>
            <span>Zone {location.zone}</span>
            <strong>{location.state}</strong>
          </div>
        ))}
      </div>
    </section>
  );
}
