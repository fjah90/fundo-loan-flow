import Link from "next/link";

type SearchParams = Promise<{ [key: string]: string | string[] | undefined }>;

const MESSAGES: Record<string, string> = {
  STATE_NOT_SERVED: "We don't offer loans in your state yet.",
  NOT_ELIGIBLE: "We can't approve this application.",
};
const FALLBACK = MESSAGES.NOT_ELIGIBLE;

export default async function DeniedPage({ searchParams }: { searchParams: SearchParams }) {
  const { reasons } = await searchParams;
  const raw = Array.isArray(reasons) ? reasons[0] : reasons;
  const known = [...new Set((raw ?? "").split(",").filter((c) => Object.hasOwn(MESSAGES, c)))];
  const messages = known.length > 0 ? known.map((c) => MESSAGES[c]) : [FALLBACK];

  return (
    <section className="card result result-denied">
      <h1>
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <circle cx="12" cy="12" r="10" />
          <line x1="12" y1="8" x2="12" y2="12" />
          <line x1="12" y1="16" x2="12.01" y2="16" />
        </svg>
        Application not approved
      </h1>
      {messages.map((m) => (
        <p key={m}>{m}</p>
      ))}
      <Link href="/">Back to the form</Link>
    </section>
  );
}
