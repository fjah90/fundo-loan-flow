"use client";

import { useActionState, useEffect, useRef, type ReactNode } from "react";
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

const ERROR_ORDER = Object.keys(LABELS) as FieldName[];

type FieldProps = {
  name: FieldName;
  help?: string;
  error?: string;
  children: ReactNode;
};

function Field({ name, help, error, children }: FieldProps) {
  return (
    <div className="field">
      <label htmlFor={name}>{LABELS[name]}</label>
      {children}
      {help && (
        <p className="help" id={`${name}-help`}>
          {help}
        </p>
      )}
      {error && (
        <p className="field-error" id={`${name}-error`}>
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
    <div className="card">
      {hasErrors && (
        <div className="error-summary" role="alert" tabIndex={-1} ref={summaryRef}>
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="8" x2="12" y2="12" />
            <line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
          {state.formError && <p>{state.formError}</p>}
          {errorFields.length > 0 && (
            <ul>
              {errorFields.map((f) => (
                <li key={f}>
                  <a href={`#${f}`}>
                    {LABELS[f]}: {state.fieldErrors[f]}
                  </a>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      <form key={state.attempt} action={formAction}>
        <fieldset>
          <legend>Personal details</legend>
          <div className="grid-2">
            <Field name="firstName" error={state.fieldErrors.firstName}>
              <input type="text" maxLength={100} autoComplete="given-name" {...control("firstName")} />
            </Field>
            <Field name="lastName" error={state.fieldErrors.lastName}>
              <input type="text" maxLength={100} autoComplete="family-name" {...control("lastName")} />
            </Field>
          </div>
          <Field name="ssn" help="Format: 123-45-6789" error={state.fieldErrors.ssn}>
            <input
              type="text"
              inputMode="numeric"
              autoComplete="off"
              pattern="\d{3}-?\d{2}-?\d{4}"
              {...control("ssn", true)}
            />
          </Field>
        </fieldset>

        <fieldset>
          <legend>Address</legend>
          <Field name="street" error={state.fieldErrors.street}>
            <input type="text" maxLength={100} autoComplete="address-line1" {...control("street")} />
          </Field>
          <div className="grid-3">
            <Field name="city" error={state.fieldErrors.city}>
              <input type="text" maxLength={100} autoComplete="address-level2" {...control("city")} />
            </Field>
            <Field name="state" error={state.fieldErrors.state}>
              <select autoComplete="address-level1" {...control("state")}>
                <option value="">Select a state</option>
                {US_STATES.map((s) => (
                  <option key={s.code} value={s.code}>
                    {s.code} — {s.name}
                  </option>
                ))}
              </select>
            </Field>
            <Field name="zipCode" error={state.fieldErrors.zipCode}>
              <input
                type="text"
                inputMode="numeric"
                autoComplete="postal-code"
                pattern="\d{5}(-\d{4})?"
                {...control("zipCode")}
              />
            </Field>
          </div>
        </fieldset>

        <fieldset>
          <legend>Loan</legend>
          <Field name="company" error={state.fieldErrors.company}>
            <input type="text" maxLength={100} autoComplete="organization" {...control("company")} />
          </Field>
          <Field
            name="requestedAmount"
            help="Any amount greater than $0, up to 2 decimals."
            error={state.fieldErrors.requestedAmount}
          >
            <div className="amount">
              <span className="prefix" aria-hidden="true">
                $
              </span>
              <input
                type="text"
                inputMode="decimal"
                pattern="\d+(\.\d{1,2})?"
                {...control("requestedAmount", true)}
              />
            </div>
          </Field>
        </fieldset>

        <button type="submit" className="button-primary" disabled={isPending}>
          {isPending ? "Submitting…" : "Submit application"}
        </button>
      </form>
    </div>
  );
}
