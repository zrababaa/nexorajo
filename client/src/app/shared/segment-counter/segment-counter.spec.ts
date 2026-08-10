import { countSegments } from './segment-counter';

describe('countSegments', () => {
  it('has zero segments for an empty message', () => {
    expect(countSegments('').segments).toBe(0);
  });

  describe.each([
    [159, 1],
    [160, 1],
    [161, 2],
    [306, 2],
    [307, 3],
  ])('latin message of length %i', (length, expectedSegments) => {
    it(`has ${expectedSegments} segment(s)`, () => {
      const message = 'a'.repeat(length);
      const result = countSegments(message);
      expect(result.isUnicode).toBe(false);
      expect(result.segments).toBe(expectedSegments);
    });
  });

  describe.each([
    [69, 1],
    [70, 1],
    [71, 2],
    [134, 2],
    [135, 3],
  ])('unicode message of length %i', (length, expectedSegments) => {
    it(`has ${expectedSegments} segment(s)`, () => {
      const message = 'ا'.repeat(length); // Arabic alef
      const result = countSegments(message);
      expect(result.isUnicode).toBe(true);
      expect(result.segments).toBe(expectedSegments);
    });
  });

  it('uses unicode thresholds for a mixed-script message', () => {
    const message = 'a'.repeat(100) + 'ا';
    expect(countSegments(message).segments).toBe(2);
  });

  it('does not push a latin message with line breaks onto unicode thresholds', () => {
    // GSM-7 carries CR/LF/tab, so a plain Latin message with a line break is still one part at
    // 160 characters - it must not be charged as three.
    const message = 'a'.repeat(79) + '\r\n' + 'a'.repeat(79);
    const result = countSegments(message);
    expect(result.isUnicode).toBe(false);
    expect(result.segments).toBe(1);
  });
});
