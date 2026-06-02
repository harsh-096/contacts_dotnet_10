"use client";

import { type ReactNode } from "react";
import { cn } from "@/lib/utils";

type Tone = "info" | "error" | "success" | "warning";

const toneClasses: Record<Tone, string> = {
  info: "bg-brand-50 text-brand-800 border-brand-100",
  error: "bg-red-50 text-red-800 border-red-100",
  success: "bg-emerald-50 text-emerald-800 border-emerald-100",
  warning: "bg-amber-50 text-amber-800 border-amber-100",
};

const icons: Record<Tone, ReactNode> = {
  info: "ⓘ",
  error: "⚠",
  success: "✓",
  warning: "!",
};

export function Alert({
  tone = "info",
  title,
  children,
  className,
  onClose,
}: {
  tone?: Tone;
  title?: string;
  children?: ReactNode;
  className?: string;
  onClose?: () => void;
}) {
  return (
    <div
      role="alert"
      className={cn(
        "flex items-start gap-3 rounded-xl border px-4 py-3 text-sm",
        toneClasses[tone],
        className
      )}
    >
      <span className="mt-0.5 inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-white/60 text-xs font-bold">
        {icons[tone]}
      </span>
      <div className="flex-1">
        {title && <p className="font-medium">{title}</p>}
        {children && <div className={cn("whitespace-pre-line", title && "mt-0.5")}>{children}</div>}
      </div>
      {onClose && (
        <button
          type="button"
          aria-label="Dismiss"
          onClick={onClose}
          className="ml-2 text-current/70 hover:opacity-80"
        >
          ×
        </button>
      )}
    </div>
  );
}
