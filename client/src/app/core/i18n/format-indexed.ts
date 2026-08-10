/** Substitutes .NET-style `{0}`, `{1}`, ... placeholders - the format the ported resx strings use. */
export function formatIndexed(template: string, ...args: Array<string | number>): string {
  return template.replace(/\{(\d+)\}/g, (match, index) => {
    const value = args[Number(index)];
    return value === undefined ? match : String(value);
  });
}
