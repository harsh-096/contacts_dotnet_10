"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ContactsApi, ProjectsApi } from "@/lib/api";
import { useAsync, useMutation } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { contactCreateSchema, type ContactCreateForm } from "@/lib/validation";
import { PageHeader } from "@/components/layout/PageHeader";
import { PageError } from "@/components/layout/Page";
import { Field, Input, Select, Checkbox } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { Alert } from "@/components/ui/Alert";
import { FullPageSpinner } from "@/components/ui/Spinner";
import { describeError } from "@/lib/errors";
import { useEffect } from "react";

export default function NewContactPage() {
  const router = useRouter();
  const params = useSearchParams();
  const presetProject = Number(params.get("projectId")) || 0;
  const toast = useToast();

  const projects = useAsync(["projects"], () => ProjectsApi.list());

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<ContactCreateForm>({
    resolver: zodResolver(contactCreateSchema),
    defaultValues: {
      first_name: "",
      last_name: "",
      country_code: "+91",
      national_number: "",
      project_id: presetProject || 0,
      is_subscribed: true,
    },
  });

  useEffect(() => {
    if (presetProject) setValue("project_id", presetProject);
  }, [presetProject, setValue]);

  const { run, loading, error } = useMutation((input: ContactCreateForm) =>
    ContactsApi.create(input)
  );

  const onSubmit = handleSubmit(async (values) => {
    const created = await run(values);
    if (!created) return;
    toast.push({ tone: "success", title: "Contact created" });
    router.push(`/contacts/${created.contact_id}`);
  });

  if (projects.loading && !projects.data) return <FullPageSpinner />;
  if (projects.error && !projects.data) {
    return <PageError error={projects.error} onRetry={projects.refresh} />;
  }

  return (
    <div className="mx-auto max-w-2xl">
      <PageHeader
        title="New contact"
        description="PhoneNumber is composed server-side from countryCode + nationalNumber and must be unique."
        actions={
          <Link href="/contacts">
            <Button variant="secondary">← Back</Button>
          </Link>
        }
      />

      <form
        onSubmit={onSubmit}
        className="space-y-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-soft"
      >
        {error && <Alert tone="error">{describeError(error)}</Alert>}

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field label="First name" required error={errors.first_name?.message}>
            <Input
              {...register("first_name")}
              invalid={!!errors.first_name}
              maxLength={50}
              autoFocus
            />
          </Field>
          <Field label="Last name" required error={errors.last_name?.message}>
            <Input
              {...register("last_name")}
              invalid={!!errors.last_name}
              maxLength={50}
            />
          </Field>
        </div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field
            label="Country code"
            required
            hint="e.g. +91, +1, +971. Include the leading +."
            error={errors.country_code?.message}
          >
            <Input
              {...register("country_code")}
              invalid={!!errors.country_code}
              placeholder="+91"
              maxLength={5}
            />
          </Field>
          <Field
            label="National number"
            required
            hint="Digits only, 4-20 characters."
            error={errors.national_number?.message}
          >
            <Input
              {...register("national_number")}
              invalid={!!errors.national_number}
              placeholder="9087648930"
              inputMode="numeric"
              maxLength={20}
            />
          </Field>
        </div>

        <Field label="Project" required error={errors.project_id?.message}>
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
        </Field>

        <Field label="Subscription">
          <Checkbox
            {...register("is_subscribed")}
            label="Contact is opted-in to receive messages"
          />
        </Field>

        <div className="flex items-center justify-end gap-2 pt-2">
          <Link href="/contacts">
            <Button variant="secondary" type="button">Cancel</Button>
          </Link>
          <Button type="submit" loading={loading}>Create contact</Button>
        </div>
      </form>
    </div>
  );
}
