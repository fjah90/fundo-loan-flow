"use server";

import { redirect } from "next/navigation";

export type FieldName =
  | "firstName"
  | "lastName"
  | "ssn"
  | "street"
  | "city"
  | "state"
  | "zipCode"
  | "company"
  | "requestedAmount";

export type FormState = {
  values: Partial<Record<FieldName, string>>;
  fieldErrors: Partial<Record<FieldName, string>>;
  formError?: string;
  attempt: number;
};

const FIELDS: readonly FieldName[] = [
  "firstName",
  "lastName",
  "ssn",
  "street",
  "city",
  "state",
  "zipCode",
  "company",
  "requestedAmount",
];

const API_URL = process.env.API_URL ?? "http://localhost:5080";
const GENERIC_ERROR = "We couldn't submit your application. Please try again.";

type Outcome =
  | { redirectTo: string }
  | { fieldErrors: FormState["fieldErrors"]; formError?: string };

const isRecord = (v: unknown): v is Record<string, unknown> =>
  typeof v === "object" && v !== null;

const isField = (k: string): k is FieldName => (FIELDS as readonly string[]).includes(k);

async function send(body: string): Promise<Outcome> {
  try {
    const res = await fetch(`${API_URL}/api/loan-applications`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body,
      cache: "no-store",
      signal: AbortSignal.timeout(70_000), // Render free tier wakes the API in 25-60 s (UI-28)
    });
    const json: unknown = await res.json();
    if (!isRecord(json)) return { fieldErrors: {}, formError: GENERIC_ERROR };

    if (res.status === 200) {
      if (json.status === "Approved" && typeof json.applicationId === "string") {
        const q = new URLSearchParams({
          applicationId: json.applicationId,
          returning: String(json.isReturningCustomer === true),
        });
        return { redirectTo: `/approved?${q}` };
      }
      if (json.status === "Denied" && Array.isArray(json.reasons)) {
        const codes = json.reasons.flatMap((r: unknown) =>
          isRecord(r) && typeof r.code === "string" ? [r.code] : [],
        );
        return { redirectTo: `/denied?${new URLSearchParams({ reasons: codes.join(",") })}` };
      }
    }

    if (res.status === 400 && isRecord(json.errors)) {
      const fieldErrors: FormState["fieldErrors"] = {};
      let formError: string | undefined;
      for (const [key, msgs] of Object.entries(json.errors)) {
        const first = Array.isArray(msgs) && typeof msgs[0] === "string" ? msgs[0] : undefined;
        if (isField(key)) {
          if (first) fieldErrors[key] = first;
        } else {
          formError ??= first ?? GENERIC_ERROR;
        }
      }
      if (formError || Object.keys(fieldErrors).length > 0) return { fieldErrors, formError };
    }
  } catch {
    // Network down, timeout or broken JSON: same generic error (UI-19).
  }
  return { fieldErrors: {}, formError: GENERIC_ERROR };
}

export async function submitLoanApplication(
  prev: FormState,
  formData: FormData,
): Promise<FormState> {
  const values: Partial<Record<FieldName, string>> = {};
  for (const f of FIELDS) values[f] = String(formData.get(f) ?? "");

  const rawAmount = (values.requestedAmount ?? "").trim();
  // Number() loses precision past 15 digits: send other input as the raw string and let the API parse or reject it.
  const amount = rawAmount === "" ? null : /^\d{1,13}(\.\d{1,2})?$/.test(rawAmount) ? Number(rawAmount) : rawAmount;
  const body = JSON.stringify({ ...values, requestedAmount: amount });

  const outcome = await send(body);
  if ("redirectTo" in outcome) redirect(outcome.redirectTo);
  return { values, attempt: prev.attempt + 1, ...outcome };
}
