// Zod validation schemas that mirror the backend's DataAnnotations rules
// exactly. We use them in React Hook Form so users see the same errors the
// API would return — but we also do a final preflight here so the API
// receives only valid payloads.

import { z } from "zod";

export const nameField = (label: string, max = 50) =>
  z
    .string()
    .trim()
    .min(1, `${label} is required.`)
    .max(max, `${label} must be 1-${max} characters.`);

export const longNameField = (label: string) =>
  z
    .string()
    .trim()
    .min(1, `${label} is required.`)
    .max(255, `${label} must be 1-255 characters.`);

// countryCode: matches backend regex ^\+[1-9]\d{0,3}$ and length 2-5.
export const countryCodeField = z
  .string()
  .trim()
  .min(2, "CountryCode must be 2-5 characters including the leading '+'.")
  .max(5, "CountryCode must be 2-5 characters including the leading '+'.")
  .regex(
    /^\+[1-9]\d{0,3}$/,
    "CountryCode must start with '+' followed by 1-4 digits (e.g. +1, +91, +971)."
  );

// nationalNumber: digits only, 4-20 chars.
export const nationalNumberField = z
  .string()
  .trim()
  .min(4, "NationalNumber must be 4-20 digits.")
  .max(20, "NationalNumber must be 4-20 digits.")
  .regex(
    /^\d+$/,
    "NationalNumber must contain digits only (no '+', spaces, or dashes)."
  );

// A project id: must be a positive integer.
export const projectIdField = z
  .number({ invalid_type_error: "Project is required." })
  .int()
  .positive("Project is required.");

// A contact id: must be a positive integer.
export const contactIdField = z
  .number({ invalid_type_error: "Contact is required." })
  .int()
  .positive("Contact is required.");

// -------- Projects --------
export const projectCreateSchema = z.object({
  project_name: longNameField("ProjectName"),
});

export const projectUpdateSchema = z
  .object({
    project_name: longNameField("ProjectName"),
  })
  .partial()
  .refine((v) => Object.values(v).some((x) => x !== undefined), {
    message: "At least one field (projectName) must be provided.",
  });

// -------- Groups --------
// A group belongs to a project and can contain many contacts
// via the GroupContacts junction table.
export const groupCreateSchema = z.object({
  group_name: longNameField("GroupName"),
  project_id: projectIdField,
});

export const groupUpdateSchema = z
  .object({
    group_name: longNameField("GroupName").optional(),
    project_id: z
      .number({ invalid_type_error: "ProjectId must be a positive integer." })
      .int()
      .positive("ProjectId must be a positive integer.")
      .optional(),
  })
  .refine(
    (v) => v.group_name !== undefined || v.project_id !== undefined,
    { message: "At least one field (groupName, projectId) must be provided." }
  );

// -------- Contacts --------
export const contactCreateSchema = z.object({
  first_name: nameField("FirstName"),
  last_name: nameField("LastName"),
  country_code: countryCodeField,
  national_number: nationalNumberField,
  project_id: projectIdField,
  is_subscribed: z.boolean().default(true),
});

export const contactUpdateSchema = z
  .object({
    first_name: nameField("FirstName").optional(),
    last_name: nameField("LastName").optional(),
    country_code: countryCodeField.optional(),
    national_number: nationalNumberField.optional(),
    project_id: z.number().int().positive().optional(),
    is_subscribed: z.boolean().optional(),
  })
  .refine(
    (v) =>
      [
        v.first_name,
        v.last_name,
        v.country_code,
        v.national_number,
        v.project_id,
        v.is_subscribed,
      ].some((x) => x !== undefined),
    {
      message:
        "At least one field (firstName, lastName, countryCode, nationalNumber, projectId, isSubscribed) must be provided.",
    }
  );

// Inferred TS types
export type ProjectCreateForm = z.infer<typeof projectCreateSchema>;
export type ProjectUpdateForm = z.infer<typeof projectUpdateSchema>;
export type GroupCreateForm = z.infer<typeof groupCreateSchema>;
export type GroupUpdateForm = z.infer<typeof groupUpdateSchema>;
export type ContactCreateForm = z.infer<typeof contactCreateSchema>;
export type ContactUpdateForm = z.infer<typeof contactUpdateSchema>;
