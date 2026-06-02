"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useState } from "react";
import { ContactsApi, ProjectsApi } from "@/lib/api";
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
import { formatDate, formatPhoneDisplay, initials, phoneNumberToDisplay } from "@/lib/format";
import { describeError } from "@/lib/errors";

export default function ContactDetailPage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);
  const router = useRouter();
  const toast = useToast();

  const contact = useAsync(["contact", id], () => ContactsApi.get(id), {
    enabled: Number.isFinite(id) && id > 0,
  });
  const groups = useAsync(
    ["contact", id, "groups"],
    () => ContactsApi.groups(id),
    { enabled: Number.isFinite(id) && id > 0 }
  );
  const project = useAsync(
    ["contact", id, "project"],
    async () => {
      const c = await ContactsApi.get(id);
      return ProjectsApi.get(c.project_id);
    },
    { enabled: Number.isFinite(id) && id > 0 }
  );

  const { run: remove, loading: removing } = useMutation(() =>
    ContactsApi.remove(id)
  );
  const { run: toggleSubscribed, loading: toggling } = useMutation(
    (next: boolean) => ContactsApi.update(id, { is_subscribed: next })
  );

  async function onDelete() {
    if (!contact.data) return;
    const ok = await confirmDialog({
      title: "Delete contact?",
      message: `“${contact.data.first_name} ${contact.data.last_name}” will be permanently removed. Group memberships are removed automatically.`,
      confirmLabel: "Delete",
      danger: true,
    });
    if (!ok) return;
    const result = await remove();
    if (result === null) {
      toast.push({ tone: "error", title: "Delete failed" });
      return;
    }
    toast.push({ tone: "success", title: "Contact deleted" });
    router.push("/contacts");
  }

  async function onToggleSubscribed() {
    if (!contact.data) return;
    const next = !contact.data.is_subscribed;
    const result = await toggleSubscribed(next);
    if (!result) {
      toast.push({ tone: "error", title: "Could not update subscription" });
      return;
    }
    toast.push({
      tone: "success",
      title: next ? "Subscribed" : "Unsubscribed",
    });
    contact.refresh();
  }

  if (contact.loading && !contact.data) return <FullPageSpinner />;
  if (contact.error && !contact.data) {
    return <PageError error={contact.error} onRetry={contact.refresh} />;
  }
  if (!contact.data) return null;

  const c = contact.data;
  const groupList = groups.data ?? [];

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <PageHeader
        title={`${c.first_name} ${c.last_name}`}
        description={`Contact #${c.contact_id} · ${phoneNumberToDisplay(c.phone_number)}`}
        actions={
          <>
            <Link href="/contacts">
              <Button variant="secondary">← Back</Button>
            </Link>
            <Button
              variant="secondary"
              loading={toggling}
              onClick={onToggleSubscribed}
            >
              {c.is_subscribed ? "Unsubscribe" : "Subscribe"}
            </Button>
            <Link href={`/contacts/${c.contact_id}/edit`}>
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
            <div className="flex items-center gap-3">
              <div className="flex h-12 w-12 items-center justify-center rounded-full bg-brand-100 text-base font-semibold text-brand-700">
                {initials(c.first_name, c.last_name)}
              </div>
              <div>
                <p className="text-sm font-semibold text-slate-900">
                  {c.first_name} {c.last_name}
                </p>
                <p className="text-xs text-slate-500">Contact #{c.contact_id}</p>
              </div>
            </div>
            <div className="mt-4 flex flex-wrap gap-2">
              {c.is_subscribed ? (
                <Badge tone="green">Subscribed</Badge>
              ) : (
                <Badge tone="slate">Unsubscribed</Badge>
              )}
              <Badge tone="brand">Project #{c.project_id}</Badge>
            </div>
          </CardBody>
        </Card>

        <Card>
          <CardBody>
            <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Phone</p>
            <p className="mt-2 font-mono text-sm text-slate-900">
              {formatPhoneDisplay(c.country_code, c.national_number)}
            </p>
            <p className="mt-1 font-mono text-xs text-slate-500">
              stored as bigint: {c.phone_number}
            </p>
          </CardBody>
        </Card>

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
      </div>

      <Card>
        <CardHeader
          title="Group memberships"
          description={`${groupList.length} group${groupList.length === 1 ? "" : "s"}`}
        />
        {groups.loading && !groups.data ? (
          <CardBody>
            <div className="flex h-24 items-center justify-center text-slate-400">
              <Spinner />
            </div>
          </CardBody>
        ) : groupList.length === 0 ? (
          <CardBody>
            <EmptyState
              icon="👥"
              title="Not a member of any group"
              description="Open a group belonging to the same project to add this contact."
            />
          </CardBody>
        ) : (
          <DataTable
            columns={[
              { key: "id", header: "ID", className: "w-16" },
              { key: "name", header: "Group" },
              { key: "project", header: "Project" },
              { key: "actions", header: "", className: "w-32 text-right" },
            ]}
            rows={groupList.map((g) => ({
              id: g.group_id,
              cells: [
                <span className="font-mono text-xs text-slate-500">#{g.group_id}</span>,
                <Link
                  href={`/groups/${g.group_id}`}
                  className="font-medium text-slate-900 hover:text-brand-600"
                >
                  {g.group_name}
                </Link>,
                <Badge tone="brand">#{g.project_id}</Badge>,
                <Link href={`/groups/${g.group_id}`}>
                  <Button size="sm" variant="secondary">View group</Button>
                </Link>,
              ],
            }))}
          />
        )}
      </Card>

      <Card>
        <CardHeader title="Timestamps" />
        <CardBody>
          <dl className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-2">
            <div>
              <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">Created</dt>
              <dd className="mt-1 text-slate-900">{formatDate(c.created_date)}</dd>
            </div>
            <div>
              <dt className="text-xs font-medium uppercase tracking-wide text-slate-500">Last updated</dt>
              <dd className="mt-1 text-slate-900">{formatDate(c.updated_date)}</dd>
            </div>
          </dl>
        </CardBody>
      </Card>
    </div>
  );
}
