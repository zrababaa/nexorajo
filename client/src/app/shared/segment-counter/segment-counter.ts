// Mirror of SMPP.Infrastructure.Segmenting.SegmentCounter (and its other mirror,
// wwwroot/js/app.js). Keep all three in step: this is what tells the user what a message will
// cost, and the server is what actually charges for it.
const LATIN_SINGLE = 160;
const LATIN_FIRST_OF_MULTI = 153;
const UNICODE_SINGLE = 70;
const UNICODE_FIRST_OF_MULTI = 67;

export interface SegmentInfo {
  characters: number;
  isUnicode: boolean;
  segments: number;
}

export function isUnicode(message: string): boolean {
  for (let i = 0; i < message.length; i++) {
    const c = message.charCodeAt(i);
    if (c === 10 || c === 13 || c === 9) {
      continue; // newlines and tabs exist in GSM-7 too
    }
    if (c < 32 || c > 126) {
      return true;
    }
  }
  return false;
}

export function countSegments(message: string): SegmentInfo {
  const unicode = isUnicode(message);
  const characters = message.length;

  if (characters === 0) {
    return { characters, isUnicode: unicode, segments: 0 };
  }

  const single = unicode ? UNICODE_SINGLE : LATIN_SINGLE;
  if (characters <= single) {
    return { characters, isUnicode: unicode, segments: 1 };
  }

  const chunk = unicode ? UNICODE_FIRST_OF_MULTI : LATIN_FIRST_OF_MULTI;
  return { characters, isUnicode: unicode, segments: Math.ceil(characters / chunk) };
}
