import { ApiError } from "@/lib/api";

// Convert any thrown error into a friendly message.
export function describeError(err: unknown): string {
  if (!err) return "Something went wrong.";
  if (err instanceof ApiError) {
    if (err.errors && err.errors.length) {
      return err.errors.join("\n");
    }
    return err.message || `Request failed (${err.status}).`;
  }
  if (err instanceof Error) return err.message;
  return "Unexpected error.";
}
