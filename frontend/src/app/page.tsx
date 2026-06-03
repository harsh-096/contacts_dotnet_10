"use client";

import Link from "next/link";
import { useState, useMemo } from "react";
import { ContactsApi, GroupsApi, ProjectsApi } from "@/lib/api";
import { useAsync, useMutation } from "@/lib/hooks";
import { PageHeader } from "@/components/layout/PageHeader";
import { PageError } from "@/components/layout/Page";
import { FullPageSpinner, Spinner } from "@/components/ui/Spinner";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import { Input, Field, Select } from "@/components/ui/Input";
import { initials, formatPhoneDisplay } from "@/lib/format";
import { cn } from "@/lib/utils";
import { useToast } from "@/components/ui/Toast";
import type { Project, Contact, Group } from "@/lib/types";

export default function DashboardPage() {
  const projects = useAsync(["projects"], () => ProjectsApi.list());
  const contacts = useAsync(["contacts"], () => ContactsApi.list());
  const groups = useAsync(["groups"], () => GroupsApi.list());

  const loading = projects.loading || contacts.loading || groups.loading;
  const error = projects.error || contacts.error || groups.error;

  const [expandedProjects, setExpandedProjects] = useState<Set<number>>(new Set());
  const [expandedGroups, setExpandedGroups] = useState<Set<number>>(new Set());

  const projectContacts = useMemo(() => {
    const map = new Map<number, Contact[]>();
    if (!projects.data) return map;
    for (const p of projects.data) {
      map.set(p.project_id, contacts.data?.filter((c) => c.project_id === p.project_id) ?? []);
    }
    return map;
  }, [projects.data, contacts.data]);

  const projectGroups = useMemo(() => {
    const map = new Map<number, Group[]>();
    if (!projects.data) return map;
    for (const p of projects.data) {
      map.set(p.project_id, groups.data?.filter((g) => g.project_id === p.project_id) ?? []);
    }
    return map;
  }, [projects.data, groups.data]);

  const toggleProject = (id: number) => {
    setExpandedProjects((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  };

  const toggleGroup = (id: number) => {
    setExpandedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  };

  if (loading && !projects.data) {
    return <FullPageSpinner />;
  }

  if (error && !projects.data) {
    return <PageError error={error} onRetry={() => { projects.refresh(); contacts.refresh(); groups.refresh(); }} />;
  }

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <PageHeader
        title="Dashboard"
        description="Browse your projects, the people in them, and the group specialities they form."
      />

      {(!projects.data || projects.data.length === 0) ? (
        <div className="rounded-2xl border border-slate-200 bg-white p-12 text-center shadow-soft">
          <p className="text-lg font-medium text-slate-900">No projects yet</p>
          <p className="mt-1 text-sm text-slate-500">Create your first project to get started.</p>
          <Link
            href="/projects/new"
            className="mt-4 inline-flex h-10 items-center rounded-lg bg-brand-600 px-4 text-sm font-medium text-white hover:bg-brand-700"
          >
            + New Project
          </Link>
        </div>
      ) : (
        <div className="space-y-3">
          {projects.data.map((project) => {
            const projectContactList = projectContacts.get(project.project_id) ?? [];
            const projectGroupList = projectGroups.get(project.project_id) ?? [];
            const isProjectOpen = expandedProjects.has(project.project_id);

            return (
              <div key={project.project_id} className="rounded-2xl border border-slate-200 bg-white shadow-soft">
                <button
                  type="button"
                  onClick={() => toggleProject(project.project_id)}
                  className="flex w-full items-center gap-3 px-5 py-4 text-left transition hover:bg-slate-50"
                >
                  <Chevron open={isProjectOpen} />
                  <span className="text-lg">📁</span>
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-semibold text-slate-900">{project.project_name}</p>
                    <p className="text-xs text-slate-500">Project #{project.project_id}</p>
                  </div>
                  <Badge tone="brand">{projectContactList.length} contact{projectContactList.length !== 1 ? "s" : ""}</Badge>
                  <Badge>{projectGroupList.length} group{projectGroupList.length !== 1 ? "s" : ""}</Badge>
                  <Link
                    href={`/projects/${project.project_id}`}
                    onClick={(e) => e.stopPropagation()}
                    className="text-xs font-medium text-brand-600 hover:text-brand-700 hover:underline"
                  >
                    View
                  </Link>
                </button>

                {isProjectOpen && (
                  <div className="border-t border-slate-100 px-5 pb-4 pt-3">
                    {/* Contacts section */}
                    {projectContactList.length === 0 ? (
                      <div className="py-3 text-center text-sm text-slate-500">
                        No contacts in this project.
                        <Link href={`/contacts/new?projectId=${project.project_id}`} className="ml-1 text-brand-600 hover:underline">
                          Add one
                        </Link>
                      </div>
                    ) : (
                      <div className="mb-3">
                        <p className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-500">
                          People ({projectContactList.length})
                        </p>
                        <div className="space-y-1">
                          {projectContactList.map((contact) => (
                            <div
                              key={contact.contact_id}
                              className="flex items-center gap-2 rounded-lg bg-slate-50 px-3 py-2"
                            >
                              <div className="flex h-7 w-7 items-center justify-center rounded-full bg-brand-100 text-xs font-semibold text-brand-700">
                                {initials(contact.first_name, contact.last_name)}
                              </div>
                              <Link
                                href={`/contacts/${contact.contact_id}`}
                                className="text-sm font-medium text-slate-900 hover:text-brand-600 hover:underline"
                              >
                                {contact.first_name} {contact.last_name}
                              </Link>
                              <span className="hidden text-xs text-slate-400 sm:inline">
                                {formatPhoneDisplay(contact.country_code, contact.national_number)}
                              </span>
                              {contact.is_subscribed ? (
                                <Badge tone="green">Subscribed</Badge>
                              ) : (
                                <Badge tone="slate">Unsubscribed</Badge>
                              )}
                              <Link
                                href={`/contacts/${contact.contact_id}`}
                                className="ml-auto rounded-md px-2 py-1 text-xs text-slate-500 hover:bg-slate-100 hover:text-slate-700"
                              >
                                View
                              </Link>
                            </div>
                          ))}
                          <div className="pt-1 text-center">
                            <Link
                              href={`/contacts/new?projectId=${project.project_id}`}
                              className="inline-flex items-center gap-1 text-xs font-medium text-brand-600 hover:text-brand-700 hover:underline"
                            >
                              + Add person to {project.project_name}
                            </Link>
                          </div>
                        </div>
                      </div>
                    )}

                    {/* Groups section */}
                    {projectGroupList.length === 0 ? (
                      <div className="py-2 text-center text-sm text-slate-500">
                        No groups in this project.
                        <Link href={`/groups/new?projectId=${project.project_id}`} className="ml-1 text-brand-600 hover:underline">
                          Create one
                        </Link>
                      </div>
                    ) : (
                      <div>
                        <p className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-500">
                          Groups ({projectGroupList.length})
                        </p>
                        <div className="space-y-1">
                          {projectGroupList.map((group) => (
                            <GroupNode
                              key={group.group_id}
                              group={group}
                              isOpen={expandedGroups.has(group.group_id)}
                              onToggle={() => toggleGroup(group.group_id)}
                            />
                          ))}
                          <div className="pt-1 text-center">
                            <Link
                              href={`/groups/new?projectId=${project.project_id}`}
                              className="inline-flex items-center gap-1 text-xs font-medium text-brand-600 hover:text-brand-700 hover:underline"
                            >
                              + New group in {project.project_name}
                            </Link>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Summary row */}
      {projects.data && projects.data.length > 0 && (
        <div className="grid grid-cols-3 gap-3 text-center text-sm text-slate-500">
          <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-soft">
            <span className="font-semibold text-slate-900">{projects.data.length}</span> Projects
          </div>
          <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-soft">
            <span className="font-semibold text-slate-900">{contacts.data?.length ?? 0}</span> People
          </div>
          <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-soft">
            <span className="font-semibold text-slate-900">{groups.data?.length ?? 0}</span> Groups
          </div>
        </div>
      )}
    </div>
  );
}

function Chevron({ open }: { open: boolean }) {
  return (
    <svg
      className={cn("h-4 w-4 shrink-0 text-slate-400 transition-transform", open && "rotate-90")}
      fill="none"
      viewBox="0 0 24 24"
      stroke="currentColor"
      strokeWidth={2}
    >
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
    </svg>
  );
}

function GroupNode({
  group,
  isOpen,
  onToggle,
}: {
  group: Group;
  isOpen: boolean;
  onToggle: () => void;
}) {
  const members = useAsync(
    ["group", group.group_id, "members"],
    () => GroupsApi.getContacts(group.group_id),
    { enabled: isOpen }
  );

  return (
    <div className="rounded-lg border border-slate-100 bg-white">
      <button
        type="button"
        onClick={onToggle}
        className="flex w-full items-center gap-2 px-3 py-2 text-left hover:bg-slate-50"
      >
        <Chevron open={isOpen} />
        <span className="text-sm">👥</span>
        <span className="text-sm font-medium text-slate-900">{group.group_name}</span>
        <span className="text-xs text-slate-400">Group #{group.group_id}</span>
        <Link
          href={`/groups/${group.group_id}`}
          onClick={(e) => e.stopPropagation()}
          className="ml-auto rounded-md px-2 py-1 text-xs text-slate-500 hover:bg-slate-100 hover:text-slate-700"
        >
          View
        </Link>
      </button>
      {isOpen && (
        <div className="border-t border-slate-100 px-3 pb-2 pt-1">
          {members.loading ? (
            <div className="flex items-center justify-center py-2">
              <Spinner size={14} />
            </div>
          ) : !members.data || members.data.length === 0 ? (
            <p className="py-2 text-center text-xs text-slate-400">No members in this group.</p>
          ) : (
            <div className="space-y-1">
              {members.data.map((m) => (
                <div key={m.contact_id} className="flex items-center gap-2 rounded bg-slate-50 px-3 py-1.5">
                  <div className="flex h-6 w-6 items-center justify-center rounded-full bg-brand-100 text-[10px] font-semibold text-brand-700">
                    {initials(m.first_name, m.last_name)}
                  </div>
                  <Link
                    href={`/contacts/${m.contact_id}`}
                    className="text-sm text-slate-700 hover:text-brand-600 hover:underline"
                  >
                    {m.first_name} {m.last_name}
                  </Link>
                  {m.is_subscribed ? (
                    <Badge tone="green">Subscribed</Badge>
                  ) : (
                    <Badge tone="slate">Unsubscribed</Badge>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
