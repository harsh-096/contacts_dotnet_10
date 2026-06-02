"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";

const items = [
  { href: "/", label: "Dashboard", icon: "📊" },
  { href: "/projects", label: "Projects", icon: "📁" },
  { href: "/groups", label: "Groups", icon: "👥" },
  { href: "/contacts", label: "Contacts", icon: "📇" },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="hidden w-64 shrink-0 border-r border-slate-200 bg-white md:flex md:flex-col">
      <div className="flex h-16 items-center gap-2 border-b border-slate-100 px-5">
        <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600 text-white shadow-soft">
          <span className="text-base font-bold">CS</span>
        </div>
        <div>
          <p className="text-sm font-semibold text-slate-900">Contact System</p>
          <p className="text-xs text-slate-500">.NET 10 backend</p>
        </div>
      </div>
      <nav className="flex-1 space-y-1 p-3">
        {items.map((item) => {
          const active =
            item.href === "/"
              ? pathname === "/"
              : pathname === item.href || pathname.startsWith(item.href + "/");
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition",
                active
                  ? "bg-brand-50 text-brand-700"
                  : "text-slate-600 hover:bg-slate-100 hover:text-slate-900"
              )}
            >
              <span aria-hidden className="text-base">
                {item.icon}
              </span>
              {item.label}
            </Link>
          );
        })}
      </nav>
      <div className="border-t border-slate-100 p-4 text-xs text-slate-500">
        <p>API: <code className="rounded bg-slate-100 px-1.5 py-0.5">/api/*</code></p>
        <p className="mt-1">Backend: <code className="rounded bg-slate-100 px-1.5 py-0.5">BACKEND_URL</code></p>
      </div>
    </aside>
  );
}
