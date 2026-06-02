"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import {
  ContactsApi,
  GroupsApi,
  ProjectsApi,
} from "@/lib/api";
import { useAsync, useMutation } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { confirmDialog } from "@/components/layout/ConfirmHost";
import { PageError } from "@/components/layout/Page";
import { PageHeader } from "@/components/layout/PageHeader";
import { Button } from "@/components/ui/Button";
import { Card, CardBody, CardHeader } from "@/components/ui/Card";
import { DataTable } from "@/components/ui/DataTable";
import { EmptyState } from "@/components/ui/EmptyState";
import { FullPageSpinner, Spinner } from "@/components/ui/Spinner";
import { Badge } from "@/components/ui/Badge";
import { Field, Select } from "@/components/ui/Input";
import { Alert } from "@/components/ui/Alert";
import { initials, formatPhoneDisplay, formatDate } from "@/lib/format";
import { describeError } from "@/lib/errors";

export default function GroupDetailPage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);
  const router = useRouter();
  const toast = useToast();

  const group = useAsync(["group", id], () => GroupsApi.get(id), {
    enabled: Number.isFinite(id) && id > 0,
  });
  const members = useAsync(
    ["group", id, "contacts"],
    () => GroupsApi.contacts(id),
    { enabled: Number.isFinite(id) && id > 0 }
  );
  const project = useAsync(
    ["group", id, "project"],
    async () => {
      const g = await GroupsApi.get(id);
      if (!g) throw new Error("Group not found");
      return ProjectsApi.get(g.project_id);
    },
    { enabled: Number.isFinite(id) && id > 0 }
  );

  // Candidates to add: contacts that belong to the same project, are not
  // already in this group. The frontend never sends phoneNumber — the server
  // rebuilds it on create/update.
  const allContacts = useAsync(
    ["group", id, "project", project.data?.project_id, "contacts"],
    async () => {
      if (!project.data) return [];
      return ContactsApi.byProject(project.data.project_id);
    },
    { enabled: !!project.data }
  );

  const { run: remove, loading: removing } = useMutation(() =>
    GroupsApi.remove(id)
  );
  const { run: addContact, loading: adding } = useMutation(
    (contactId: number) => GroupsApi.addContact(id, contactId)
  );
  const { run: removeContact, loading: removingContact } = useMutation(
    (contactId: number) => GroupsApi.removeContact(id, contactId)
  );

  const [candidate, setCandidate] = useState<number | "">("");

  const memberIds = useMemo(
    () => new Set((members.data ?? []).map((c) => c.contact_id)),
    [members.data]
  );

  const candidates = useMemo(
    () => (allContacts.data ?? []).filter((c) => !memberIds.has(c.contact_id)),
    [allContacts.data, memberIds]
  );

  async function onDelete() {
    if (!group.data) return;
    const ok = await confirmDialog({
      title: "Delete group?",
      message: `“${group.data.group_name}” will be permanently removed. The API will refuse if it still has contact members — remove them first.`,
      confirmLabel: "Delete",
      danger: true,
    });
    if (!ok) return;
    const result = await remove();
    if (result === null) {
      toast.push({
        tone: "error",
        title: "Delete failed",
        description: "Remove all contact members first.",
      });
      return;
    }
    toast.push({ tone: "success", title: "Group deleted" });
    router.push("/groups");
  }

  async function onAddContact() {
    if (!candidate) return;
    const result = await addContact(Number(candidate));
    if (result === null) {
      toast.push({
        tone: "error",
        title: "Could not add contact",
        description: "The contact must belong to this group's project.",
      });
      return;
    }
    toast.push({ tone: "success", title: "Contact added to group" });
    setCandidate("");
    members.refresh();
    allContacts.refresh();
  }

  async function onRemoveContact(contactId: number, name: string) {
    const ok = await confirmDialog({
      title: "Remove from group?",
      message: `Remove “${name}” from this group? The contact itself is not deleted.`,
      confirmLabel: "Remove",
      danger: true,
    });
    if (!ok) return;
    const result = await removeContact(contactId);
    if (result === null) {
      toast.push({ tone: "error", title: "Remove failed" });
      return;
    }
    toast.push({ tone: "success", title: "Contact removed" });
    members.refresh();
    allContacts.refresh();
  }

  if (group.loading && !group.data) return <FullPageSpinner />;
  if (group.error && !group.data) {
    return <PageError error={group.error} onRetry={group.refresh} />;
  }
  if (!group.data) return null;

  const g = group.data;
  const memberList = members.data ?? [];

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <PageHeader
        title={g.group_name}
        description={`Group #${g.group_id} · Project #${g.project_id}`}
        actions={
          <>
            <Link href="/groups">
              <Button variant="secondary">← Back</Button>
            </Link>
            <Link href={`/groups/${g.group_id}/edit`}>
              <Button variant="secondary">Edit</Button>
            </Link>
            <Button variant="danger" loading={removing} onClick={onDelete}>
              Delete
            </Button>
          </>
        }
      />

      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <Card>
          <CardBody>
            <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Project</p>
            {project.loading ? (
              <div className="mt-2 text-slate-400"><Spinner size={14} /></div>
            ) : project.data ? (
              <Link
                href={`/projects/${project.data.project_id}`}
                className="mt-2 inline-block text-sm font-semibold text-slate-900 hover:text-brand-600"
              >
                {project.data.project_name}
              </Link>
            ) : (
              <p className="mt-2 text-sm text-red-600">{describeError(project.error)}</p>
            )}
          </CardBody>
        </Card>
        <Card>
          <CardBody>
            <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Members</p>
            <p className="mt-2 text-2xl font-semibold text-slate-900">
              {members.loading ? "…" : memberList.length}
            </p>
            <p className="mt-1 text-xs text-slate-500">
              {memberList.filter((c) => c.is_subscribed).length} subscribed
            </p>
          </CardBody>
        </Card>
        <Card>
          <CardBody>
            <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Add member</p>
            {candidates.length === 0 ? (
              <p className="mt-2 text-sm text-slate-500">
                All eligible contacts in this project are already in this group.
              </p>
            ) : (
              <div className="mt-2 space-y-2">
                <Field>
                  <Select
                    value={candidate}
                    onChange={(e) => setCandidate(e.target.value ? Number(e.target.value) : "")}
                  >
                    <option value="">Pick a contact…</option>
                    {candidates.map((c) => (
                      <option key={c.contact_id} value={c.contact_id}>
                        {c.first_name} {c.last_name} · {formatPhoneDisplay(c.country_code, c.national_number)}
                      </option>
                    ))}
                  </Select>
                </Field>
                <Button
                  size="sm"
                  loading={adding}
                  disabled={!candidate}
                  onClick={onAddContact}
                >
                  Add to group
                </Button>
              </div>
            )}
          </CardBody>
        </Card>
      </div>

      <Card>
        <CardHeader
          title="Members"
          description={`${memberList.length} contact${memberList.length === 1 ? "" : "s"} in this group`}
        />
        {members.loading && !members.data ? (
          <CardBody>
            <div className="flex h-24 items-center justify-center text-slate-400">
              <Spinner />
            </div>
          </CardBody>
        ) : memberList.length === 0 ? (
          <CardBody>
            <EmptyState
              icon="👥"
              title="No members yet"
              description="Use the picker above to add contacts from the same project."
            />
          </CardBody>
        ) : (
          <DataTable
            columns={[
              { key: "name", header: "Name" },
              { key: "phone", header: "Phone" },
              { key: "status", header: "Status" },
              { key: "joined", header: "Joined" },
              { key: "actions", header: "", className: "w-44 text-right" },
            ]}
            rows={memberList.map((c) => ({
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
                <span className="font-mono text-xs text-slate-700">
                  {formatPhoneDisplay(c.country_code, c.national_number)}
                </span>,
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
                    loading={removingContact}
                    onClick={() => onRemoveContact(c.contact_id, `${c.first_name} ${c.last_name}`)}
                  >
                    Remove
                  </Button>
                </div>,
              ],
            }))}
          />
        )}
      </Card>
    </div>
  );
}
