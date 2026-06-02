// Formatting helpers. The backend stores PhoneNumber as a bigint (long) built
// from countryCode (no '+') + nationalNumber, e.g. +91 9087648930 -> 919087648930.
//
// All timestamps coming from the API are emitted in Indian Standard Time
// (UTC+05:30) with an explicit "+05:30" offset (see
// Helpers/UtcDateTimeConverter.cs on the backend). Browsers' Date parser
// honours that offset, so we let toLocaleString render the value in the
// browser's local timezone — and since the offset is unambiguous the
// displayed wall-clock time is correct everywhere.

export function formatPhoneDisplay(countryCode: string, nationalNumber: string) {
  if (!countryCode && !nationalNumber) return "—";
  return `${countryCode} ${nationalNumber}`.trim();
}

export function phoneNumberToDisplay(phoneNumber: number): string {
  if (!phoneNumber) return "—";
  return `+${phoneNumber}`;
}

export function formatDate(value: string | null | undefined): string {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

export function initials(first?: string, last?: string) {
  return `${(first?.[0] ?? "").toUpperCase()}${(last?.[0] ?? "").toUpperCase()}` || "?";
}
