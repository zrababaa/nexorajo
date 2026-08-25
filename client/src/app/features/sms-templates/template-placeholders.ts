/**
 * Mirrors the server's SMPP.Application.SmsTemplates.TemplatePlaceholders token syntax
 * ([Placeholder]) and SMPP.Infrastructure.Services.TemplateMessageResolver's customer-field
 * list, so the UI can show the same placeholders/required variables the server will compute.
 */
const PLACEHOLDER_PATTERN = /\[([A-Za-z0-9_]+)\]/g;

export const CUSTOMER_FIELDS = ['Name', 'CompanyName', 'Email', 'Phone', 'Address'];

export function extractPlaceholders(body: string): string[] {
  const found = new Set<string>();
  for (const match of body.matchAll(PLACEHOLDER_PATTERN)) {
    found.add(match[1]);
  }
  return [...found];
}

/**
 * Placeholders that aren't resolved per-recipient - from a Customer, or from the selected
 * campaign's imported file columns (`extraKnownFields`, e.g. Name/Amount from a headered
 * CSV/XLSX import) - i.e. must be given a single value up front for the whole send.
 */
export function globalPlaceholdersOf(body: string, extraKnownFields: readonly string[] = []): string[] {
  const knownLower = new Set([...CUSTOMER_FIELDS, ...extraKnownFields].map((f) => f.toLowerCase()));
  return extractPlaceholders(body).filter((p) => !knownLower.has(p.toLowerCase()));
}
