"use client";

import { useActionState, useEffect, useRef, type ReactNode } from "react";
import { CircleAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input, inputClass } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { submitLoanApplication, type FieldName, type FormState } from "./actions";
import { US_STATES } from "./us-states";

const initialState: FormState = { values: {}, fieldErrors: {}, attempt: 0 };

const LABELS: Record<FieldName, string> = {
  firstName: "First name",
  lastName: "Last name",
  ssn: "Social Security number",
  street: "Street address",
  city: "City",
  state: "State",
  zipCode: "ZIP code",
  company: "Company",
  requestedAmount: "Loan amount",
};

const FIELDSET = "m-0 flex min-w-0 flex-col gap-4 border-t p-0";
const LEGEND = "mb-4 flex items-center gap-3 p-0 text-lg font-bold";

// Numbered node that marks each form group as a step.
function Node({ n }: { n: number }) {
  return (
    <span
      aria-hidden="true"
      className="grid size-7 shrink-0 place-items-center rounded-full border border-primary text-sm font-bold text-primary tabular-nums"
    >
      {n}
    </span>
  );
}

const ERROR_ORDER = Object.keys(LABELS) as FieldName[];

type FieldProps = {
  name: FieldName;
  help?: string;
  error?: string;
  children: ReactNode;
};

function Field({ name, help, error, children }: FieldProps) {
  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <Label htmlFor={name}>{LABELS[name]}</Label>
      {children}
      {help && (
        <p className="text-sm text-muted-foreground" id={`${name}-help`}>
          {help}
        </p>
      )}
      {error && (
        <p className="text-sm text-destructive" id={`${name}-error`}>
          {error}
        </p>
      )}
    </div>
  );
}

export function LoanForm() {
  const [state, formAction, isPending] = useActionState(submitLoanApplication, initialState);
  const summaryRef = useRef<HTMLDivElement>(null);

  const errorFields = ERROR_ORDER.filter((f) => state.fieldErrors[f]);
  const hasErrors = Boolean(state.formError) || errorFields.length > 0;

  useEffect(() => {
    if (hasErrors) summaryRef.current?.focus();
  }, [state, hasErrors]);

  // Props shared by every control: id/name = JSON key (UI-19), kept value (UI-20), a11y wiring.
  const control = (name: FieldName, hasHelp = false) => {
    const error = state.fieldErrors[name];
    const described = [hasHelp && `${name}-help`, error && `${name}-error`].filter(Boolean).join(" ");
    return {
      id: name,
      name,
      required: true,
      defaultValue: state.values[name],
      "aria-invalid": error ? true : undefined,
      "aria-describedby": described || undefined,
    };
  };

  return (
    <div className="rounded-xl border bg-card p-4 sm:p-8">
      {hasErrors && (
        <div
          className="mb-6 grid grid-cols-[auto_1fr] gap-x-3 gap-y-2 border-l-2 border-l-destructive bg-muted p-4 text-destructive"
          role="alert"
          tabIndex={-1}
          ref={summaryRef}
        >
          <CircleAlert className="size-5" aria-hidden="true" />
          {state.formError && <p className="col-start-2">{state.formError}</p>}
          {errorFields.length > 0 && (
            <ul className="col-start-2">
              {errorFields.map((f) => (
                <li key={f}>
                  <a href={`#${f}`} className="underline underline-offset-2">
                    {LABELS[f]}: {state.fieldErrors[f]}
                  </a>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      <form key={state.attempt} action={formAction} className="flex flex-col gap-8">
        <fieldset className={FIELDSET}>
          <legend className={LEGEND}>
            <Node n={1} />
            Personal details
          </legend>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field name="firstName" error={state.fieldErrors.firstName}>
              <Input type="text" maxLength={100} autoComplete="given-name" {...control("firstName")} />
            </Field>
            <Field name="lastName" error={state.fieldErrors.lastName}>
              <Input type="text" maxLength={100} autoComplete="family-name" {...control("lastName")} />
            </Field>
          </div>
          <Field name="ssn" help="Format: 123-45-6789" error={state.fieldErrors.ssn}>
            <Input
              type="text"
              inputMode="numeric"
              autoComplete="off"
              pattern="\d{3}-?\d{2}-?\d{4}"
              {...control("ssn", true)}
            />
          </Field>
        </fieldset>

        <fieldset className={FIELDSET}>
          <legend className={LEGEND}>
            <Node n={2} />
            Address
          </legend>
          <Field name="street" error={state.fieldErrors.street}>
            <Input type="text" maxLength={100} autoComplete="address-line1" {...control("street")} />
          </Field>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Field name="city" error={state.fieldErrors.city}>
              <Input type="text" maxLength={100} autoComplete="address-level2" {...control("city")} />
            </Field>
            <Field name="state" error={state.fieldErrors.state}>
              <select className={inputClass} autoComplete="address-level1" {...control("state")}>
                <option value="">Select a state</option>
                {US_STATES.map((s) => (
                  <option key={s.code} value={s.code}>
                    {s.code} — {s.name}
                  </option>
                ))}
              </select>
            </Field>
            <Field name="zipCode" error={state.fieldErrors.zipCode}>
              <Input
                type="text"
                inputMode="numeric"
                autoComplete="postal-code"
                pattern="\d{5}(-\d{4})?"
                {...control("zipCode")}
              />
            </Field>
          </div>
        </fieldset>

        <fieldset className={FIELDSET}>
          <legend className={LEGEND}>
            <Node n={3} />
            Loan
          </legend>
          <Field name="company" error={state.fieldErrors.company}>
            <Input type="text" maxLength={100} autoComplete="organization" {...control("company")} />
          </Field>
          <Field
            name="requestedAmount"
            help="Any amount greater than $0, up to 2 decimals."
            error={state.fieldErrors.requestedAmount}
          >
            <div className="flex items-center gap-2 tabular-nums">
              <span className="text-muted-foreground" aria-hidden="true">
                $
              </span>
              <Input
                type="text"
                inputMode="decimal"
                pattern="\d+(\.\d{1,2})?"
                {...control("requestedAmount", true)}
              />
            </div>
          </Field>
        </fieldset>

        <Button type="submit" className="w-full sm:w-auto" disabled={isPending}>
          {isPending ? "Submitting…" : "Submit application"}
        </Button>
      </form>
    </div>
  );
}
