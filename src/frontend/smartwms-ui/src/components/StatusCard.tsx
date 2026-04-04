type StatusCardProps = {
  label: string;
  value: string;
  tone?: "neutral" | "good" | "warn";
};

export default function StatusCard({ label, value, tone = "neutral" }: StatusCardProps) {
  return (
    <article className={`status-card ${tone}`}>
      <p>{label}</p>
      <strong>{value}</strong>
    </article>
  );
}
