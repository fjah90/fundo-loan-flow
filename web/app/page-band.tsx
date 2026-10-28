import type { ReactNode } from "react";

const CURVE = "M-40 150 C 140 20, 300 270, 500 140 S 760 40, 860 110";
const OFFSETS = [0, 14, 28, 42, 56];

export function PageBand({ icon, title, children }: { icon?: ReactNode; title: string; children?: ReactNode }) {
  return (
    <section className="dark relative isolate overflow-hidden bg-background text-foreground">
      <div aria-hidden="true" className="pointer-events-none absolute inset-0 -z-10 text-primary">
        <svg
          className="absolute inset-0 size-full opacity-[0.12]"
          viewBox="0 0 800 260"
          preserveAspectRatio="xMidYMid slice"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.25"
        >
          {OFFSETS.map((dy) => (
            <path key={dy} d={CURVE} transform={`translate(0 ${dy})`} />
          ))}
        </svg>
        <div className="bg-dots absolute inset-0 opacity-[0.08]" />
        <span className="float absolute top-10 right-[8%] size-2.5 rounded-full border border-primary" />
        <span className="absolute right-[18%] bottom-12 size-1.5 rounded-full border border-primary opacity-60" />
      </div>
      <div className="stagger mx-auto flex w-full max-w-[40rem] flex-col gap-4 px-4 py-12 sm:px-6 sm:py-16">
        <h1 className="flex items-center gap-3 text-[clamp(2rem,5vw,3rem)] leading-tight font-black text-balance">
          {icon}
          {title}
        </h1>
        {children}
      </div>
      <div aria-hidden="true" className="bg-brand-gradient h-[3px] w-full" />
    </section>
  );
}

export function PageBody({ children }: { children: ReactNode }) {
  return <div className="mx-auto w-full max-w-[40rem] px-4 py-10 sm:px-6 sm:py-12">{children}</div>;
}
