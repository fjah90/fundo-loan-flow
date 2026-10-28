# Design System: Loan Flow (web)

UI guide for the loan application form and its two result pages, `/approved` and `/denied`. Colors live only as custom properties in `app/globals.css`; components use Tailwind utilities backed by those tokens (`bg-primary`, `text-muted-foreground`), never raw hex values.

---

## 1. Visual theme and atmosphere

A precise, calm look: thin parallel lines, connection nodes and a deep navy band over a clear working surface. The user hands over an SSN and an amount and waits for a decision, so the form stays plain while the page heading carries the identity.

| Axis | Value | Reading |
|---|---|---|
| Density | 4 / 10 | Form split into short numbered groups; nothing cramped. |
| Variation | 3 / 10 | Fixed single column under a full-width band. Groups and headings carry the hierarchy. |
| Motion | 4 / 10 | CSS only: one staggered entrance in the band, one slow float on a decorative node, hover transitions. |

**Identity:** original. No logo, no isotype, no third-party brand. The only brand element is the text wordmark "Loan Flow".

**Modes:** light and dark with equal weight. The default follows the system setting (`next-themes`, `.dark` class, `disableTransitionOnChange`); a toggle in the header switches it. The navy band forces the dark tokens in both modes.

---

## 2. Palette and roles

### Brand colors

- **Deep navy** `#02143A`: dark background, text in light mode.
- **Slate** `#253446`: secondary text in light mode, borders in dark mode.
- **Electric blue** `#2334FD`: primary in light mode.
- **Circuit cyan** `#39EBEB`: primary in dark mode.
- **Gradient** `linear-gradient(90deg, #39EBEB, #2F95F3, #2334FD)`: lines and accents only (`bg-brand-gradient`).

### Semantic tokens (`app/globals.css`)

| Token | Light | Dark | Role |
|---|---|---|---|
| `background` | `#F6F8FC` | `#02143A` | Page canvas |
| `foreground` | `#02143A` | `#EAF2FF` | Primary text |
| `card` | `#FFFFFF` | `#0A1C42` | Form card and result blocks |
| `muted` | `#E6EBF3` | `#0B1F47` | Secondary surfaces, error summary, hover fill |
| `muted-foreground` | `#253446` | `#A9B6CC` | Secondary text, help text, the `$` prefix |
| `border` | `#D5DCE8` | `#253446` | Dividers and card borders (decorative) |
| `input` | `#6F7F96` | `#6F82A3` | Input, select and outline-button border (functional) |
| `primary` | `#2334FD` | `#39EBEB` | Primary button, nodes, links |
| `primary-foreground` | `#FFFFFF` | `#02143A` | Text on the primary button |
| `ring` | `#2334FD` | `#39EBEB` | Focus outline and focused-field border |
| `success` | `#15803D` | `#4ADE80` | `/approved` accent border, icon and node |
| `destructive` | `#B91C1C` | `#FF8A8A` | Field errors, invalid border, error summary, `/denied` accent |

### Measured contrast (WCAG 2.x)

| Pair | Light | Dark | Allowed use |
|---|---|---|---|
| `foreground` on `background` | 16.97 | 16.02 | Any text |
| `foreground` on `card` | 18.05 | 14.82 | Any text |
| `muted-foreground` on `background` | 11.91 | 8.81 | Secondary text |
| `muted-foreground` on `card` | 12.66 | 8.15 | Secondary text |
| `primary` on `card` | 7.04 | 11.34 | Text, links, nodes |
| `primary-foreground` on `primary` | 7.04 | 12.25 | Button text |
| white on primary at 90% (button hover, light) | 5.91 | n/a | Button text on hover |
| `success` on `card` | 5.02 | 9.58 | Result text and icon |
| `success` on `background` | 4.72 | 10.36 | Result icon in the band |
| `destructive` on `card` | 6.47 | 7.36 | Field errors |
| `destructive` on `muted` | 5.40 | 7.11 | Error summary text and links |
| `destructive` on `background` | 6.09 | 7.95 | Icon in the band, error text |
| `input` on `card` | 4.08 | 4.29 | Functional border (at least 3:1, WCAG 1.4.11) |
| `input` on `background` | 3.83 | 4.64 | Functional border (at least 3:1, WCAG 1.4.11) |
| `ring` on `background` | 6.63 | 11.34 | Focus outline |
| `brand-blue` (`#2F95F3`) on `background` | 2.94 | n/a | **Decoration only.** Never text or a functional border |
| `border` on `background` | 1.30 | 1.42 | **Decoration only.** Never an input border |

The band always uses the dark column. The disabled submit button drops to 60% opacity; disabled controls are exempt.

