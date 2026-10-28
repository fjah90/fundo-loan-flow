import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Loan Flow",
  description: "Apply for a loan.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en">
      <body>
        <header>Loan Flow</header>
        <main className="container">{children}</main>
      </body>
    </html>
  );
}
