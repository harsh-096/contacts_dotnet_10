"use client";

import { type ReactNode } from "react";
import { describeError } from "@/lib/errors";
import { Alert } from "@/components/ui/Alert";
import { Button } from "@/components/ui/Button";
import { useRouter } from "next/navigation";

export function ErrorAlert({ error }: { error: unknown }) {
  if (!error) return null;
  return (
    <Alert tone="error" title="Something went wrong">
      {describeError(error)}
    </Alert>
  );
}

export function PageError({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const router = useRouter();
  return (
    <div className="mx-auto max-w-2xl">
      <Alert tone="error" title="We couldn't load this page">
        {describeError(error)}
      </Alert>
      <div className="mt-4 flex gap-2">
        {onRetry && (
          <Button variant="secondary" onClick={onRetry}>
            Try again
          </Button>
        )}
        <Button variant="primary" onClick={() => router.refresh()}>
          Reload
        </Button>
      </div>
    </div>
  );
}

export function Section({
  title,
  description,
  actions,
  children,
}: {
  title: ReactNode;
  description?: ReactNode;
  actions?: ReactNode;
  children: ReactNode;
}) {
  return (
    <section className="rounded-2xl border border-slate-200 bg-white shadow-soft">
      <header className="flex flex-col gap-1 border-b border-slate-100 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-base font-semibold text-slate-900">{title}</h2>
          {description && <p className="mt-0.5 text-sm text-slate-500">{description}</p>}
        </div>
        {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
      </header>
      <div className="px-5 py-4">{children}</div>
    </section>
  );
}
