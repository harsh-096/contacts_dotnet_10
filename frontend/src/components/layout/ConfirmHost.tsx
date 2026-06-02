"use client";

import { useEffect, useState, type ReactNode } from "react";
import { Button } from "@/components/ui/Button";

type AskOptions = {
  title: string;
  message: string;
  confirmLabel?: string;
  danger?: boolean;
};

type ConfirmState = AskOptions & {
  open: boolean;
  resolve?: (v: boolean) => void;
};

// Global, singleton confirm dialog. The host is mounted once in the root
// layout; anywhere in the app can call `confirmDialog({...})` to get a
// Promise<boolean>. Falls back to window.confirm during SSR or before the
// host has mounted.
let askFn: ((opts: AskOptions) => Promise<boolean>) | null = null;

export function confirmDialog(opts: AskOptions): Promise<boolean> {
  if (askFn) return askFn(opts);
  if (typeof window !== "undefined") {
    return Promise.resolve(window.confirm(opts.message));
  }
  return Promise.resolve(false);
}

export function ConfirmHost() {
  const [state, setState] = useState<ConfirmState>({
    open: false,
    title: "",
    message: "",
    confirmLabel: "Confirm",
    danger: false,
  });

  useEffect(() => {
    askFn = (opts) =>
      new Promise<boolean>((resolve) => {
        setState({ ...opts, open: true, resolve });
      });
    return () => {
      askFn = null;
    };
  }, []);

  useEffect(() => {
    if (!state.open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") close(false);
    };
    window.addEventListener("keydown", onKey);
    document.body.style.overflow = "hidden";
    return () => {
      window.removeEventListener("keydown", onKey);
      document.body.style.overflow = "";
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [state.open]);

  function close(value: boolean) {
    state.resolve?.(value);
    setState((s) => ({ ...s, open: false, resolve: undefined }));
  }

  if (!state.open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4" role="dialog" aria-modal="true">
      <div className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm" onClick={() => close(false)} />
      <div className="relative z-10 w-full max-w-md rounded-2xl bg-white p-5 shadow-2xl">
        <h2 className="text-base font-semibold text-slate-900">{state.title}</h2>
        <p className="mt-2 whitespace-pre-line text-sm text-slate-600">{state.message}</p>
        <div className="mt-5 flex items-center justify-end gap-2">
          <Button variant="secondary" onClick={() => close(false)}>
            Cancel
          </Button>
          <Button
            variant={state.danger ? "danger" : "primary"}
            onClick={() => close(true)}
          >
            {state.confirmLabel ?? "Confirm"}
          </Button>
        </div>
      </div>
    </div>
  );
}
