"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ContactsApi, ProjectsApi } from "@/lib/api";
import { useAsync, useMutation } from "@/lib/hooks";
import { useToast } from "@/components/ui/Toast";
import { contactUpdateSchema, type ContactUpdateForm } from "@/lib/validation";
import { PageHeader } from "@/components/layout/PageHeader";
import { PageError } from "@/components/layout/Page";
import { Field, Input, Select, Checkbox } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { Alert } from "@/components/ui/Alert";
import { FullPageSpinner } from "@/components/ui/Spinner";
import { describeError } from "@/lib/errors";

type EditableField = "firstName" | "lastName" | "phone" | "project" | "subscribed";

export default function EditContactPage() {
  const params = useParams<{ id: string }>();
  const id = Number(params.id);
  const router = useRouter();
  const toast = useToast();

  const contact = useAsync(["contact", id], () => ContactsApi.get(id), {
    enabled: Number.isFinite(id) && id > 0,
  });
  const projects = useAsync(["projects"], () => ProjectsApi.list());

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<ContactUpdateForm>({
    resolver: zodResolver(contactUpdateSchema),
    defaultValues: {
      first_name: undefined,
      last_name: undefined,
      country_code: undefined,
      national_number: undefined,
      project_id: undefined,
      is_subscribed: undefined,
    },
  });

  const [enabled, setEnabled] = useState<Record<EditableField, boolean>>({
    firstName: false,
    lastName: false,
    phone: false,
    project: false,
    subscribed: false,
  });

  useEffect(() => {
    if (contact.data) {
      reset({
        first_name: contact.data.first_name,
        last_name: contact.data.last_name,
        country_code: contact.data.country_code,
        national_number: contact.data.national_number,
        project_id: contact.data.project_id,
        is_subscribed: contact.data.is_subscribed,
      });
      setEnabled({
        firstName: false,
        lastName: false,
        phone: false,
        project: false,
        subscribed: false,
      });
    }
  }, [contact.data, reset]);

  const { run, loading, error } = useMutation((input: ContactUpdateForm) =>
    ContactsApi.update(id, input)
  );

  const onSubmit = handleSubmit(async (values) => {
    const payload: ContactUpdateForm = {};
    if (enabled.firstName && values.first_name) payload.first_name = values.first_name.trim();
    if (enabled.lastName && values.last_name) payload.last_name = values.last_name.trim();
    if (enabled.phone) {
      if (values.country_code) payload.country_code = values.country_code.trim();
      if (values.national_number) payload.national_number = values.national_number.trim();
    }
    if (enabled.project && values.project_id && values.project_id > 0) {
      payload.project_id = values.project_id;
    }
    if (enabled.subscribed && typeof values.is_subscribed === "boolean") {
      payload.is_subscribed = values.is_subscribed;
    }
    if (Object.keys(payload).length === 0) {
      toast.push({ tone: "error", title: "Nothing to update" });
      return;
    }
    const updated = await run(payload);
    if (!updated) return;
    toast.push({ tone: "success", title: "Contact updated" });
    router.push(`/contacts/${updated.contact_id}`);
  });

  if (contact.loading && !contact.data) return <FullPageSpinner />;
  if (contact.error && !contact.data) {
    return <PageError error={contact.error} onRetry={contact.refresh} />;
  }
  if (!contact.data) return null;

  const c = contact.data;
  const firstName = watch("first_name") ?? "";
  const lastName = watch("last_name") ?? "";
  const countryCode = watch("country_code") ?? "";
  const nationalNumber = watch("national_number") ?? "";
  const projectId = watch("project_id");
  const isSubscribed = watch("is_subscribed");

  const dirty =
    (enabled.firstName && firstName.trim() !== c.first_name) ||
    (enabled.lastName && lastName.trim() !== c.last_name) ||
    (enabled.phone &&
      (countryCode.trim() !== c.country_code ||
        nationalNumber.trim() !== c.national_number)) ||
    (enabled.project && (projectId ?? 0) !== c.project_id) ||
    (enabled.subscribed && isSubscribed !== c.is_subscribed);

  function toggle(field: EditableField) {
    setEnabled((s) => {
      const next = { ...s, [field]: !s[field] };
      // When re-enabling, reset field to current contact value.
      if (next[field] && contact.data) {
        if (field === "firstName") setValue("first_name", contact.data.first_name);
        if (field === "lastName") setValue("last_name", contact.data.last_name);
        if (field === "phone") {
          setValue("country_code", contact.data.country_code);
          setValue("national_number", contact.data.national_number);
        }
        if (field === "project") setValue("project_id", contact.data.project_id);
        if (field === "subscribed") setValue("is_subscribed", contact.data.is_subscribed);
      }
      return next;
    });
  }

  return (
    <div className="mx-auto max-w-2xl">
      <PageHeader
        title={`Edit “${c.first_name} ${c.last_name}”`}
        description="Toggle each section on, change it, and save. The server rebuilds phoneNumber when you change either part."
        actions={
          <Link href={`/contacts/${id}`}>
            <Button variant="secondary">← Back</Button>
          </Link>
        }
      />

      <form
        onSubmit={onSubmit}
        className="space-y-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-soft"
      >
        {error && <Alert tone="error">{describeError(error)}</Alert>}

        <EditRow
          title="First name"
          enabled={enabled.firstName}
          onToggle={() => toggle("firstName")}
        >
          <Field error={errors.first_name?.message}>
            <Input {...register("first_name")} invalid={!!errors.first_name} maxLength={50} />
          </Field>
        </EditRow>

        <EditRow
          title="Last name"
          enabled={enabled.lastName}
          onToggle={() => toggle("lastName")}
        >
          <Field error={errors.last_name?.message}>
            <Input {...register("last_name")} invalid={!!errors.last_name} maxLength={50} />
          </Field>
        </EditRow>

        <EditRow
          title="Phone number"
          hint="Server recomputes phoneNumber = countryCode (no '+') + nationalNumber."
          enabled={enabled.phone}
          onToggle={() => toggle("phone")}
        >
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="Country code" error={errors.country_code?.message}>
              <Input
                {...register("country_code")}
                invalid={!!errors.country_code}
                placeholder="+91"
                maxLength={5}
              />
            </Field>
            <Field label="National number" error={errors.national_number?.message}>
              <Input
                {...register("national_number")}
                invalid={!!errors.national_number}
                placeholder="9087648930"
                inputMode="numeric"
                maxLength={20}
              />
            </Field>
          </div>
        </EditRow>

        <EditRow
          title="Project"
          enabled={enabled.project}
          onToggle={() => toggle("project")}
        >
          <Field error={errors.project_id?.message}>
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
        </EditRow>

        <EditRow
          title="Subscription"
          enabled={enabled.subscribed}
          onToggle={() => toggle("subscribed")}
        >
          <Checkbox
            {...register("is_subscribed")}
            label="Contact is opted-in to receive messages"
          />
        </EditRow>

        <div className="flex items-center justify-end gap-2 pt-2">
          <Link href={`/contacts/${id}`}>
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

function EditRow({
  title,
  hint,
  enabled,
  onToggle,
  children,
}: {
  title: string;
  hint?: string;
  enabled: boolean;
  onToggle: () => void;
  children: React.ReactNode;
}) {
  return (
    <div className="rounded-xl border border-slate-200 p-4">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm font-medium text-slate-900">{title}</p>
          {hint && <p className="text-xs text-slate-500">{hint}</p>}
        </div>
        <Button
          size="sm"
          variant={enabled ? "secondary" : "primary"}
          type="button"
          onClick={onToggle}
        >
          {enabled ? "Cancel" : "Edit"}
        </Button>
      </div>
      {enabled && <div className="mt-3">{children}</div>}
    </div>
  );
}
