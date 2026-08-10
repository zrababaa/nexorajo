import { Injectable } from '@angular/core';
import type { AuthenticatedUser } from '../api/api.types';

interface StoredSession {
  token: string;
  expiresAtUtc: string;
  user: AuthenticatedUser;
}

const STORAGE_KEY = 'smpp.auth.session';

@Injectable({ providedIn: 'root' })
export class TokenStorage {
  load(): StoredSession | null {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as StoredSession;
    } catch {
      sessionStorage.removeItem(STORAGE_KEY);
      return null;
    }
  }

  save(session: StoredSession): void {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  }

  clear(): void {
    sessionStorage.removeItem(STORAGE_KEY);
  }
}

export type { StoredSession };
