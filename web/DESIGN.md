# Design System: Loan Flow (web)

UI guide for the loan application form and its two result pages, `/approved` and `/denied`. Colors live only as custom properties in `app/globals.css`. Components use `var(--...)`, never raw hex values.

---

## 1. Visual theme and atmosphere

Someone hands over an SSN and an amount, then waits for a decision. No marketing: it is one form and two result pages.

One column, one action color, no decoration beyond what the form needs.

| Axis | Value | Reading |
|---|---|---|
| Density | 4 / 10 | Form split into short sections; nothing cramped. |
| Variation | 2 / 10 | Fixed single-column layout. Headings and groups carry the hierarchy, not the composition. |
| Motion | 1 / 10 | Hover and focus transitions only. No entrance animations. |

**Identity:** neutral and original. No Fundo logo, name or colors: this is a demo, not a piece of their brand.

**Mode:** light only. Dark mode is out of scope (see section 9).

---

## 2. Palette and roles

### Tokens (`app/globals.css`)

| Token | Value | Role |
|---|---|---|
| `--bg` | `#F7F8FA` | Page canvas |
| `--surface` | `#FFFFFF` | Form card, result cards, input background |
| `--fg` | `#111827` | Primary text |
| `--muted` | `#4B5563` | Secondary text, help text, the `$` prefix |
| `--border` | `#E5E7EB` | Card border (decorative) |
| `--input-border` | `#6B7280` | Input and select border (functional) |
| `--primary` | `#1D4ED8` | Primary button, links, focus ring |
| `--primary-hover` | `#1E40AF` | Primary button on hover and active |
| `--on-primary` | `#FFFFFF` | Text on the primary button |
| `--success` | `#15803D` | `/approved` accent border and icon |
| `--success-bg` | `#F0FDF4` | Reserved success background; no component uses it yet |
| `--danger` | `#B91C1C` | Field errors, invalid-field border, error summary, `/denied` accent |
| `--danger-bg` | `#FEF2F2` | Error summary background |
| `--radius` | `0.5rem` | Corner radius for cards, inputs and the button |
| `--gutter` | `1rem`, `1.5rem` from 768px | Horizontal page and card padding |

### Measured contrast (WCAG 2.x)

| Pair | Ratio | Allowed use |
|---|---|---|
| `--fg` on `--bg` | 16.69 | Any text |
| `--muted` on `--surface` | 7.56 | Secondary text |
| `--primary` on `--surface` | 6.70 | Text and links |
| `--on-primary` on `--primary` | 6.70 | Button text |
| `--on-primary` on `--primary-hover` | 8.72 | Button text on hover |
| `--input-border` on `--surface` | 4.83 | Functional border (at least 3:1, WCAG 1.4.11) |
| `--danger` on `--surface` | 6.47 | Field error messages |
| `--danger` on `--danger-bg` | 5.91 | Error summary text and links |
| `--success` on `--success-bg` | 4.79 | Success text on the tinted background |
| `--border` on `--surface` | 1.24 | **Decoration only.** Never an input border |

The disabled submit button drops to 60% opacity. Disabled controls are exempt from contrast requirements.

### Color rules

- One action color (`--primary`) and one primary button per view.
- Green and red mark results and errors only. They always come with text; the result pages and the error summary add an icon. Color is never the only signal.
- No pure black or pure white as the page background.
- One neutral family (cool grays).

---

## 3. Typography

- **Family:** system stack, no web fonts: `system-ui, -apple-system, "Segoe UI", Roboto, sans-serif`. Zero requests, zero layout shift. The amount field uses `font-variant-numeric: tabular-nums`.
- **Scale:**

| Level | Size | Weight |
|---|---|---|
| Page H1 | `clamp(1.5rem, 4vw, 2rem)` | 700, `text-wrap: balance` |
| Group title (`legend`) | `1rem` | 600 |
| Label | `0.9375rem` | 500 |
| Body and input | `1rem` (16px, avoids iOS zoom) | 400 |
| Help and error text | `0.875rem` | 400 |

- Body `line-height: 1.5`; paragraphs capped at `65ch`.
- Help text uses `--muted`. Error text uses `--danger`.
- **Not allowed:** another family, decorative italics, all caps in paragraphs.

---

## 4. Components

No component library and no Tailwind: semantic HTML plus one `globals.css` built on custom properties. The only dependencies are Next.js and React. Base radius is `--radius`.

