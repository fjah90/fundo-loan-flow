import { CircleCheck } from "lucide-react";
import Link from "next/link";
import { redirect } from "next/navigation";
import { buttonVariants } from "@/components/ui/button";
import { PageBand, PageBody } from "../page-band";

type SearchParams = Promise<{ [key: string]: string | string[] | undefined }>;

const first = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);

export default async function ApprovedPage({ searchParams }: { searchParams: SearchParams }) {
  const params = await searchParams;
  const applicationId = first(params.applicationId);
  if (!applicationId || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(applicationId)) redirect("/");
  const returning = first(params.returning) === "true";

  return (
    <>
      <PageBand
        title="Application approved"
        icon={
          <span className="grid size-10 shrink-0 place-items-center rounded-full border border-success text-success">
            <CircleCheck className="size-5" aria-hidden="true" />
          </span>
        }
      />
      <PageBody>
        <section className="flex flex-col items-start gap-4 rounded-xl border border-l-2 border-l-success bg-card p-6 sm:p-8">
          <p className="max-w-[65ch] break-words">
            Your application ID is <strong>{applicationId}</strong>.
          </p>
          <p className="max-w-[65ch]">{returning ? "We updated your existing application." : "This is a new application."}</p>
          <Link href="/" className={buttonVariants()}>
            Submit another application
          </Link>
        </section>
      </PageBody>
    </>
  );
}
