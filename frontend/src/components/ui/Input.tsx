"use client";

import { forwardRef, type InputHTMLAttributes, type SelectHTMLAttributes, type TextareaHTMLAttributes, type ReactNode } from "react";
import { cn } from "@/lib/utils";

interface FieldProps {
  label?: string;
  hint?: string;
  error?: string;
  required?: boolean;
  className?: string;
  children: ReactNode;
}

export function Field({ label, hint, error, required, className, children }: FieldProps) {
  return (
    <label className={cn("block", className)}>
      {label && (
        <span className="mb-1 inline-flex items-center gap-1 text-sm font-medium text-slate-700">
          {label}
          {required && <span className="text-red-500">*</span>}
        </span>
      )}
      {children}
      {error ? (
        <span className="mt-1 block text-xs text-red-600">{error}</span>
      ) : hint ? (
        <span className="mt-1 block text-xs text-slate-500">{hint}</span>
      ) : null}
    </label>
  );
}

const baseInput =
  "block w-full rounded-lg border bg-white px-3 py-2 text-sm text-slate-900 placeholder:text-slate-400 transition focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent disabled:bg-slate-100 disabled:cursor-not-allowed";

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement> & { invalid?: boolean }>(
  function Input({ className, invalid, ...rest }, ref) {
    return (
      <input
        ref={ref}
        className={cn(
          baseInput,
          "h-10",
          invalid
            ? "border-red-400 focus:ring-red-400"
            : "border-slate-200 hover:border-slate-300",
          className
        )}
        {...rest}
      />
    );
  }
);

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement> & { invalid?: boolean }>(
  function Textarea({ className, invalid, ...rest }, ref) {
    return (
      <textarea
        ref={ref}
        className={cn(
          baseInput,
          "min-h-[88px] py-2",
          invalid
            ? "border-red-400 focus:ring-red-400"
            : "border-slate-200 hover:border-slate-300",
          className
        )}
        {...rest}
      />
    );
  }
);

export const Select = forwardRef<HTMLSelectElement, SelectHTMLAttributes<HTMLSelectElement> & { invalid?: boolean }>(
  function Select({ className, invalid, children, ...rest }, ref) {
    return (
      <select
        ref={ref}
        className={cn(
          baseInput,
          "h-10 pr-8",
          invalid
            ? "border-red-400 focus:ring-red-400"
            : "border-slate-200 hover:border-slate-300",
          className
        )}
        {...rest}
      >
        {children}
      </select>
    );
  }
);

export function Checkbox({
  className,
  label,
  ...rest
}: { className?: string; label?: string } & InputHTMLAttributes<HTMLInputElement>) {
  return (
    <label className="inline-flex items-center gap-2 text-sm text-slate-700 select-none">
      <input
        type="checkbox"
        className={cn(
          "h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500",
          className
        )}
        {...rest}
      />
      {label && <span>{label}</span>}
    </label>
  );
}