- **Form (`/`)**
  - One `--surface` card with a `--border` border, holding a `<form>` bound to a server action.
  - Three `<fieldset>` groups with a `<legend>`: **Personal details** (first name, last name, Social Security number), **Address** (street address, city, state, ZIP code), **Loan** (company, loan amount).
  - Label above, then the control, then optional help, then the error. No floating labels, no placeholders standing in for labels.
  - Every control is `required`. Native validation runs first (`required`, `pattern`, `maxLength`, `inputMode`, `autocomplete`) and the server has the final word. API 400 errors appear under their field, with `aria-invalid="true"` on the control and `aria-describedby` pointing at the help and error text.
  - Field `id` and `name` equal the API's JSON keys (`firstName`, `lastName`, `ssn`, `street`, `city`, `state`, `zipCode`, `company`, `requestedAmount`), so each 400 error lands on its field without a mapping.
  - After a failed submit the form keeps what you typed, SSN included, so you do not retype nine fields.
  - **SSN:** `inputMode="numeric"`, `autoComplete="off"`, `pattern="\d{3}-?\d{2}-?\d{4}"`, help text "Format: 123-45-6789". Not masked while typing.
  - **ZIP code:** `inputMode="numeric"`, `pattern="\d{5}(-\d{4})?"`.
  - **State:** native `<select>`. The first option is "Select a state" (empty value, so `required` rejects it), followed by the 50 states plus DC as "CA — California". No custom combobox.
  - **Loan amount:** `inputMode="decimal"`, `pattern="\d+(\.\d{1,2})?"`, a `$` prefix outside the input (`aria-hidden`) and help text "Any amount greater than $0, up to 2 decimals."
- **Primary button:** `--primary` fill, `--on-primary` text, 44px minimum height (`2.75rem`), full width below 640px and auto width from 640px up. Label "Submit application". While the request is in flight it is disabled and reads "Submitting…" (`isPending` from `useActionState`). No spinner.
- **Error summary:** after a failed submit, a block above the form with a 3px `--danger` left border, `--danger-bg` background, an alert-circle icon, `role="alert"` and focus moved to it. A general failure (409, 5xx, API down, timeout) shows "We couldn't submit your application. Please try again." Field errors show as a list of links, each formatted "Label: message" and pointing at its field.
- **`/approved`:** a card with a 3px `--success` left border and a check icon (inline SVG, `aria-hidden`) in the H1 "Application approved". Below it: "Your application ID is `<id>`." and either "This is a new application." or "We updated your existing application." A link, "Submit another application", returns to the form. Opened without an `applicationId` in GUID format, the page redirects to `/`.
- **`/denied`:** a card with a 3px `--danger` left border, an alert-circle icon and the H1 "Application not approved". Each known reason code shows its message once, in order: `STATE_NOT_SERVED` reads "We don't offer loans in your state yet." and `NOT_ELIGIBLE` reads "We can't approve this application." The text never mentions the SSN or any list. Empty or unknown codes fall back to the `NOT_ELIGIBLE` message. A link, "Back to the form", returns to the form.
- **Header:** the product name as bold text ("Loan Flow"). No navigation, no theme toggle.
- **Icons:** two inline SVGs (check, alert circle) at 20px. No icon library.

---

## 5. Layout

- **Container:** one centered column, `max-width: 40rem`, side gutter `--gutter` (`1rem` on mobile, `1.5rem` from 768px). Padding is `1.5rem` above and `3rem` below.
- **Rhythm:** `2rem` between form groups, `1rem` between fields.
- **Grids inside a `fieldset`:** first and last name in 2 columns; city, state and ZIP in 3 columns. Both apply from 640px up. Below that, everything is one column.
- Heights use `min-height: 100dvh`, never `100vh`.

### Responsive

- Mobile first. Check at 360, 768 and 1280px.
- Horizontal scroll on mobile is a critical bug.

---

## 6. Motion and interaction

- **CSS only.** Transitions last 150ms: `border-color` and `box-shadow` on inputs and selects, `background-color` on the primary button. No animation libraries.
- Focus is always visible: `:focus-visible { outline: 2px solid var(--primary); outline-offset: 2px; }`.
- `prefers-reduced-motion: reduce` turns all transitions off.

---

## 7. Accessibility (minimums)

- One H1 per page, one `<main>`, `lang="en"` on `<html>`.
- Every control has a `<label for>`; groups use `<fieldset>` and `<legend>`.
- Errors are announced: the summary has `role="alert"` and takes focus, and each invalid field has `aria-invalid` and `aria-describedby` pointing at its message.
- Contrast meets AA as measured in section 2. Inputs, selects and the submit button are at least 44px tall (`2.75rem`); links are inline text.
- The SSN never appears in a URL or a page title. Result URLs carry only an application id and reason codes.