### Color rules

- The gradient and accents appear only on lines, nodes and the primary button; never as a section background and never behind paragraphs.
- Green and red mark results and errors only, and always come with text and an icon. Color is never the only signal.
- No pure black; the canvas is `#F6F8FC` or `#02143A`.
- One neutral family (blue-tinted).

---

## 3. Typography

- **Family:** Lato through `next/font/google` (weights 100/300/400/700/900, `display: swap`), exposed as `--font-lato` and wired to Tailwind `font-sans`. No other family.
- **Wordmark:** "Loan Flow", weight 700, `tracking-[0.35em]`, with `pl-[0.35em]` so the tracking does not push it off-center optically.
- **Scale:**

| Level | Size | Weight |
|---|---|---|
| Page H1 | `clamp(2rem, 5vw, 3rem)`, `text-balance` | 900 |
| Band intro | `1.125rem` | 400 |
| Group title (`legend`) | `1.125rem` | 700 |
| Label | `0.9375rem` | 400 |
| Body and input | `1rem` (avoids iOS zoom) | 400 |
| Help and error text | `0.875rem` | 400 |

- Wide tracking belongs to the wordmark only. Paragraphs are capped at `65ch`.
- **Not allowed:** another family, decorative italics, gradient text.

---

## 4. Components

Tailwind CSS v4 (`@tailwindcss/postcss`, tokens in `app/globals.css` through `@theme inline`, dark variant `@custom-variant dark (&:is(.dark *))`) plus three shadcn-style components in `components/ui`: `button`, `input`, `label`. Icons come from `lucide-react` at 16-20px. Corner radius is `0.5rem` for controls and `0.75rem` for cards.

- **Header:** full-width, bottom `border`, holding the text wordmark and the theme toggle.
- **Theme toggle:** outline icon button, 44px square, `aria-label="Toggle color theme"`. Sun and moon are swapped by CSS (`dark:hidden` / `hidden dark:block`), so server and client markup match.
- **Page band:** the heading area of every route. It carries the `dark` class (dark tokens in both modes), the H1, an optional intro line and the motifs of section 5.
- **Form (`/`)**
  - One `card` with a `border`, holding a `<form>` bound to a server action.
  - Three `<fieldset>` groups with a `<legend>` and a numbered node: **Personal details** (first name, last name, Social Security number), **Address** (street address, city, state, ZIP code), **Loan** (company, loan amount). Groups are separated by a thin line.
  - Label above, then the control, then optional help, then the error. No floating labels, no placeholders standing in for labels.
  - Every control is `required`. Native validation runs first (`required`, `pattern`, `maxLength`, `inputMode`, `autocomplete`) and the server has the final word. API 400 errors appear under their field, with `aria-invalid="true"` and `aria-describedby` pointing at the help and error text. An invalid control gets a `destructive` border.
  - Field `id` and `name` equal the API's JSON keys (`firstName`, `lastName`, `ssn`, `street`, `city`, `state`, `zipCode`, `company`, `requestedAmount`).
  - After a failed submit the form keeps what you typed, SSN included.
  - **SSN:** `inputMode="numeric"`, `autoComplete="off"`, `pattern="\d{3}-?\d{2}-?\d{4}"`, help text "Format: 123-45-6789". Not masked while typing.
  - **ZIP code:** `inputMode="numeric"`, `pattern="\d{5}(-\d{4})?"`.
  - **State:** native `<select>` with the same classes as the input. First option "Select a state" (empty value, so `required` rejects it), then the 50 states plus DC as "CA — California". No custom combobox.
  - **Loan amount:** `inputMode="decimal"`, `pattern="\d+(\.\d{1,2})?"`, a `$` prefix outside the input (`aria-hidden`) and help text "Any amount greater than $0, up to 2 decimals."
  - **Controls:** 44px high, 1px `input` border, transparent background (the card shows through); focus changes the border to `ring` and adds the outline.
