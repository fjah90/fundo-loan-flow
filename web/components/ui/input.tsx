import * as React from "react";
import { cn } from "@/lib/utils";

// Shared with the native State <select> so both controls look identical.
export const inputClass =
  "h-11 w-full min-w-0 rounded-lg border border-input bg-transparent px-3 text-base text-foreground transition-[color,border-color] duration-200 ease-out placeholder:text-muted-foreground focus-visible:border-ring aria-invalid:border-destructive disabled:opacity-60";

export function Input({ className, type, ...props }: React.ComponentProps<"input">) {
  return <input type={type} className={cn(inputClass, className)} {...props} />;
}
