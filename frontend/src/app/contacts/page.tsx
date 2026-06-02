"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { ContactsApi, ProjectsApi } from "@/lib/api";
import { useAsync, useMutation } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { confirmDialog } from "@/components/layout/ConfirmHost";
import { PageHeader } from "@/components/layout/PageHeader";
import { PageError } from "@/components/layout/Page";
import { Button } from "@/components/ui/Button";
import { DataTable } from "@/components/ui/DataTable";
import { EmptyState } from "@/components/ui/EmptyState";
import { FullPageSpinner, Spinner } from "@/components/ui/Spinner";
import { Badge } from "@/components/ui/Badge";
import { Field, Input, Select } from "@/components/ui/Input";
import { formatDate, formatPhoneDisplay, initials } from "@/lib/format";

export default function ContactsPage() {
  const toast = useToast();
  const contacts = useAsync(["contacts"], () => ContactsApi.list());
  const projects = useAsync(["projects"], () => ProjectsApi.list());

  const [query, setQuery] = useState("");
  const [projectFilter, setProjectFilter] = useState<number | "all">("all");
  const [statusFilter, setStatusFilter] = useState<"all" | "subscribed" | "unsubscribed">("all");

  const { run: remove, loading: removing } = useMutation((id: number) =>
    ContactsApi.remove(id)
  );

  const projectById = useMemo(() => {
    const m = new Map<number, string>();
    for (const p of projects.data ?? []) m.set(p.project_id, p.project_name);
    return m;
  }, [projects.data]);

  const filtered = useMemo(() => {
    const list = contacts.data ?? [];
    const q = query.trim().toLowerCase();
    return list.filter((c) => {
      if (projectFilter !== "all" && c.project_id !== projectFilter) return false;
      if (statusFilter === "subscribed" && !c.is_subscribed) return false;
      if (statusFilter === "unsubscribed" && c.is_subscribed) return false;
      if (!q) return true;
      return (
        c.first_name.toLowerCase().includes(q) ||
        c.last_name.toLowerCase().includes(q) ||
        c.country_code.toLowerCase().includes(q) ||
        c.national_number.toLowerCase().includes(q)
      );
    });
  }, [contacts.data, query, projectFilter, statusFilter]);

  async function onDelete(id: number, name: string) {
    const ok = await confirmDialog({
      title: "Delete contact?",
      message: `“${name}” will be permanently removed. Any group memberships are removed automatically.`,
      confirmLabel: "Delete",
      danger: true,
    });
    if (!ok) return;
    const result = await remove(id);
    if (result === null) {
      toast.push({ tone: "error", title: "Delete failed" });
      return;
    }
    toast.push({ tone: "success", title: "Contact deleted" });
    contacts.refresh();
  }

  if (contacts.loading && !contacts.data) return <FullPageSpinner />;
  if (contacts.error && !contacts.data) {
    return <PageError error={contacts.error} onRetry={contacts.refresh} />;
  }

  const list = contacts.data ?? [];

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <PageHeader
        title="Contacts"
        description="Manage every contact in the system."
        actions={
          <Link href="/contacts/new">
            <Button>+ New contact</Button>
          </Link>
        }
      />

      <div className="grid grid-cols-1 gap-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-soft sm:grid-cols-3">
        <Field label="Search">
          <Input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Name, country code or number…"
          />
        </Field>
        <Field label="Project">
          <Select
            value={projectFilter}
            onChange={(e) =>
              setProjectFilter(e.target.value === "all" ? "all" : Number(e.target.value))
            }
          >
            <option value="all">All projects</option>
            {(projects.data ?? []).map((p) => (
              <option key={p.project_id} value={p.project_id}>
                {p.project_name}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Status">
          <Select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as typeof statusFilter)}
          >
            <option value="all">All</option>
            <option value="subscribed">Subscribed</option>
            <option value="unsubscribed">Unsubscribed</option>
          </Select>
        </Field>
      </div>

      {list.length === 0 ? (
        <EmptyState
          icon="📇"
          title="No contacts yet"
          description="Add a contact and assign it to a project to get started."
          action={
            <Link href="/contacts/new">
              <Button>+ New contact</Button>
            </Link>
          }
        />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon="🔍"
          title="No matches"
          description="Try adjusting your filters or search query."
        />
      ) : (
        <DataTable
          columns={[
            { key: "name", header: "Name" },
            { key: "phone", header: "Phone" },
            { key: "project", header: "Project" },
            { key: "status", header: "Status" },
            { key: "created", header: "Created" },
            { key: "actions", header: "", className: "w-44 text-right" },
          ]}
          rows={filtered.map((c) => ({
            id: c.contact_id,
            cells: [
              <div className="flex items-center gap-3">
                <div className="flex h-8 w-8 items-center justify-center rounded-full bg-brand-100 text-xs font-semibold text-brand-700">
                  {initials(c.first_name, c.last_name)}
                </div>
                <Link
                  href={`/contacts/${c.contact_id}`}
                  className="font-medium text-slate-900 hover:text-brand-600"
                >
                  {c.first_name} {c.last_name}
                </Link>
              </div>,
              <div className="flex flex-col">
                <span className="font-mono text-xs text-slate-700">
                  {formatPhoneDisplay(c.country_code, c.national_number)}
                </span>
                <span className="font-mono text-[10px] text-slate-400">
                  phone #{c.phone_number}
                </span>
              </div>,
              <div className="flex items-center gap-2">
                <Badge tone="brand">#{c.project_id}</Badge>
                <span className="text-sm text-slate-700">
                  {projectById.get(c.project_id) ?? "—"}
                </span>
              </div>,
              c.is_subscribed ? (
                <Badge tone="green">Subscribed</Badge>
              ) : (
                <Badge tone="slate">Unsubscribed</Badge>
              ),
              <span className="text-xs text-slate-500">{formatDate(c.created_date)}</span>,
              <div className="flex items-center justify-end gap-2">
                <Link href={`/contacts/${c.contact_id}`}>
                  <Button size="sm" variant="secondary">View</Button>
                </Link>
                <Button
                  size="sm"
                  variant="danger"
                  loading={removing}
                  onClick={() => onDelete(c.contact_id, `${c.first_name} ${c.last_name}`)}
                >
                  Delete
                </Button>
              </div>,
            ],
          }))}
        />
      )}

      {contacts.loading && list.length > 0 && (
        <div className="flex items-center gap-2 text-xs text-slate-500">
          <Spinner size={12} /> Refreshing…
        </div>
      )}
    </div>
  );
}
