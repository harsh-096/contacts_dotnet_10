"use client";

import Link from "next/link";
import { useMemo } from "react";
import { ContactsApi, GroupsApi, ProjectsApi } from "@/lib/api";
import { useAsync } from "@/lib/hooks";
import { PageHeader } from "@/components/layout/PageHeader";
import { PageError } from "@/components/layout/Page";
import { FullPageSpinner, Spinner } from "@/components/ui/Spinner";
import { Badge } from "@/components/ui/Badge";
import { initials, formatDate, formatPhoneDisplay } from "@/lib/format";

export default function DashboardPage() {
  const projects = useAsync(["projects"], () => ProjectsApi.list());
  const groups = useAsync(["groups"], () => GroupsApi.list());
  const contacts = useAsync(["contacts"], () => ContactsApi.list());

  const loading = projects.loading || groups.loading || contacts.loading;
  const error = projects.error || groups.error || contacts.error;

  const recentContacts = useMemo(
    () => (contacts.data ?? []).slice(0, 5),
    [contacts.data]
  );

  const subscribedCount = useMemo(
    () => (contacts.data ?? []).filter((c) => c.is_subscribed).length,
    [contacts.data]
  );

  if (loading && !contacts.data) {
    return <FullPageSpinner />;
  }

  if (error && !contacts.data) {
    return <PageError error={error} onRetry={() => { projects.refresh(); groups.refresh(); contacts.refresh(); }} />;
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <PageHeader
        title="Dashboard"
        description="Overview of your projects, groups and contacts."
      />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Projects" value={projects.data?.length ?? 0} href="/projects" icon="📁" />
        <StatCard label="Groups" value={groups.data?.length ?? 0} href="/groups" icon="👥" />
        <StatCard label="Contacts" value={contacts.data?.length ?? 0} href="/contacts" icon="📇" />
        <StatCard
          label="Subscribed"
          value={subscribedCount}
          href="/contacts"
          icon="🔔"
          tone="brand"
        />
      </div>

      <section className="rounded-2xl border border-slate-200 bg-white shadow-soft">
        <header className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
          <div>
            <h2 className="text-base font-semibold text-slate-900">Recent contacts</h2>
            <p className="mt-0.5 text-sm text-slate-500">
              {contacts.data?.length
                ? `Latest ${recentContacts.length} of ${contacts.data.length}`
                : "No contacts yet"}
            </p>
          </div>
          <Link
            href="/contacts"
            className="text-sm font-medium text-brand-600 hover:text-brand-700"
          >
            View all →
          </Link>
        </header>
        {contacts.loading ? (
          <div className="flex h-32 items-center justify-center text-slate-400">
            <Spinner size={20} />
          </div>
        ) : recentContacts.length === 0 ? (
          <div className="px-5 py-10 text-center text-sm text-slate-500">
            No contacts yet. <Link href="/contacts/new" className="text-brand-600 hover:underline">Add the first one</Link>.
          </div>
        ) : (
          <ul className="divide-y divide-slate-100">
            {recentContacts.map((c) => (
              <li key={c.contact_id} className="flex items-center gap-3 px-5 py-3">
                <div className="flex h-9 w-9 items-center justify-center rounded-full bg-brand-100 text-sm font-semibold text-brand-700">
                  {initials(c.first_name, c.last_name)}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-slate-900">
                    {c.first_name} {c.last_name}
                  </p>
                  <p className="truncate text-xs text-slate-500">
                    {formatPhoneDisplay(c.country_code, c.national_number)} · {formatDate(c.created_date)}
                  </p>
                </div>
                {c.is_subscribed ? (
                  <Badge tone="green">Subscribed</Badge>
                ) : (
                  <Badge tone="slate">Unsubscribed</Badge>
                )}
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

function StatCard({
  label,
  value,
  icon,
  href,
  tone = "slate",
}: {
  label: string;
  value: number;
  icon: string;
  href: string;
  tone?: "slate" | "brand";
}) {
  return (
    <Link
      href={href}
      className="group block rounded-2xl border border-slate-200 bg-white p-5 shadow-soft transition hover:border-brand-200 hover:shadow-md"
    >
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-slate-500">{label}</span>
        <span className="text-xl">{icon}</span>
      </div>
      <p className="mt-3 text-3xl font-semibold text-slate-900">{value}</p>
      <p className={`mt-1 text-xs font-medium ${tone === "brand" ? "text-brand-600" : "text-slate-400"} group-hover:underline`}>
        View all →
      </p>
    </Link>
  );
}
