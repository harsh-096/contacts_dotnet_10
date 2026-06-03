"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ProjectsApi } from "@/lib/api";
import { useMutation } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { projectCreateSchema, type ProjectCreateForm } from "@/lib/validation";
import { PageHeader } from "@/components/layout/PageHeader";
import { Field, Input } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { Alert } from "@/components/ui/Alert";
import { describeError } from "@/lib/errors";

export default function NewProjectPage() {
  const router = useRouter();
  const toast = useToast();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ProjectCreateForm>({
    resolver: zodResolver(projectCreateSchema),
    defaultValues: { project_name: "" },
  });

  const { run, loading, error } = useMutation((input: ProjectCreateForm) =>
    ProjectsApi.create(input)
  );

  const onSubmit = handleSubmit(async (values) => {
    const created = await run(values);
    if (!created) return;
    toast.push({ tone: "success", title: "Project created" });
    router.push(`/projects/${created.project_id}`);
  });

  return (
    <div className="mx-auto max-w-xl">
      <PageHeader
        title="New project"
        description="A project is the top-level container for contacts. Each contact can own groups."
        actions={
          <Link href="/projects">
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
            placeholder="e.g. Apollo"
            invalid={!!errors.project_name}
            maxLength={255}
            autoFocus
          />
        </Field>

        <div className="flex items-center justify-end gap-2 pt-2">
          <Link href="/projects">
            <Button variant="secondary" type="button">Cancel</Button>
          </Link>
          <Button type="submit" loading={loading}>Create project</Button>
        </div>
      </form>
    </div>
  );
}
