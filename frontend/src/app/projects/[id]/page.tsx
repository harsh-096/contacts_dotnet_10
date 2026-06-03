"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
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
import { formatDate, formatPhoneDisplay, initials } from "@/lib/format";

export default function ProjectDetailPage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);
  const router = useRouter();
  const toast = useToast();

  const project = useAsync(["project", id], () => ProjectsApi.get(id), {
    enabled: Number.isFinite(id) && id > 0,
  });
  const contacts = useAsync(
    ["contacts", "byProject", id],
    () => ContactsApi.byProject(id),
    { enabled: Number.isFinite(id) && id > 0 }
  );
  const groups = useAsync(
    ["groups", "byProject", id],
    () => GroupsApi.byProject(id),
    { enabled: Number.isFinite(id) && id > 0 }
  );

  const { run: remove, loading: removing } = useMutation(() =>
    ProjectsApi.remove(id)
  );

  async function onDelete() {
    if (!project.data) return;
    const ok = await confirmDialog({
      title: "Delete project?",
      message: `“${project.data.project_name}” will be permanently removed. The API will refuse if it still has dependent contacts.`,
      confirmLabel: "Delete",
      danger: true,
    });
    if (!ok) return;
    const result = await remove();
    if (result === null) {
      toast.push({
        tone: "error",
        title: "Delete failed",
        description: "Make sure this project has no contacts first.",
      });
      return;
    }
    toast.push({ tone: "success", title: "Project deleted" });
    router.push("/projects");
  }

  if (project.loading && !project.data) return <FullPageSpinner />;
  if (project.error && !project.data) {
    return <PageError error={project.error} onRetry={project.refresh} />;
  }
  if (!project.data) return null;

  const p = project.data;
  const contactList = contacts.data ?? [];

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <PageHeader
        title={p.project_name}
        description={`Project #${p.project_id}`}
        actions={
          <>
            <Link href="/projects">
              <Button variant="secondary">← Back</Button>
            </Link>
            <Link href={`/projects/${p.project_id}/edit`}>
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
            <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Contacts</p>
            <p className="mt-2 text-2xl font-semibold text-slate-900">
              {contacts.loading ? "…" : contactList.length}
            </p>
            <p className="mt-1 text-xs text-slate-500">
              {contactList.filter((c) => c.is_subscribed).length} subscribed
            </p>
          </CardBody>
        </Card>
        <Card>
          <CardBody>
            <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Groups</p>
            <p className="mt-2 text-2xl font-semibold text-slate-900">
              {groups.loading ? "…" : (groups.data?.length ?? 0)}
            </p>
            <p className="mt-1 text-xs text-slate-500">
              Speciality teams in this project
            </p>
          </CardBody>
        </Card>
        <Card>
          <CardBody>
            <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Quick actions</p>
            <div className="mt-2 flex flex-wrap gap-2">
              <Link href={`/contacts/new?projectId=${p.project_id}`}>
                <Button size="sm">+ New contact</Button>
              </Link>
              <Link href={`/groups/new?projectId=${p.project_id}`}>
                <Button size="sm" variant="secondary">+ New group</Button>
              </Link>
            </div>
          </CardBody>
        </Card>
      </div>

      <Card>
        <CardHeader
          title="Contacts in this project"
          description={`${contactList.length} contact${contactList.length === 1 ? "" : "s"}`}
          actions={
            <Link href={`/contacts/new?projectId=${p.project_id}`}>
              <Button size="sm">+ Add contact</Button>
            </Link>
          }
        />
        {contacts.loading && !contacts.data ? (
          <CardBody>
            <div className="flex h-24 items-center justify-center text-slate-400">
              <Spinner />
            </div>
          </CardBody>
        ) : contactList.length === 0 ? (
          <CardBody>
            <EmptyState
              icon="📇"
              title="No contacts in this project"
              description="Add a contact and assign it to this project to see it here."
              action={
                <Link href={`/contacts/new?projectId=${p.project_id}`}>
                  <Button>+ New contact</Button>
                </Link>
              }
            />
          </CardBody>
        ) : (
          <DataTable
            columns={[
              { key: "name", header: "Name" },
              { key: "phone", header: "Phone" },
              { key: "groups", header: "Groups" },
              { key: "status", header: "Status" },
              { key: "created", header: "Created" },
              { key: "actions", header: "", className: "w-32 text-right" },
            ]}
          rows={contactList.map((c) => ({
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
                <span className="text-xs text-slate-500">—</span>,
                c.is_subscribed ? (
                  <Badge tone="green">Subscribed</Badge>
                ) : (
                  <Badge tone="slate">Unsubscribed</Badge>
                ),
                <span className="text-xs text-slate-500">{formatDate(c.created_date)}</span>,
                <Link href={`/contacts/${c.contact_id}`}>
                  <Button size="sm" variant="secondary">View</Button>
                </Link>,
              ],
            }))}
          />
        )}
      </Card>
    </div>
  );
}
