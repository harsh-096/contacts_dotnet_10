// API contract — mirrors ASP.NET DTOs exactly.
// The backend serialises everything in snake_case via JsonNamingPolicy.SnakeCaseLower
// (see Program.cs). All optional/partial-update fields use `| null` so we can
// distinguish "omit from PATCH body" from "explicitly null".

// -------- Envelope --------
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[] | null;
  status_code: number;
}

// -------- Project --------
export interface Project {
  project_id: number;
  project_name: string;
}

export interface CreateProjectInput {
  project_name: string;
}

export type UpdateProjectInput = Partial<CreateProjectInput>;

// -------- Group --------
// A group belongs to a project and can contain many contacts
// via the GroupContacts junction table.
export interface Group {
  group_id: number;
  group_name: string;
  project_id: number;
}

export interface CreateGroupInput {
  group_name: string;
  project_id: number;
}

export interface UpdateGroupInput {
  group_name?: string | null;
  project_id?: number | null;
}

// -------- Contact --------
export interface Contact {
  contact_id: number;
  first_name: string;
  last_name: string;
  country_code: string;
  national_number: string;
  phone_number: number;
  project_id: number;
  is_subscribed: boolean;
  created_date: string;
  updated_date: string | null;
}

export interface CreateContactInput {
  first_name: string;
  last_name: string;
  country_code: string;
  national_number: string;
  project_id: number;
  is_subscribed: boolean;
}

export interface UpdateContactInput {
  first_name?: string | null;
  last_name?: string | null;
  country_code?: string | null;
  national_number?: string | null;
  project_id?: number | null;
  is_subscribed?: boolean | null;
}
