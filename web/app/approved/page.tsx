import Link from "next/link";
import { redirect } from "next/navigation";

type SearchParams = Promise<{ [key: string]: string | string[] | undefined }>;

const first = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);

export default async function ApprovedPage({ searchParams }: { searchParams: SearchParams }) {
  const params = await searchParams;
  const applicationId = first(params.applicationId);
  if (!applicationId || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(applicationId)) redirect("/");
  const returning = first(params.returning) === "true";

  return (
    <section className="card result result-approved">
      <h1>
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <circle cx="12" cy="12" r="10" />
          <path d="M8 12l3 3 5-6" />
        </svg>
        Application approved
      </h1>
      <p>
        Your application ID is <strong>{applicationId}</strong>.
      </p>
      <p>{returning ? "We updated your existing application." : "This is a new application."}</p>
      <Link href="/">Submit another application</Link>
    </section>
  );
}
