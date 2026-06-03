"use client";

import Link from "next/link";
import { useMemo } from "react";
import { ContactsApi, GroupsApi, ProjectsApi } from "@/lib/api";
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

export default function ProjectsPage() {
  const toast = useToast();
  const projects = useAsync(["projects"], () => ProjectsApi.list());
  const groups = useAsync(["groups"], () => GroupsApi.list());
  const contacts = useAsync(["contacts"], () => ContactsApi.list());

  const { run: remove, loading: removing } = useMutation((id: number) =>
    ProjectsApi.remove(id)
  );

  async function onDelete(id: number, name: string) {
    const ok = await confirmDialog({
      title: "Delete project?",
      message: `“${name}” will be permanently removed. The API will refuse if it still has dependent contacts.`,
      confirmLabel: "Delete",
      danger: true,
    });
    if (!ok) return;
    const result = await remove(id);
    if (result === null) {
      toast.push({
        tone: "error",
        title: "Delete failed",
        description: "Make sure this project has no contacts first.",
      });
      return;
    }
    toast.push({ tone: "success", title: "Project deleted" });
    projects.refresh();
  }

  // Groups belong to projects directly via project_id.
  const groupCountByProject = useMemo(() => {
    const m = new Map<number, number>();
    for (const g of groups.data ?? []) {
      m.set(g.project_id, (m.get(g.project_id) ?? 0) + 1);
    }
    return m;
  }, [groups.data]);

  const contactCountByProject = useMemo(() => {
    const m = new Map<number, number>();
    for (const c of contacts.data ?? []) {
      m.set(c.project_id, (m.get(c.project_id) ?? 0) + 1);
    }
    return m;
  }, [contacts.data]);

  if (projects.loading && !projects.data) return <FullPageSpinner />;
  if (projects.error && !projects.data) {
    return <PageError error={projects.error} onRetry={projects.refresh} />;
  }

  const list = projects.data ?? [];

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <PageHeader
        title="Projects"
        description="Top-level containers that own contacts. Each contact can have many groups."
        actions={
          <Link href="/projects/new">
            <Button>+ New project</Button>
          </Link>
        }
      />

      {list.length === 0 ? (
        <EmptyState
          icon="📁"
          title="No projects yet"
          description="Create your first project to start adding contacts."
          action={
            <Link href="/projects/new">
              <Button>+ New project</Button>
            </Link>
          }
        />
      ) : (
        <DataTable
          columns={[
            { key: "id", header: "ID", className: "w-16" },
            { key: "name", header: "Project name" },
            { key: "groups", header: "Groups" },
            { key: "contacts", header: "Contacts" },
            { key: "actions", header: "", className: "w-44 text-right" },
          ]}
          rows={list.map((p) => ({
            id: p.project_id,
            cells: [
              <span className="font-mono text-xs text-slate-500">#{p.project_id}</span>,
              <Link
                href={`/projects/${p.project_id}`}
                className="font-medium text-slate-900 hover:text-brand-600"
              >
                {p.project_name}
              </Link>,
              <Badge tone="brand">
                {groupCountByProject.get(p.project_id) ?? 0} group
                {(groupCountByProject.get(p.project_id) ?? 0) === 1 ? "" : "s"}
              </Badge>,
              <Badge tone="slate">
                {contactCountByProject.get(p.project_id) ?? 0} contact
                {(contactCountByProject.get(p.project_id) ?? 0) === 1 ? "" : "s"}
              </Badge>,
              <div className="flex items-center justify-end gap-2">
                <Link href={`/projects/${p.project_id}`}>
                  <Button size="sm" variant="secondary">View</Button>
                </Link>
                <Button
                  size="sm"
                  variant="danger"
                  loading={removing}
                  onClick={() => onDelete(p.project_id, p.project_name)}
                >
                  Delete
                </Button>
              </div>,
            ],
          }))}
        />
      )}

      {projects.loading && list.length > 0 && (
        <div className="flex items-center gap-2 text-xs text-slate-500">
          <Spinner size={12} /> Refreshing…
        </div>
      )}
    </div>
  );
}
