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

/** Placeholders that aren't resolved per-recipient from a Customer, i.e. must be given a value at send time. */
export function globalPlaceholdersOf(body: string): string[] {
  const customerFieldsLower = new Set(CUSTOMER_FIELDS.map((f) => f.toLowerCase()));
  return extractPlaceholders(body).filter((p) => !customerFieldsLower.has(p.toLowerCase()));
}
