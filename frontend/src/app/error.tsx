"use client";

import Link from "next/link";
import { useEffect } from "react";
import { Button } from "@/components/ui/Button";

export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <div className="mx-auto max-w-xl py-16 text-center">
      <p className="text-3xl">💥</p>
      <h1 className="mt-3 text-xl font-semibold text-slate-900">
        Something went wrong
      </h1>
      <p className="mt-2 text-sm text-slate-500">
        {error.message || "An unexpected error occurred while rendering this page."}
      </p>
      <div className="mt-6 flex justify-center gap-2">
        <Button variant="secondary" onClick={() => reset()}>
          Try again
        </Button>
        <Link href="/">
          <Button>Go home</Button>
        </Link>
      </div>
    </div>
  );
}
