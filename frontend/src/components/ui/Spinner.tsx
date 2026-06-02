"use client";

import { type ReactNode } from "react";
import { cn } from "@/lib/utils";

export function Spinner({ className, size = 16 }: { className?: string; size?: number }) {
  return (
    <span
      role="status"
      aria-label="Loading"
      className={cn(
        "inline-block animate-spin rounded-full border-2 border-current border-t-transparent",
        className
      )}
      style={{ width: size, height: size }}
    />
  );
}

export function FullPageSpinner() {
  return (
    <div className="flex h-[60vh] items-center justify-center text-slate-500">
      <Spinner size={28} />
    </div>
  );
}

export function InlineSpinner({ children }: { children?: ReactNode }) {
  return (
    <span className="inline-flex items-center gap-2 text-slate-500">
      <Spinner size={14} />
      {children}
    </span>
  );
}
