"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { ApiError, ContactsApi, GroupsApi } from "@/lib/api";
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
import { Checkbox, Field, Input, Select } from "@/components/ui/Input";
import { Modal } from "@/components/ui/Modal";
import { formatDate, formatPhoneDisplay, initials } from "@/lib/format";

export default function ContactsPage() {
  const toast = useToast();
  const contacts = useAsync(["contacts"], () => ContactsApi.list());
  const allGroups = useAsync(["groups"], () => GroupsApi.list());

  const [query, setQuery] = useState("");
  const [projectFilter, setProjectFilter] = useState<number | "all">("all");
  const [statusFilter, setStatusFilter] = useState<"all" | "subscribed" | "unsubscribed">("all");

  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [pendingGroupId, setPendingGroupId] = useState(0);
  const [showAddGroup, setShowAddGroup] = useState(false);

  const projectById = useMemo(() => {
    const m = new Map<number, string>();
    for (const c of contacts.data ?? []) m.set(c.project_id, "");
    return m;
  }, [contacts.data]);

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

  function toggleAll() {
    if (selected.size === filtered.length && filtered.length > 0) {
      setSelected(new Set());
    } else {
      setSelected(new Set(filtered.map((c) => c.contact_id)));
    }
  }

  function toggleOne(id: number) {
    const next = new Set(selected);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    setSelected(next);
  }

  const bulkDelete = useMutation(async () => {
    const ids = [...selected];
    const errors: string[] = [];
    let succeeded = 0;
    for (const id of ids) {
      try {
        await ContactsApi.remove(id);
        succeeded++;
      } catch (err) {
        const msg = err instanceof ApiError ? err.message : "Unknown error";
        errors.push(msg);
      }
    }
    if (succeeded > 0) {
      toast.push({ tone: "success", title: `${succeeded} contact${succeeded === 1 ? "" : "s"} deleted` });
    }
    for (const msg of errors) {
      toast.push({ tone: "error", title: msg });
    }
    setSelected(new Set());
    contacts.refresh();
  });

  const bulkAddGroup = useMutation(async (groupId: number) => {
    const ids = [...selected];
    const errors: string[] = [];
    let succeeded = 0;
    for (const id of ids) {
      try {
        await GroupsApi.addContact(groupId, id);
        succeeded++;
      } catch (err) {
        const msg = err instanceof ApiError ? err.message : "Unknown error";
        errors.push(msg);
      }
    }
    if (succeeded > 0) {
      toast.push({ tone: "success", title: `${succeeded} contact${succeeded === 1 ? "" : "s"} added to group` });
    }
    for (const msg of errors) {
      toast.push({ tone: "error", title: msg });
    }
    if (succeeded > 0) {
      setShowAddGroup(false);
      setPendingGroupId(0);
      setSelected(new Set());
      contacts.refresh();
    }
  });

  async function handleBulkDelete() {
    const ok = await confirmDialog({
      title: `Delete ${selected.size} contact${selected.size === 1 ? "" : "s"}?`,
      message: `${selected.size} contact${selected.size === 1 ? "" : "s"} will be permanently removed. This action cannot be undone.`,
      confirmLabel: `Delete ${selected.size}`,
      danger: true,
    });
    if (!ok) return;
    await bulkDelete.run();
  }

  async function handleBulkAddGroup() {
    if (pendingGroupId <= 0) return;
    await bulkAddGroup.run(pendingGroupId);
  }

  if (contacts.loading && !contacts.data) return <FullPageSpinner />;
  if (contacts.error && !contacts.data) {
    return <PageError error={contacts.error} onRetry={contacts.refresh} />;
  }

  const list = contacts.data ?? [];
  const groups = allGroups.data ?? [];
  const hasSelection = selected.size > 0;

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
            {(contacts.data ?? [])
              .filter((c, i, a) => a.findIndex((x) => x.project_id === c.project_id) === i)
              .map((c) => (
                <option key={c.project_id} value={c.project_id}>
                  Project #{c.project_id}
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

      {hasSelection && (
        <div className="flex items-center gap-3 rounded-2xl border border-brand-200 bg-brand-50 p-3 shadow-soft">
          <span className="text-sm font-medium text-brand-800">
            {selected.size} selected
          </span>
          <div className="ml-auto flex gap-2">
            <Button
              size="sm"
              variant="secondary"
              leftIcon={<span>+</span>}
              onClick={() => setShowAddGroup(true)}
              disabled={!hasSelection}
            >
              Add to Groups
            </Button>
            <Button
              size="sm"
              variant="danger"
              loading={bulkDelete.loading}
              onClick={handleBulkDelete}
              disabled={!hasSelection}
            >
              Delete Contacts
            </Button>
            <Button
              size="sm"
              variant="secondary"
              onClick={() => setSelected(new Set())}
            >
              Clear
            </Button>
          </div>
        </div>
      )}

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
            {
              key: "select",
              header: (
                <Checkbox
                  checked={filtered.length > 0 && selected.size === filtered.length}
                  onChange={toggleAll}
                />
              ),
              className: "w-10",
            },
            { key: "name", header: "Name" },
            { key: "phone", header: "Phone" },
            { key: "status", header: "Status" },
            { key: "created", header: "Created" },
            { key: "actions", header: "", className: "w-20 text-right" },
          ]}
          rows={filtered.map((c) => ({
            id: c.contact_id,
            cells: [
              <Checkbox
                checked={selected.has(c.contact_id)}
                onChange={() => toggleOne(c.contact_id)}
              />,
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

      <Modal
        open={showAddGroup}
        onClose={() => { setShowAddGroup(false); setPendingGroupId(0); }}
        title="Add to group"
        description={`Add ${selected.size} contact${selected.size === 1 ? "" : "s"} to a group.`}
        footer={
          <>
            <Button variant="secondary" size="sm" onClick={() => { setShowAddGroup(false); setPendingGroupId(0); }}>
              Cancel
            </Button>
            <Button size="sm" loading={bulkAddGroup.loading} onClick={handleBulkAddGroup} disabled={pendingGroupId <= 0}>
              Add
            </Button>
          </>
        }
      >
        <Field label="Select a group" required>
          <Select
            value={pendingGroupId || ""}
            onChange={(e) => setPendingGroupId(Number(e.target.value))}
          >
            <option value="">Choose a group…</option>
            {groups.map((g) => (
              <option key={g.group_id} value={g.group_id}>
                {g.group_name} (Project #{g.project_id})
              </option>
            ))}
          </Select>
        </Field>
        {bulkAddGroup.error && (
          <p className="mt-2 text-xs text-red-600">{bulkAddGroup.error.message}</p>
        )}
      </Modal>
    </div>
  );
}
