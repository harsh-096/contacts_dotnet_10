// Centralised typed API client. Every method returns the unwrapped `data`
// payload from the ApiResponse<T> envelope and throws a typed ApiError on
// any non-2xx response so React components can render friendly errors.

import type {
  ApiResponse,
  Contact,
  CreateContactInput,
  CreateGroupInput,
  CreateProjectInput,
  Group,
  Project,
  UpdateContactInput,
  UpdateGroupInput,
  UpdateProjectInput,
} from "./types";

// In the browser, the Next.js rewrite at /api/* forwards to the backend.
// During SSR, calls go directly to BACKEND_URL to avoid the dev-server hop.
const baseUrl =
  typeof window === "undefined"
    ? (process.env.BACKEND_URL ?? "http://localhost:5094").replace(/\/+$/, "")
    : "";

function joinUrl(path: string): string {
  const left = baseUrl.replace(/\/+$/, "");
  const right = path.replace(/^\/+/, "");
  return `${left}/${right}`;
}

export class ApiError extends Error {
  status: number;
  errors: string[] | null;

  constructor(message: string, status: number, errors: string[] | null) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.errors = errors;
  }
}

type RequestOptions = Omit<RequestInit, "body"> & { body?: unknown };

async function request<T>(path: string, init?: RequestOptions): Promise<T> {
  const { body, headers, ...rest } = init ?? {};
  const isJsonBody = body !== undefined && !(body instanceof FormData);
  const res = await fetch(joinUrl(path), {
    ...rest,
    headers: {
      Accept: "application/json",
      ...(isJsonBody ? { "Content-Type": "application/json" } : {}),
      ...(headers ?? {}),
    },
    body: (isJsonBody ? JSON.stringify(body) : (body as BodyInit | undefined)) as
      | BodyInit
      | null,
    cache: "no-store",
  });

  let payload: ApiResponse<T> | null = null;
  const text = await res.text();
  if (text) {
    try {
      payload = JSON.parse(text) as ApiResponse<T>;
    } catch {
      // Non-JSON body — fall through to a generic error below.
    }
  }

  if (!res.ok || !payload?.success) {
    const message =
      payload?.message ?? `Request failed with status ${res.status}.`;
    throw new ApiError(message, payload?.status_code ?? res.status, payload?.errors ?? null);
  }

  return payload.data;
}

// -------- Projects --------
export const ProjectsApi = {
  list: () => request<Project[]>("/api/Projects"),
  get: (id: number) => request<Project>(`/api/Projects/${id}`),
  create: (input: CreateProjectInput) =>
    request<Project>("/api/Projects", { method: "POST", body: input }),
  update: (id: number, input: UpdateProjectInput) =>
    request<Project>(`/api/Projects/${id}`, { method: "PUT", body: input }),
  remove: (id: number) =>
    request<boolean>(`/api/Projects/${id}`, { method: "DELETE" }),
};

// -------- Groups --------
export const GroupsApi = {
  list: () => request<Group[]>("/api/Groups"),
  get: (id: number) => request<Group>(`/api/Groups/${id}`),
  byProject: (projectId: number) =>
    request<Group[]>(`/api/Groups/project/${projectId}`),
  create: (input: CreateGroupInput) =>
    request<Group>("/api/Groups", { method: "POST", body: input }),
  update: (id: number, input: UpdateGroupInput) =>
    request<Group>(`/api/Groups/${id}`, { method: "PUT", body: input }),
  remove: (id: number) =>
    request<boolean>(`/api/Groups/${id}`, { method: "DELETE" }),

  contacts: (groupId: number) =>
    request<Contact[]>(`/api/Groups/${groupId}/contacts`),
  addContact: (groupId: number, contactId: number) =>
    request<boolean>(`/api/Groups/${groupId}/contacts/${contactId}`, {
      method: "POST",
    }),
  removeContact: (groupId: number, contactId: number) =>
    request<boolean>(`/api/Groups/${groupId}/contacts/${contactId}`, {
      method: "DELETE",
    }),
};

// -------- Contacts --------
export const ContactsApi = {
  list: () => request<Contact[]>("/api/Contacts"),
  get: (id: number) => request<Contact>(`/api/Contacts/${id}`),
  byProject: (projectId: number) =>
    request<Contact[]>(`/api/Contacts/project/${projectId}`),
  groups: (contactId: number) =>
    request<Group[]>(`/api/Contacts/${contactId}/groups`),
  create: (input: CreateContactInput) =>
    request<Contact>("/api/Contacts", { method: "POST", body: input }),
  update: (id: number, input: UpdateContactInput) =>
    request<Contact>(`/api/Contacts/${id}`, { method: "PUT", body: input }),
  remove: (id: number) =>
    request<boolean>(`/api/Contacts/${id}`, { method: "DELETE" }),
};
