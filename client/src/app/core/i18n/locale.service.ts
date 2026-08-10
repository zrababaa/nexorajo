import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

const STORAGE_KEY = 'smpp.locale';
const RTL_LANGS = new Set(['ar']);
export const SUPPORTED_LANGS = ['en', 'ar'] as const;
export type SupportedLang = (typeof SUPPORTED_LANGS)[number];

@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly transloco = inject(TranslocoService);
  private readonly document = inject(DOCUMENT);

  init(): void {
    const lang = this.storedLang() ?? this.transloco.getDefaultLang();
    this.setLang(lang as SupportedLang);
    this.transloco.langChanges$.subscribe((lang) => this.applyDirection(lang));
  }

  setLang(lang: SupportedLang): void {
    localStorage.setItem(STORAGE_KEY, lang);
    this.transloco.setActiveLang(lang);
    this.applyDirection(lang);
  }

  get activeLang(): string {
    return this.transloco.getActiveLang();
  }

  isRtl(lang: string = this.activeLang): boolean {
    return RTL_LANGS.has(lang);
  }

  private storedLang(): SupportedLang | null {
    const stored = localStorage.getItem(STORAGE_KEY);
    return stored && (SUPPORTED_LANGS as readonly string[]).includes(stored) ? (stored as SupportedLang) : null;
  }

  private applyDirection(lang: string): void {
    const html = this.document.documentElement;
    html.lang = lang;
    html.dir = this.isRtl(lang) ? 'rtl' : 'ltr';
  }
}
