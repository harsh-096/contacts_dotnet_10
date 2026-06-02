"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { GroupsApi, ProjectsApi } from "@/lib/api";
import { useAsync, useMutation } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { groupUpdateSchema, type GroupUpdateForm } from "@/lib/validation";
import { PageHeader } from "@/components/layout/PageHeader";
import { PageError } from "@/components/layout/Page";
import { Field, Input, Select } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { Alert } from "@/components/ui/Alert";
import { FullPageSpinner } from "@/components/ui/Spinner";
import { describeError } from "@/lib/errors";

export default function EditGroupPage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);
  const router = useRouter();
  const toast = useToast();

  const group = useAsync(["group", id], () => GroupsApi.get(id), {
    enabled: Number.isFinite(id) && id > 0,
  });
  const projects = useAsync(["projects"], () => ProjectsApi.list());
  const allGroups = useAsync(["groups"], () => GroupsApi.list());

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<GroupUpdateForm>({
    resolver: zodResolver(groupUpdateSchema),
    defaultValues: { group_name: "", project_id: undefined },
  });

  const [editing, setEditing] = useState<{
    name: boolean;
    project: boolean;
  }>({ name: false, project: false });

  useEffect(() => {
    if (group.data) {
      reset({
        group_name: group.data.group_name,
        project_id: group.data.project_id,
      });
      setEditing({ name: false, project: false });
    }
  }, [group.data, reset]);

  const { run, loading, error } = useMutation((input: GroupUpdateForm) =>
    GroupsApi.update(id, input)
  );

  const onSubmit = handleSubmit(async (values) => {
    const payload: { group_name?: string; project_id?: number } = {};
    if (editing.name) payload.group_name = values.group_name?.trim() ?? "";
    if (editing.project && values.project_id && values.project_id > 0) {
      payload.project_id = values.project_id;
    }
    if (!editing.name && !editing.project) {
      toast.push({ tone: "error", title: "Nothing to update" });
      return;
    }
    const updated = await run(payload);
    if (!updated) return;
    toast.push({ tone: "success", title: "Group updated" });
    router.push(`/groups/${updated.group_id}`);
  });

  if (group.loading && !group.data) return <FullPageSpinner />;
  if (group.error && !group.data) {
    return <PageError error={group.error} onRetry={group.refresh} />;
  }
  if (!group.data) return null;

  const g = group.data;
  const projectId = watch("project_id");
  const projectTaken = (allGroups.data ?? []).some(
    (x) => x.project_id === projectId && x.group_id !== g.group_id
  );
  const nameValue = watch("group_name") ?? "";
  const nameDirty = nameValue.trim() !== g.group_name;
  const projectDirty = (projectId ?? 0) !== g.project_id;
  const canSave = (editing.name && nameDirty) || (editing.project && projectDirty);

  return (
    <div className="mx-auto max-w-xl">
      <PageHeader
        title={`Edit “${g.group_name}”`}
        description="Toggle each section on, change it, and save. The API only updates fields you actually change."
        actions={
          <Link href={`/groups/${id}`}>
            <Button variant="secondary">← Back</Button>
          </Link>
        }
      />

      <form
        onSubmit={onSubmit}
        className="space-y-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-soft"
      >
        {error && <Alert tone="error">{describeError(error)}</Alert>}

        <div className="rounded-xl border border-slate-200 p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-slate-900">Group name</p>
              <p className="text-xs text-slate-500">Current: {g.group_name}</p>
            </div>
            <Button
              size="sm"
              variant={editing.name ? "secondary" : "primary"}
              type="button"
              onClick={() => setEditing((s) => ({ ...s, name: !s.name }))}
            >
              {editing.name ? "Cancel" : "Edit"}
            </Button>
          </div>
          {editing.name && (
            <div className="mt-3">
              <Field error={errors.group_name?.message}>
                <Input
                  {...register("group_name")}
                  invalid={!!errors.group_name}
                  maxLength={255}
                />
              </Field>
            </div>
          )}
        </div>

        <div className="rounded-xl border border-slate-200 p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-slate-900">Project</p>
              <p className="text-xs text-slate-500">
                Current: project #{g.project_id}
              </p>
            </div>
            <Button
              size="sm"
              variant={editing.project ? "secondary" : "primary"}
              type="button"
              onClick={() => setEditing((s) => ({ ...s, project: !s.project }))}
            >
              {editing.project ? "Cancel" : "Move"}
            </Button>
          </div>
          {editing.project && (
            <div className="mt-3">
              <Field
                hint="The target project must not already own a different group, otherwise 409 Conflict is returned."
                error={errors.project_id?.message}
              >
                <Select
                  {...register("project_id", { valueAsNumber: true })}
                  invalid={!!errors.project_id}
                >
                  <option value={0}>Select a project…</option>
                  {(projects.data ?? []).map((p) => (
                    <option key={p.project_id} value={p.project_id}>
                      {p.project_name} (#{p.project_id})
                    </option>
                  ))}
                </Select>
                {projectTaken && (
                  <p className="mt-1 text-xs text-amber-700">
                    ⚠ That project already has a different group.
                  </p>
                )}
              </Field>
            </div>
          )}
        </div>

        <div className="flex items-center justify-end gap-2 pt-2">
          <Link href={`/groups/${id}`}>
            <Button variant="secondary" type="button">Cancel</Button>
          </Link>
          <Button type="submit" loading={loading} disabled={!canSave}>
            Save changes
          </Button>
        </div>
      </form>
    </div>
  );
}
