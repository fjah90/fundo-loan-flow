import { CircleAlert } from "lucide-react";
import Link from "next/link";
import { buttonVariants } from "@/components/ui/button";
import { PageBand, PageBody } from "../page-band";

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
    <>
      <PageBand
        title="Application not approved"
        icon={
          <span className="grid size-10 shrink-0 place-items-center rounded-full border border-destructive text-destructive">
            <CircleAlert className="size-5" aria-hidden="true" />
          </span>
        }
      />
      <PageBody>
        <section className="flex flex-col items-start gap-4 rounded-xl border border-l-2 border-l-destructive bg-card p-6 sm:p-8">
          {messages.map((m) => (
            <p key={m} className="max-w-[65ch]">
              {m}
            </p>
          ))}
          <Link href="/" className={buttonVariants()}>
            Back to the form
          </Link>
        </section>
      </PageBody>
    </>
  );
}
