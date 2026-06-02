# Contact System — Frontend

A production-ready Next.js 14 (App Router, TypeScript, Tailwind) frontend for the
ASP.NET 10 Contact Management API in `../ContactSystem`.

It implements full CRUD for **Projects**, **Groups** and **Contacts**, including
the cross-entity relationships (project → groups/contacts, group ↔ contacts
membership, contact → group memberships), exactly as exposed by the backend.

## 1. Architecture at a glance

```
Browser  ──same-origin──▶  Next.js dev server  ──proxy /api/*──▶  ASP.NET 10 API
   :3000                        :3000                            :5094
```

The Next.js dev server proxies every `/api/*` request to the ASP.NET backend.
That means:

- The browser never makes a cross-origin call, so **CORS is not required** on the
  backend. You don't need to touch `Program.cs` to add `AddCors()`.
- The target backend URL is configured via the `BACKEND_URL` env var (see
  `.env.local.example`).

The proxy lives in `next.config.mjs`:

```js
async rewrites() {
  const target = process.env.BACKEND_URL?.replace(/\/+$/, "") ?? "http://localhost:5094";
  return [{ source: "/api/:path*", destination: `${target}/api/:path*` }];
}
```

## 2. Quick start

```bash
# 1. Start the backend (from the parent folder)
cd ../ContactSystem
dotnet run --project ContactSystem.csproj
# → should listen on http://localhost:5094

# 2. Start the frontend
cd ../frontend
cp .env.local.example .env.local       # adjust BACKEND_URL if needed
npm install
npm run dev
# → http://localhost:3000
```

Open <http://localhost:3000>. The dashboard shows live counts of
projects / groups / contacts from the API.

## 3. Environment variables

| Variable      | Default                   | Purpose                                  |
| ------------- | ------------------------- | ---------------------------------------- |
| `BACKEND_URL` | `http://localhost:5094`   | Where `/api/*` requests are proxied to.  |

Only `BACKEND_URL` is used (in `next.config.mjs`). The frontend itself is
configured at runtime via `process.env.BACKEND_URL`, so production deployments
work the same way as local dev.

## 4. What's implemented

### API surface (matches the backend exactly)

The backend serialises everything in `snake_case` (`JsonNamingPolicy.SnakeCaseLower`)
and wraps every response in `ApiResponse<T>`. The frontend mirrors that:

```ts
interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[] | null;
  status_code: number;
}
```

`src/lib/api.ts` is a single typed client:

| Resource     | Operations                                                                                        |
| ------------ | -------------------------------------------------------------------------------------------------- |
| `ProjectsApi` | `list`, `get`, `create`, `update` (partial), `remove`                                             |
| `GroupsApi`   | `list`, `get`, `byProject`, `contacts` (members), `create`, `update` (partial), `remove`, `addContact`, `removeContact` |
| `ContactsApi` | `list`, `get`, `byProject`, `groups` (memberships), `create`, `update` (partial), `remove`         |

The `ApiError` thrown by the client preserves `status_code` and the
backend's `errors[]` list, which is rendered verbatim in the UI.

### Pages

| Path                        | What it does                                                                 |
| --------------------------- | ------------------------------------------------------------------------------ |
| `/`                         | Dashboard with KPI tiles and a recent-contacts list.                          |
| `/projects`                 | List + delete. Shows group and contact counts per project.                    |
| `/projects/new`             | Create a project (validates `projectName` 1-255 chars).                       |
| `/projects/[id]`            | Detail view with linked group, contacts table, and quick actions.             |
| `/projects/[id]/edit`       | Partial-update form (only sends changed fields).                              |
| `/groups`                   | List with project name lookup.                                                |
| `/groups/new`               | Create a group (validates `groupName` 1-255, requires `projectId`, warns if project already has a group). |
| `/groups/[id]`              | Detail with member list, "add contact" picker scoped to the same project, remove from group. |
| `/groups/[id]/edit`         | Partial update: rename, move to another project.                              |
| `/contacts`                 | List with search, project filter, status filter, delete.                      |
| `/contacts/new`             | Create a contact (validates all backend DTO rules).                           |
| `/contacts/[id]`            | Detail with subscription toggle, group memberships, timestamps.                |
| `/contacts/[id]/edit`       | Per-field partial update — country code & national number toggle together; the server rebuilds `phoneNumber` automatically. |

### Cross-entity relationships in the UI

- **Project → Group** (1:1): shown on the project detail page. The "no group yet"
  card links straight to the create form with `?projectId=…` pre-filled.
- **Project → Contacts** (1:N): shown on the project detail page; can also be
  filtered on the contacts list.
- **Group ↔ Contact** (M:N via `GroupContacts`):
  - Group detail shows all members with "remove from group".
  - Group detail has an "add contact" picker that lists only contacts belonging
    to the same project (which is what the backend requires for 200 OK).
  - Contact detail shows every group the contact is in.

### Loading / error / empty / success states

- All lists show a full-page spinner on first load, an inline spinner while
  refreshing, and a friendly `EmptyState` when the response is empty.