---

## 8. Copy

- English, short sentences, second person ("Your application ID is...").
- Button: "Submit application". No "Get started", no "Unlock", no exclamation marks.
- The UI never explains internal rules: no "blacklist", "flagged" or "fraud".

---

## 9. Anti-patterns (not allowed)

- Emojis in the interface.
- Fundo's logo, name or colors.
- UI libraries, Tailwind, web fonts or icon libraries.
- Dark mode, a theme toggle or i18n: nobody asked for them.
- Multi-step wizard: nine fields fit on one screen.
- Modals for results or errors.
- Generic spinners, skeletons and entrance animations.
- Floating labels, or placeholders used as labels.
- Color as the only status signal.
- Invented data: testimonials, rates, "approved in 5 minutes".
- Raw hex values in components.

---

## 10. Decision log

The brief gives no visual guide. It asks for the form fields (first name, last name, address with state, company, amount and SSN), a denied page that shows the reason, and a UI that is clear, functional and well considered. It penalizes over-engineering. Everything below is a choice made beyond that.

| ID | Decision | Why | Rejected alternative |
|---|---|---|---|
| UI-01 | Neutral identity; no Fundo logo, name or colors. | It is a demo, not a piece of their brand. | Imitate the brand. |
| UI-02 | Light mode only. | Nobody asked for dark mode. | Light and dark. |
| UI-03 | One blue for actions, green for approved, red for denied, all with measured AA contrast. | Minimum needed to tell action from result. | Brand palette. |
| UI-04 | System font stack. | Zero requests, zero layout shift. | `next/font`. |
| UI-05 | Own CSS with custom properties; no Tailwind or UI library. | No dependency beyond Next.js and React. | Tailwind + shadcn. |
| UI-06 | Address split into street, city, state and ZIP. | The brief only requires the state; a separate field lets us validate it. | One free-text field. |
| UI-07 | State as a native `<select>` with 50 states plus DC. | Prevents typos in the field that decides the New York rule. | Free text or combobox. |
| UI-08 | Single-view form in three `fieldset` groups. | Nine fields fit on one screen. | Step-by-step wizard. |
| UI-09 | SSN as `123-45-6789` with optional dashes, no input mask. | Simple native validation; the server normalizes. | Masked input. |
| UI-10 | `/approved` shows the id and whether the application is new or updated. | Makes the returning customer visible in the demo. | Generic message. |
| UI-11 | Neutral copy for `NOT_ELIGIBLE`. | Does not reveal that an SSN is flagged. | Say "blacklist". |
| UI-12 | Errors under the field and also in a `role="alert"` summary. | Screen reader support. | Field errors only. |
| UI-13 | No modals, spinners or entrance animations. | The disabled button already signals the submit. | Spinner. |
| UI-14 | UI and copy in English. | One language across code, UI and docs; US reviewer. | Spanish. |
| UI-15 | Result URLs carry only ids and codes, never PII. | URLs end up in history and logs. | Pass data in the query string. |
| UI-16 | The server action calls the API from the server using `API_URL`. | No CORS, and the API URL stays out of the browser. | `fetch` from the client. |
| UI-17 | `API_URL` defaults to `http://localhost:5080`; compose overrides it with `http://api:8080`. | Works unconfigured locally; one documented port. | Require the variable and fail without it. |
| UI-18 | Redirect contract: approved goes to `/approved?applicationId=<guid>&returning=<true\|false>`, denied to `/denied?reasons=<CODE>,<CODE>`. | Only ids and codes (UI-15); the page does not need `customerId`. | Cookie or session state holding the result. |
| UI-19 | Form field names are the API's JSON keys; a 409, 5xx or unreachable API shows one general error. | One contract, no mapping table; the user cannot fix a 409 or 500 in a field. | Key translation table; per-status messages. |
| UI-20 | On error the action returns the submitted values (SSN included) so the form is not emptied; they travel only in the POST response to the same user. | Retyping nine fields after an error is bad UX. | Controlled client form; withholding the SSN. |
| UI-21 | `/approved` without a valid GUID redirects to `/`; `/denied` with empty or unknown codes shows "We can't approve this application." | Opening a URL by hand must not show an approval with no id or an empty page. | 404 on both; show the raw code. |
