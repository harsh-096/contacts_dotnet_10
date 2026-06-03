"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useState } from "react";
import { ContactsApi, GroupsApi, ProjectsApi } from "@/lib/api";
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
import { Modal } from "@/components/ui/Modal";
import { Select, Field } from "@/components/ui/Input";
import { formatDate, formatPhoneDisplay, initials } from "@/lib/format";
import { describeError } from "@/lib/errors";

export default function GroupDetailPage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);
  const router = useRouter();
  const toast = useToast();

  const group = useAsync(["group", id], () => GroupsApi.get(id), {
    enabled: Number.isFinite(id) && id > 0,
  });
  const project = useAsync(
    ["group", id, "project"],
    async () => {
      const g = await GroupsApi.get(id);
      if (!g) throw new Error("Group not found");
      return ProjectsApi.get(g.project_id);
    },
    { enabled: Number.isFinite(id) && id > 0 }
  );
  const members = useAsync(
    ["group", id, "members"],
    () => GroupsApi.getContacts(id),
    { enabled: Number.isFinite(id) && id > 0 }
  );
  const allContacts = useAsync(["contacts"], () => ContactsApi.list());

  const { run: remove, loading: removing } = useMutation(() =>
    GroupsApi.remove(id)
  );

  const [showAddMember, setShowAddMember] = useState(false);
  const [selectedContactId, setSelectedContactId] = useState(0);

  const addMember = useMutation(
    (contactId: number) => GroupsApi.addContact(id, contactId)
  );

  const removeMember = useMutation(
    (contactId: number) => GroupsApi.removeContact(id, contactId)
  );

  async function onDelete() {
    if (!group.data) return;
    const ok = await confirmDialog({
      title: "Delete group?",
      message: `"${group.data.group_name}" will be permanently removed.`,
      confirmLabel: "Delete",
      danger: true,
    });
    if (!ok) return;
    const result = await remove();
    if (result === null) {
      toast.push({ tone: "error", title: "Delete failed" });
      return;
    }
    toast.push({ tone: "success", title: "Group deleted" });
    router.push("/groups");
  }

  async function handleAddMember() {
    if (selectedContactId <= 0) return;
    const result = await addMember.run(selectedContactId);
    if (result) {
      toast.push({ tone: "success", title: "Member added" });
      setShowAddMember(false);
      setSelectedContactId(0);
      members.refresh();
    }
  }

  async function handleRemoveMember(contactId: number, name: string) {
    const ok = await confirmDialog({
      title: "Remove member?",
      message: `Remove "${name}" from this group?`,
      confirmLabel: "Remove",
      danger: true,
    });
    if (!ok) return;
    const result = await removeMember.run(contactId);
    if (result) {
      toast.push({ tone: "success", title: "Member removed" });
      members.refresh();
    }
  }

  if (group.loading && !group.data) return <FullPageSpinner />;
  if (group.error && !group.data) {
    return <PageError error={group.error} onRetry={group.refresh} />;
  }
  if (!group.data) return null;

  const g = group.data;
  const memberList = members.data ?? [];
  const memberIds = new Set(memberList.map((m) => m.contact_id));
  const availableContacts = (allContacts.data ?? []).filter(
    (c) => !memberIds.has(c.contact_id)
  );

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

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <Card>
          <CardHeader
            title="Project"
            description="The project this group belongs to."
            actions={
              project.data ? (
                <Link href={`/projects/${project.data.project_id}`}>
                  <Button size="sm" variant="secondary">Open project</Button>
                </Link>
              ) : undefined
            }
          />
          {project.loading && !project.data ? (
            <CardBody>
              <div className="flex h-24 items-center justify-center text-slate-400">
                <Spinner />
              </div>
            </CardBody>
          ) : project.data ? (
            <CardBody>
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-brand-100 text-base">
                  📁
                </div>
                <div>
                  <p className="text-sm font-semibold text-slate-900">
                    {project.data.project_name}
                  </p>
                  <p className="text-xs text-slate-500">Project #{project.data.project_id}</p>
                </div>
              </div>
            </CardBody>
          ) : (
            <CardBody>
              <p className="text-sm text-red-600">{describeError(project.error)}</p>
            </CardBody>
          )}
        </Card>

        <Card>
          <CardHeader title="Timestamps" />
          <CardBody>
            <dl className="grid grid-cols-1 gap-3 text-sm">
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">Group id</dt>
                <dd className="mt-1 font-mono text-slate-900">#{g.group_id}</dd>
              </div>
            </dl>
          </CardBody>
        </Card>
      </div>

      <Card>
        <CardHeader
          title={`Members (${memberList.length})`}
          description="Contacts that belong to this group."
          actions={
            <Button size="sm" onClick={() => setShowAddMember(true)} leftIcon={<span>+</span>}>
              Add member
            </Button>
          }
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
              description="This group has no contacts. Add contacts to this group."
            />
          </CardBody>
        ) : (
          <DataTable
            columns={[
              { key: "name", header: "Name" },
              { key: "phone", header: "Phone" },
              { key: "status", header: "Status" },
              { key: "actions", header: "", className: "w-32 text-right" },
            ]}
            rows={memberList.map((m) => ({
              id: m.contact_id,
              cells: [
                <div className="flex items-center gap-2">
                  <div className="flex h-8 w-8 items-center justify-center rounded-full bg-brand-100 text-xs font-semibold text-brand-700">
                    {initials(m.first_name, m.last_name)}
                  </div>
                  <Link
                    href={`/contacts/${m.contact_id}`}
                    className="font-medium text-slate-900 hover:text-brand-600"
                  >
                    {m.first_name} {m.last_name}
                  </Link>
                </div>,
                <span className="font-mono text-xs text-slate-700">
                  {formatPhoneDisplay(m.country_code, m.national_number)}
                </span>,
                m.is_subscribed ? (
                  <Badge tone="green">Subscribed</Badge>
                ) : (
                  <Badge tone="slate">Unsubscribed</Badge>
                ),
                <div className="flex gap-1">
                  <Link href={`/contacts/${m.contact_id}`}>
                    <Button size="sm" variant="secondary">View</Button>
                  </Link>
                  <Button
                    size="sm"
                    variant="danger"
                    loading={removeMember.loading}
                    onClick={() => handleRemoveMember(m.contact_id, `${m.first_name} ${m.last_name}`)}
                  >
                    Remove
                  </Button>
                </div>,
              ],
            }))}
          />
        )}
      </Card>

      <Modal
        open={showAddMember}
        onClose={() => { setShowAddMember(false); setSelectedContactId(0); }}
        title="Add member"
        description={`Add a contact to "${g.group_name}"`}
        footer={
          <>
            <Button variant="secondary" size="sm" onClick={() => { setShowAddMember(false); setSelectedContactId(0); }}>
              Cancel
            </Button>
            <Button size="sm" loading={addMember.loading} onClick={handleAddMember} disabled={selectedContactId <= 0}>
              Add
            </Button>
          </>
        }
      >
        <Field label="Select a contact" required>
          <Select
            value={selectedContactId || ""}
            onChange={(e) => setSelectedContactId(Number(e.target.value))}
          >
            <option value="">Choose a contact…</option>
            {availableContacts.map((c) => (
              <option key={c.contact_id} value={c.contact_id}>
                {c.first_name} {c.last_name} · {formatPhoneDisplay(c.country_code, c.national_number)}
              </option>
            ))}
          </Select>
        </Field>
        {addMember.error && (
          <p className="mt-2 text-xs text-red-600">{describeError(addMember.error)}</p>
        )}
      </Modal>
    </div>
  );
}
