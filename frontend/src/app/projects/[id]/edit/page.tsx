"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ProjectsApi } from "@/lib/api";
import { useAsync, useMutation } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { projectUpdateSchema, type ProjectUpdateForm } from "@/lib/validation";
import { PageHeader } from "@/components/layout/PageHeader";
import { PageError } from "@/components/layout/Page";
import { Field, Input } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { Alert } from "@/components/ui/Alert";
import { FullPageSpinner } from "@/components/ui/Spinner";
import { describeError } from "@/lib/errors";

export default function EditProjectPage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);
  const router = useRouter();
  const toast = useToast();

  const project = useAsync(["project", id], () => ProjectsApi.get(id), {
    enabled: Number.isFinite(id) && id > 0,
  });

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<ProjectUpdateForm>({
    resolver: zodResolver(projectUpdateSchema),
    defaultValues: { project_name: "" },
  });

  useEffect(() => {
    if (project.data) reset({ project_name: project.data.project_name });
  }, [project.data, reset]);

  const { run, loading, error } = useMutation((input: ProjectUpdateForm) =>
    ProjectsApi.update(id, input)
  );

  const onSubmit = handleSubmit(async (values) => {
    // Only send the field if the user actually edited it.
    const trimmed = values.project_name?.trim() ?? "";
    if (!trimmed) {
      toast.push({ tone: "error", title: "ProjectName is required." });
      return;
    }
    const updated = await run({ project_name: trimmed });
    if (!updated) return;
    toast.push({ tone: "success", title: "Project updated" });
    router.push(`/projects/${updated.project_id}`);
  });

  if (project.loading && !project.data) return <FullPageSpinner />;
  if (project.error && !project.data) {
    return <PageError error={project.error} onRetry={project.refresh} />;
  }
  if (!project.data) return null;

  const original = project.data.project_name;
  const current = watch("project_name") ?? "";
  const dirty = current.trim() !== original;

  return (
    <div className="mx-auto max-w-xl">
      <PageHeader
        title={`Edit “${original}”`}
        description="Provide at least one field. Other fields are left untouched."
        actions={
          <Link href={`/projects/${id}`}>
            <Button variant="secondary">← Back</Button>
          </Link>
        }
      />

      <form
        onSubmit={onSubmit}
        className="space-y-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-soft"
      >
        {error && <Alert tone="error">{describeError(error)}</Alert>}

        <Field label="Project name" required error={errors.project_name?.message}>
          <Input
            {...register("project_name")}
            invalid={!!errors.project_name}
            maxLength={255}
            autoFocus
          />
        </Field>

        <div className="flex items-center justify-end gap-2 pt-2">
          <Link href={`/projects/${id}`}>
            <Button variant="secondary" type="button">Cancel</Button>
          </Link>
          <Button type="submit" loading={loading} disabled={!dirty}>
            Save changes
          </Button>
        </div>
      </form>
    </div>
  );
}
