import { Injectable, signal } from '@angular/core';

export type FlashKind = 'success' | 'error' | 'info';

export interface FlashMessage {
  id: number;
  kind: FlashKind;
  text: string;
}

const AUTO_DISMISS_MS = 6000;

@Injectable({ providedIn: 'root' })
export class FlashService {
  private nextId = 0;
  readonly messages = signal<FlashMessage[]>([]);

  success(text: string): void {
    this.push('success', text);
  }

  error(text: string): void {
    this.push('error', text);
  }

  info(text: string): void {
    this.push('info', text);
  }

  dismiss(id: number): void {
    this.messages.update((list) => list.filter((m) => m.id !== id));
  }

  private push(kind: FlashKind, text: string): void {
    const id = this.nextId++;
    this.messages.update((list) => [...list, { id, kind, text }]);
    setTimeout(() => this.dismiss(id), AUTO_DISMISS_MS);
  }
}
