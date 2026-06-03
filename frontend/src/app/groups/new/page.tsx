"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ProjectsApi } from "@/lib/api";
import { useAsync, useMutation } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { groupCreateSchema, type GroupCreateForm } from "@/lib/validation";
import { PageHeader } from "@/components/layout/PageHeader";
import { PageError } from "@/components/layout/Page";
import { Field, Input, Select } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { Alert } from "@/components/ui/Alert";
import { FullPageSpinner } from "@/components/ui/Spinner";
import { describeError } from "@/lib/errors";
import { GroupsApi } from "@/lib/api";

export default function NewGroupPage() {
  const router = useRouter();
  const params = useSearchParams();
  const presetProject = Number(params.get("projectId")) || 0;
  const toast = useToast();

  const projects = useAsync(["projects"], () => ProjectsApi.list());

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<GroupCreateForm>({
    resolver: zodResolver(groupCreateSchema),
    defaultValues: {
      group_name: "",
      project_id: presetProject || 0,
    },
  });

  useEffect(() => {
    if (presetProject) setValue("project_id", presetProject);
  }, [presetProject, setValue]);

  const { run, loading, error } = useMutation((input: GroupCreateForm) =>
    GroupsApi.create(input)
  );

  const onSubmit = handleSubmit(async (values) => {
    const created = await run(values);
    if (!created) return;
    toast.push({ tone: "success", title: "Group created" });
    router.push(`/groups/${created.group_id}`);
  });

  if (projects.loading && !projects.data) return <FullPageSpinner />;
  if (projects.error && !projects.data) {
    return <PageError error={projects.error} onRetry={projects.refresh} />;
  }

  const projectList = projects.data ?? [];

  return (
    <div className="mx-auto max-w-xl">
      <PageHeader
        title="New group"
        description="Groups are specialities that belong to a project (e.g. DatabaseDesign, Frontend, Backend)."
        actions={
          <Link href="/groups">
            <Button variant="secondary">← Back</Button>
          </Link>
        }
      />

      <form
        onSubmit={onSubmit}
        className="space-y-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-soft"
      >
        {error && <Alert tone="error">{describeError(error)}</Alert>}

        <Field label="Group name" required error={errors.group_name?.message}>
          <Input
            {...register("group_name")}
            placeholder="e.g. DatabaseDesign"
            invalid={!!errors.group_name}
            maxLength={255}
            autoFocus
          />
        </Field>

        <Field label="Project" required error={errors.project_id?.message} hint="A project can have many groups.">
          <Select
            {...register("project_id", { valueAsNumber: true })}
            invalid={!!errors.project_id}
          >
            <option value={0}>Select a project…</option>
            {projectList.map((p) => (
              <option key={p.project_id} value={p.project_id}>
                {p.project_name}
              </option>
            ))}
          </Select>
        </Field>

        <div className="flex items-center justify-end gap-2 pt-2">
          <Link href="/groups">
            <Button variant="secondary" type="button">Cancel</Button>
          </Link>
          <Button type="submit" loading={loading}>Create group</Button>
        </div>
      </form>
    </div>
  );
}