- **Primary button:** `primary` fill, flat, no shadow or glow, 44px minimum height, full width below 640px, auto width from 640px up. One per view. Label "Submit application". While the request is in flight it is disabled and reads "Submitting…". No spinner. Pressing it moves it 1px down.
- **Outline button:** `input` border, `muted` fill on hover. Used for the theme toggle.
- **Error summary:** after a failed submit, a block above the form with a 2px `destructive` left border, `muted` background, a `CircleAlert` icon, `role="alert"` and focus moved to it. A general failure (409, 5xx, API down, timeout) shows "We couldn't submit your application. Please try again." Field errors show as a list of links, each "Label: message" and pointing at its field.
- **`/approved`:** the band holds the H1 "Application approved" with a bordered `success` node containing a `CircleCheck`. Below, a block with a 2px `success` left border: "Your application ID is `<id>`." and either "This is a new application." or "We updated your existing application." A button-styled link, "Submit another application", returns to the form. Opened without an `applicationId` in GUID format, the page redirects to `/`.
- **`/denied`:** the band holds the H1 "Application not approved" with a bordered `destructive` node containing a `CircleAlert`. Below, a block with a 2px `destructive` left border. Each known reason code shows its message once, in order: `STATE_NOT_SERVED` reads "We don't offer loans in your state yet." and `NOT_ELIGIBLE` reads "We can't approve this application." The text never mentions the SSN or any list. Empty or unknown codes fall back to the `NOT_ELIGIBLE` message. A button-styled link, "Back to the form", returns to the form.
- **Loading state:** the button text and its disabled state. No spinner, no modal.

---

## 5. Graphic motifs

All decorative, all `aria-hidden` and `pointer-events-none`, all inside the band.

- **Parallel curves:** an inline SVG with five thin (1.25px) parallel curves at 12% opacity, stroked with the band's `primary` (cyan).
- **Dot grid:** `bg-dots`, a `radial-gradient` of dots in `currentColor` at 8% opacity, faded with `mask-image` toward a corner.
- **Gradient line:** `bg-brand-gradient`, 3px high, closing the band.
- **Nodes:** small bordered circles. Two decorative ones sit in the band (one floats, see section 6); numbered 28px nodes mark the three form groups as steps; the result icons sit in 40px bordered nodes.
- **Navy band:** the `dark` class on the band forces the dark tokens in both themes. It is the only branded surface; there are no section gradients.

---

## 6. Layout and motion

### Layout

- **Band:** full width. Its content and the page body share one centered column, `max-w-[40rem]`, with a `px-4` gutter (`px-6` from 640px).
- **Body:** the form and the result blocks live in that 40rem column; no wide marketing container.
- **Rhythm:** `2rem` between form groups, `1rem` between fields.
- **Grids inside a `fieldset`:** first and last name in 2 columns; city, state and ZIP in 3 columns. Both apply from 640px up; below that, one column.
- Heights use `min-h-dvh`, never `100vh`.
- Mobile first. Check at 360, 768 and 1280px. Horizontal scroll is a critical bug. Touch targets are at least 44px.

### Motion

- **CSS only.** Animate `transform` and `opacity` only.
- **Entrance:** the band content fades in and rises 16px, staggered by 80ms, once on load.
- **Loop:** at most one, a slow 6s float (8px) on a decorative node.
- **Hover and focus:** 150-300ms transitions on color, border and transform (`ease-out`).
- `prefers-reduced-motion: reduce` disables every animation and transition globally.
- Theme changes have no transition (`disableTransitionOnChange`).

---

## 7. Accessibility (minimums)

- One H1 per page, one `<main>`, `lang="en"` on `<html>`.
- Every control has a `<label for>`; groups use `<fieldset>` and `<legend>`. The step nodes inside a legend are `aria-hidden`, so the group name is unchanged.
- Errors are announced: the summary has `role="alert"` and takes focus; each invalid field has `aria-invalid` and `aria-describedby` pointing at its message.
- Contrast meets AA in both modes as measured in section 2; functional borders meet 3:1.
- Focus is always visible: a 2px `ring` outline with a 2px offset. Never `outline: none` without a replacement.
- Decorative SVGs and nodes are `aria-hidden`. The theme toggle has an accessible name.
- The SSN never appears in a URL or a page title. Result URLs carry only an application id and reason codes.

---

## 8. Copy

- English, short sentences, second person ("Your application ID is...").
- Button: "Submit application". No "Get started", no "Unlock", no exclamation marks.
- The UI never explains internal rules: no "blacklist", "flagged" or "fraud".

---

## 9. Anti-patterns (not allowed)

