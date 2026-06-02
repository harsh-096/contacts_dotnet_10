"use client";

import { type HTMLAttributes, type ReactNode } from "react";
import { cn } from "@/lib/utils";

export function Badge({
  children,
  tone = "slate",
  className,
  ...rest
}: { children: ReactNode; tone?: "slate" | "green" | "red" | "amber" | "brand" } & HTMLAttributes<HTMLSpanElement>) {
  const tones: Record<string, string> = {
    slate: "bg-slate-100 text-slate-700",
    green: "bg-emerald-50 text-emerald-700",
    red: "bg-red-50 text-red-700",
    amber: "bg-amber-50 text-amber-700",
    brand: "bg-brand-50 text-brand-700",
  };
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium",
        tones[tone],
        className
      )}
      {...rest}
    >
      {children}
    </span>
  );
}
