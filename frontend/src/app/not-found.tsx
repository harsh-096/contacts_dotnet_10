import Link from "next/link";
import { Button } from "@/components/ui/Button";

export default function NotFound() {
  return (
    <div className="mx-auto max-w-xl py-20 text-center">
      <p className="text-5xl">🧭</p>
      <h1 className="mt-4 text-2xl font-semibold text-slate-900">Page not found</h1>
      <p className="mt-2 text-sm text-slate-500">
        The page you&apos;re looking for doesn&apos;t exist or was moved.
      </p>
      <div className="mt-6 flex justify-center gap-2">
        <Link href="/">
          <Button>Go to dashboard</Button>
        </Link>
        <Link href="/contacts">
          <Button variant="secondary">Browse contacts</Button>
        </Link>
      </div>
    </div>
  );
}
