import { LoanForm } from "./loan-form";
import { PageBand, PageBody } from "./page-band";

export default function Home() {
  return (
    <>
      <PageBand title="Apply for a loan">
        <p className="max-w-[65ch] text-lg leading-relaxed text-muted-foreground">Tell us about yourself and the amount you need.</p>
      </PageBand>
      <PageBody>
        <LoanForm />
      </PageBody>
    </>
  );
}
