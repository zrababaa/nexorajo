/** Drops nullish/empty values and stringifies the rest, so callers can pass filter objects straight to HttpParams. */
export function cleanParams(obj: Record<string, string | number | boolean | null | undefined>): Record<string, string> {
  const result: Record<string, string> = {};
  for (const [key, value] of Object.entries(obj)) {
    if (value !== null && value !== undefined && value !== '') {
      result[key] = String(value);
    }
  }
  return result;
}
