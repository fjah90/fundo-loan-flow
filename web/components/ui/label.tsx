import * as React from "react";
import { cn } from "@/lib/utils";

export function Label({ className, ...props }: React.ComponentProps<"label">) {
  return <label className={cn("text-[0.9375rem] leading-snug font-normal", className)} {...props} />;
}
