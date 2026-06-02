// Formatting helpers. The backend stores PhoneNumber as a bigint (long) built
// from countryCode (no '+') + nationalNumber, e.g. +91 9087648930 -> 919087648930.

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

export function initials(first?: string, last?: string) {
  return `${(first?.[0] ?? "").toUpperCase()}${(last?.[0] ?? "").toUpperCase()}` || "?";
}
