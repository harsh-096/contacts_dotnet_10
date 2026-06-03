"use client";

import Link from "next/link";
import { useMemo } from "react";
import { GroupsApi, ProjectsApi } from "@/lib/api";
import { useAsync } from "@/lib/hooks";
import { PageHeader } from "@/components/layout/PageHeader";
import { PageError } from "@/components/layout/Page";
import { Button } from "@/components/ui/Button";
import { DataTable } from "@/components/ui/DataTable";
import { EmptyState } from "@/components/ui/EmptyState";
import { FullPageSpinner, Spinner } from "@/components/ui/Spinner";
import { Badge } from "@/components/ui/Badge";

export default function GroupsPage() {
  const groups = useAsync(["groups"], () => GroupsApi.list());
  const projects = useAsync(["projects"], () => ProjectsApi.list());

  const projectById = useMemo(() => {
    const m = new Map<number, string>();
    for (const p of projects.data ?? []) {
      m.set(p.project_id, p.project_name);
    }
    return m;
  }, [projects.data]);

  if (groups.loading && !groups.data) return <FullPageSpinner />;
  if (groups.error && !groups.data) {
    return <PageError error={groups.error} onRetry={groups.refresh} />;
  }

  const list = groups.data ?? [];

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <PageHeader
        title="Groups"
        description="Groups are specialities that belong to a project and can contain many contacts."
        actions={
          <Link href="/groups/new">
            <Button>+ New group</Button>
          </Link>
        }
      />

      {list.length === 0 ? (
        <EmptyState
          icon="👥"
          title="No groups yet"
          description="Create a group under a project."
          action={
            <Link href="/groups/new">
              <Button>+ New group</Button>
            </Link>
          }
        />
      ) : (
        <DataTable
          columns={[
            { key: "id", header: "ID", className: "w-16" },
            { key: "name", header: "Group name" },
            { key: "project", header: "Project" },
            { key: "actions", header: "", className: "w-32 text-right" },
          ]}
          rows={list.map((g) => {
            const projectName = projectById.get(g.project_id);
            return {
              id: g.group_id,
              cells: [
                <span className="font-mono text-xs text-slate-500">#{g.group_id}</span>,
                <Link
                  href={`/groups/${g.group_id}`}
                  className="font-medium text-slate-900 hover:text-brand-600"
                >
                  {g.group_name}
                </Link>,
                <div className="flex items-center gap-2">
                  <Badge tone="brand">#{g.project_id}</Badge>
                  <span className="text-sm text-slate-700">
                    {projectName ?? "—"}
                  </span>
                </div>,
                <Link href={`/groups/${g.group_id}`}>
                  <Button size="sm" variant="secondary">View</Button>
                </Link>,
              ],
            };
          })}
        />
      )}

      {groups.loading && list.length > 0 && (
        <div className="flex items-center gap-2 text-xs text-slate-500">
          <Spinner size={12} /> Refreshing…
        </div>
      )}
    </div>
  );
}