- Emojis in the interface.
- Any logo, isotype or third-party brand name; the only brand element is the text wordmark.
- Gradient as a section background or on paragraph text.
- Neon glow and heavy glassmorphism.
- Cyan or mid-blue as text on the light background; green or red below AA.
- A second font family, or wide tracking outside the wordmark.
- shadcn components beyond `button`, `input` and `label`; a custom select or combobox.
- Multi-step wizard: nine fields fit on one screen.
- Modals for results or errors; spinners and skeletons.
- Animating anything but `transform` and `opacity`; more than one infinite loop.
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
| UI-02 | **Replaced by UI-22.** Light mode only. | Nobody asked for dark mode. | Light and dark. |
| UI-03 | **Replaced by UI-23.** One blue for actions, green for approved, red for denied, all with measured AA contrast. | Minimum needed to tell action from result. | Brand palette. |
| UI-04 | **Replaced by UI-24.** System font stack. | Zero requests, zero layout shift. | `next/font`. |
| UI-05 | **Replaced by UI-25.** Own CSS with custom properties; no Tailwind or UI library. | No dependency beyond Next.js and React. | Tailwind + shadcn. |
| UI-06 | Address split into street, city, state and ZIP. | The brief only requires the state; a separate field lets us validate it. | One free-text field. |
| UI-07 | State as a native `<select>` with 50 states plus DC. | Prevents typos in the field that decides the New York rule. | Free text or combobox. |
| UI-08 | Single-view form in three `fieldset` groups. | Nine fields fit on one screen. | Step-by-step wizard. |
| UI-09 | SSN as `123-45-6789` with optional dashes, no input mask. | Simple native validation; the server normalizes. | Masked input. |
| UI-10 | `/approved` shows the id and whether the application is new or updated. | Makes the returning customer visible in the demo. | Generic message. |
| UI-11 | Neutral copy for `NOT_ELIGIBLE`. | Does not reveal that an SSN is flagged. | Say "blacklist". |
| UI-12 | Errors under the field and also in a `role="alert"` summary. | Screen reader support. | Field errors only. |
| UI-13 | **Replaced by UI-27.** No modals, spinners or entrance animations. | The disabled button already signals the submit. | Spinner. |
| UI-14 | UI and copy in English. | One language across code, UI and docs; US reviewer. | Spanish. |
| UI-15 | Result URLs carry only ids and codes, never PII. | URLs end up in history and logs. | Pass data in the query string. |
| UI-16 | The server action calls the API from the server using `API_URL`. | No CORS, and the API URL stays out of the browser. | `fetch` from the client. |
| UI-17 | `API_URL` defaults to `http://localhost:5080`; compose overrides it with `http://api:8080`. | Works unconfigured locally; one documented port. | Require the variable and fail without it. |
| UI-18 | Redirect contract: approved goes to `/approved?applicationId=<guid>&returning=<true\|false>`, denied to `/denied?reasons=<CODE>,<CODE>`. | Only ids and codes (UI-15); the page does not need `customerId`. | Cookie or session state holding the result. |
| UI-19 | Form field names are the API's JSON keys; a 409, 5xx or unreachable API shows one general error. | One contract, no mapping table; the user cannot fix a 409 or 500 in a field. | Key translation table; per-status messages. |
| UI-20 | On error the action returns the submitted values (SSN included) so the form is not emptied; they travel only in the POST response to the same user. | Retyping nine fields after an error is bad UX. | Controlled client form; withholding the SSN. |
| UI-21 | `/approved` without a valid GUID redirects to `/`; `/denied` with empty or unknown codes shows "We can't approve this application." | Opening a URL by hand must not show an approval with no id or an empty page. | 404 on both; show the raw code. |
| UI-22 | Light and dark with equal weight; default follows the system (`next-themes`, `.dark` class); toggle in the header. | A distinct, deliberate visual identity, and people apply for a loan at any hour. | Light only. |
| UI-23 | Navy `#02143A`, slate `#253446`, electric blue `#2334FD` (primary in light), cyan `#39EBEB` (primary in dark); a cyan-to-blue gradient only on lines and accents; green and red for results tuned to AA in both modes. | A cohesive palette with measured contrast; the gradient stays decoration, never a surface. | A single neutral blue. |
| UI-24 | Lato through `next/font/google` (100/300/400/700/900); the build downloads the font, so `docker compose up --build` needs network access. | One humanist family gives the look without a second typeface; `next/font` ships with Next and self-hosts the files. | System font stack. |
| UI-25 | Tailwind CSS v4 and shadcn/ui (only `button`, `input`, `label`) with `lucide-react`; State stays a native `<select>` (UI-07) styled like the input. | Tokens and states stay in one place; three small components cover the form. | Hand-written CSS. |
| UI-26 | Brand motifs: a navy band as the heading area of each page, an SVG of thin parallel curves, a dot grid, numbered nodes for the three form groups and a gradient line; text wordmark "Loan Flow", no logo. | Gives the pages structure and identity with no foreign branding (UI-01 still holds). | Plain pages without motifs. |
| UI-27 | CSS-only motion: staggered entrance of the band content, one subtle float loop on a decorative node, 150-300 ms hover transitions; `prefers-reduced-motion` disables all of it. Still no modals or spinners. | Polish without blocking anything; the submit state stays the disabled button. | No animation. |