- `Alert` surfaces API errors (with the server's `errors[]` list when present),
  and `Toast` shows a brief confirmation on every successful create/update/delete.
- Forms block the submit button while in flight and disable it when no fields
  have been edited (so partial updates send only what changed).
- A global `error.tsx` catches unexpected render errors with a "try again" button.

### Validation

Zod schemas in `src/lib/validation.ts` mirror the backend's
`DataAnnotations` rules exactly:

- `projectName` / `groupName`: 1-255 characters.
- `firstName` / `lastName`: 1-50 characters.
- `countryCode`: 2-5 characters, regex `^\+[1-9]\d{0,3}$` (e.g. `+1`, `+91`, `+971`).
- `nationalNumber`: 4-20 digits, digits only.
- `projectId`: positive integer (always required for groups; required for contacts).
- Update DTOs use `AtLeastOne` to forbid empty PATCH bodies — the form UI also
  blocks submission until at least one field is dirty.

## 5. Project structure

```
frontend/
├── next.config.mjs          # /api/* → BACKEND_URL rewrite
├── tailwind.config.ts
├── tsconfig.json
├── .env.local.example
└── src/
    ├── app/
    │   ├── layout.tsx                # Sidebar, mobile nav, toast + confirm hosts
    │   ├── page.tsx                  # Dashboard
    │   ├── error.tsx                 # Global error boundary
    │   ├── loading.tsx               # Global loading state
    │   ├── not-found.tsx
    │   ├── projects/
    │   │   ├── page.tsx              # List
    │   │   ├── new/page.tsx
    │   │   └── [id]/{page,edit/page}.tsx
    │   ├── groups/…
    │   └── contacts/…
    ├── components/
    │   ├── layout/
    │   │   ├── Sidebar.tsx
    │   │   ├── MobileNav.tsx
    │   │   ├── PageHeader.tsx
    │   │   ├── Page.tsx              # PageError / Section helpers
    │   │   └── ConfirmHost.tsx       # Singleton confirm dialog
    │   └── ui/
    │       ├── Alert.tsx
    │       ├── Badge.tsx
    │       ├── Button.tsx
    │       ├── Card.tsx
    │       ├── DataTable.tsx
    │       ├── EmptyState.tsx
    │       ├── Input.tsx             # Field / Input / Select / Checkbox / Textarea
    │       ├── Modal.tsx
    │       ├── Spinner.tsx
    │       └── Toast.tsx
    └── lib/
        ├── api.ts                    # Typed API client (one file per resource)
        ├── errors.ts                 # describeError()
        ├── format.ts                 # Phone / date / initials helpers
        ├── hooks.ts                  # useAsync / useMutation
        ├── types.ts                  # DTOs + ApiResponse<T>
        ├── utils.ts                  # cn() helper
        └── validation.ts             # Zod schemas (mirror backend rules)
```

## 6. Scripts

```bash
npm run dev        # Start the dev server on :3000
npm run build      # Production build
npm run start      # Run the production build
npm run lint       # Next.js / ESLint
npm run typecheck  # tsc --noEmit
```

## 7. How the UI is wired to the backend (no guessing)

Every UI action calls the same endpoint the backend exposes; there is no
synthetic layer in between. Cross-reference the controllers in
`../ContactSystem/Controllers/` with the methods in `src/lib/api.ts`:

| UI action                | Backend route                                  |
| ------------------------ | ---------------------------------------------- |
| List projects            | `GET    /api/Projects`                         |
| Create project           | `POST   /api/Projects`                         |
| Update project           | `PUT    /api/Projects/{id}`                    |
| Delete project           | `DELETE /api/Projects/{id}`                    |
| List groups              | `GET    /api/Groups`                           |
| Get group                | `GET    /api/Groups/{id}`                      |
| Get group by project     | `GET    /api/Groups/project/{projectId}`       |
| Get contacts in group    | `GET    /api/Groups/{groupId}/contacts`        |
| Create group             | `POST   /api/Groups`                           |
| Update group             | `PUT    /api/Groups/{id}`                      |
| Delete group             | `DELETE /api/Groups/{id}`                      |
| Add contact to group     | `POST   /api/Groups/{groupId}/contacts/{contactId}` |
| Remove contact from group| `DELETE /api/Groups/{groupId}/contacts/{contactId}` |
| List contacts            | `GET    /api/Contacts`                         |
| Get contact              | `GET    /api/Contacts/{id}`                    |
| Contacts by project      | `GET    /api/Contacts/project/{projectId}`     |
| Groups for a contact     | `GET    /api/Contacts/{contactId}/groups`      |
| Create contact           | `POST   /api/Contacts`                         |
| Update contact           | `PUT    /api/Contacts/{id}`                    |
| Delete contact           | `DELETE /api/Contacts/{id}`                    |

Every request body and response type matches the backend DTOs
(`CreateContactDto`, `UpdateContactDto`, `ContactResponseDto`, etc.).

## 8. Notes / future work

- Authentication is intentionally not implemented; the backend has no auth
  middleware. Add an `Authorization` header in `src/lib/api.ts` (and a token
  store) if you wire up auth later — the client is centralised, so it's a
  one-file change.
- Pagination, sorting and search are not exposed by the backend yet, so the
  contacts list filters client-side. When the backend adds paginated endpoints,
  swap the `useAsync` calls for paginated ones.
- The frontend never sends `phoneNumber`; the server always rebuilds it.
  Form inputs only edit `countryCode` + `nationalNumber`.
